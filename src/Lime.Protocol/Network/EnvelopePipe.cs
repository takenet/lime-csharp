#if NETSTANDARD2_1
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Server;

namespace Lime.Protocol.Network
{
    /// <summary>
    /// Manages <see cref="ITransport"/> buffers using the <see cref="System.IO.Pipelines.Pipe"/> class.
    /// </summary>
    public sealed class EnvelopePipe : IStartable, IStoppable, IDisposable
    {
        public const int DEFAULT_PAUSE_WRITER_THRESHOLD = 8192 * 1024;
        
        private readonly Func<Memory<byte>, CancellationToken, ValueTask<int>> _receiveFunc;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> _sendFunc;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly ITraceWriter _traceWriter;
        private readonly int _pauseWriterThreshold;
        private readonly Pipe _receivePipe;
        private readonly Pipe _sendPipe;
        private readonly CancellationTokenSource _pipeCts;
        private readonly SemaphoreSlim _semaphore;

        private Task _receiveTask;
        private Task _sendTask;

        /// <summary>
        /// Creates a new instance of <see cref="EnvelopePipe"/>.
        /// </summary>
        /// <param name="receiveFunc">The function that will be invoked by the receive task, which passes a memory to be filled by the underlying connection.</param>
        /// <param name="sendFunc">The function that will be invoked by the send task, container the buffer that should be written to the underlying connection.</param>
        /// <param name="envelopeSerializer">The envelope serializer</param>
        /// <param name="traceWriter">The trace writer</param>
        /// <param name="pauseWriterThreshold">The number of unconsumed bytes in the receive pipe to pause the read task.</param>
        /// <param name="memoryPool">The memory pool instance to be used by the pipes</param>
        public EnvelopePipe(
            Func<Memory<byte>, CancellationToken, ValueTask<int>> receiveFunc,
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> sendFunc,
            IEnvelopeSerializer envelopeSerializer, 
            ITraceWriter traceWriter = null, 
            int pauseWriterThreshold = -1, 
            MemoryPool<byte> memoryPool = null)
        {
            _receiveFunc = receiveFunc ?? throw new ArgumentNullException(nameof(receiveFunc));
            _sendFunc = sendFunc ?? throw new ArgumentNullException(nameof(sendFunc));
            _envelopeSerializer = envelopeSerializer ?? throw new ArgumentNullException(nameof(envelopeSerializer));
            _traceWriter = traceWriter;
            _pauseWriterThreshold = pauseWriterThreshold > 0 ? pauseWriterThreshold : -1;
            var pipeOptions = new PipeOptions(
                pool: memoryPool ?? MemoryPool<byte>.Shared,
                pauseWriterThreshold: _pauseWriterThreshold);
            
            _receivePipe = new Pipe(pipeOptions);
            _sendPipe = new Pipe(pipeOptions);
            _pipeCts = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(1);
        }
        
        /// <summary>
        /// Starts the pipe producer / consumer tasks.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_receiveTask != null ||
                    _sendTask != null)
                {
                    throw new InvalidOperationException("The pipe is already started");
                }
                
                _receiveTask = ReceiveAndWriteToPipeAsync(_receiveFunc, _receivePipe.Writer, _pipeCts.Token);
                _sendTask = ReadPipeAndSendAsync(_sendFunc, _sendPipe.Reader, _pipeCts.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Stops the pipe producer / consumer tasks and awaits they to end.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_receiveTask == null && _sendTask == null)
                {
                    throw new InvalidOperationException("The pipe is not started");
                }
                _pipeCts.Cancel();
                if (_receiveTask != null) await _receiveTask;
                if (_sendTask != null) await _sendTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Writes a envelope into the send pipe.
        /// </summary>
        public async Task SendAsync(Envelope envelope, CancellationToken cancellationToken)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (_sendTask == null) throw new InvalidOperationException($"The send task was not initialized. Call {nameof(StartAsync)} first.");
            if (_sendTask.IsCompleted) throw new InvalidOperationException("Send task is completed");
            
            var envelopeJson = _envelopeSerializer.Serialize(envelope);
            await TraceAsync(envelopeJson, DataOperation.Send).ConfigureAwait(false);
            
            // Gets memory from the pipe to write the encoded string.
            // .NET string are UTF16 and the length to UTF8 is usually larger.
            // We can use Encoding.UTF8.GetBytes instead to get the precise length but here this is just a hint and the impact is irrelevant, 
            // so we can avoid the overhead.
            var memory = _sendPipe.Writer.GetMemory(envelopeJson.Length); 
            var length = Encoding.UTF8.GetBytes(envelopeJson, memory.Span);

            if (_pauseWriterThreshold > 0 && 
                length >= _pauseWriterThreshold)
            {
                throw new InvalidOperationException("Serialized envelope size is larger than pauseWriterThreshold and cannot be sent");
            }
            
            // Signals the pipe that the data is ready.
            _sendPipe.Writer.Advance(length);
            
            var flushResult = await _sendPipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (flushResult.IsCompleted || flushResult.IsCanceled)
            {
                throw new InvalidOperationException("Send pipe is completed");
            }
        }

        /// <summary>
        /// Receives an envelope from the receive pipe.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Envelope> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_receiveTask == null) throw new InvalidOperationException($"The receive task was not initialized. Call {nameof(StartAsync)} first.");
            if (_receiveTask.IsCompleted) throw new InvalidOperationException("The receive task is completed");            
            
            Envelope envelope = null;
            
            while (envelope == null &&
                   !cancellationToken.IsCancellationRequested)
            {
                var readResult = await _receivePipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = readResult.Buffer;
                if (readResult.IsCompleted || buffer.IsEmpty)
                {
                    // The receiveTask is completing, no need to throw an exception.
                    break;
                }

                var consumed = buffer.Start;
                if (JsonBuffer.TryExtractJsonFromBuffer(buffer, out var json))
                {
                    var envelopeJson = Encoding.UTF8.GetString(json.ToArray());
                    await TraceAsync(envelopeJson, DataOperation.Receive).ConfigureAwait(false);
                    envelope = _envelopeSerializer.Deserialize(envelopeJson);
                    consumed = json.End;
                }

                if (envelope != null)
                {
                    // An envelope was found and the buffer may contain another one
                    _receivePipe.Reader.AdvanceTo(consumed);
                }
                else
                {
                    // No envelope found after examining the whole buffer, more data is needed
                    _receivePipe.Reader.AdvanceTo(consumed, buffer.End);
                }
            }            
            
            return envelope;
        }
        
        public void Dispose()
        {
            _pipeCts.Dispose();
        }

        private Task TraceAsync(string data, DataOperation operation)
        {
            if (_traceWriter == null || !_traceWriter.IsEnabled) return Task.CompletedTask;
            return _traceWriter.TraceAsync(data, operation);
        }
        
        /// <summary>
        /// Receives data from the provided function using the memory retrieve from the pipe writer.  
        /// </summary>
        private static async Task ReceiveAndWriteToPipeAsync(
            Func<Memory<byte>, CancellationToken, ValueTask<int>> receiveFunc, 
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            Exception exception = null;
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var memory = writer.GetMemory();
                    var read = await receiveFunc(memory, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    writer.Advance(read);
                    
                    var flushResult = await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (flushResult.IsCompleted || flushResult.IsCanceled) break;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                await writer.CompleteAsync(exception).ConfigureAwait(false);                
            }
        }

        /// <summary>
        /// Read data from the provided pipe and send it using the function. 
        /// </summary>
        private static async Task ReadPipeAndSendAsync(
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> sendFunc, 
            PipeReader reader, 
            CancellationToken cancellationToken)
        {
            Exception exception = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;

                    if (result.IsCompleted || buffer.IsEmpty) break;

                    foreach (var memory in buffer)
                    {
                        await sendFunc(memory, cancellationToken);
                    }

                    reader.AdvanceTo(buffer.End);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                await reader.CompleteAsync(exception).ConfigureAwait(false);
            }
        }
    }
}
#endif