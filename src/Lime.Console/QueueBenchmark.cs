using Lime.Protocol.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lime.Console
{
    public class QueueBenchmark
    {
        public static async Task RunAsync()
        {
            int count = 1000000;

            System.Console.WriteLine("AsyncQueue - Starting...");
            var sw1 = Stopwatch.StartNew();

            await RunAsyncQueueAsync(count);

            sw1.Stop();
            System.Console.WriteLine("AsyncQueue - Elapsed time: {0} ms", sw1.ElapsedMilliseconds);

            System.Console.WriteLine("BufferBlock - Starting...");
            var sw2 = Stopwatch.StartNew();

            await RunBufferBlockAsync(count);

            sw2.Stop();
            System.Console.WriteLine("BufferBlock - Elapsed time: {0} ms", sw2.ElapsedMilliseconds);
        }


        public static async Task RunBufferBlockAsync(int count)
        {
            var cancellationToken = CancellationToken.None;

            var target = new BufferBlock<string>();

            var enqueueTasks = new Task[count];
            var dequeueTasks = new Task<string>[count];

            var enqueueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var item = "HVQFSW613ACT65DOS2ILBA4G4QB3UP3K4PPZPB7UZ771SX9TX7DVNZR82W1TSHWDBZHIE8V6CGILADRFQ3QA76BOYA4T3XS7A8OQ3I2FCT8X04L2GXR3RY23WB2A0ZLNT58WCMZTY54PRPOVVENCMOJMCZC6D85H9HPGJ58BBOHN7PJ0G3QTDSB8K4ACT26QXG5D30WI";
                    enqueueTasks[i] = Task.Run(() => target.Post(item));
                }
            });

            var dequeueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    dequeueTasks[i] = target.ReceiveAsync(cancellationToken);
                }
            });

            await Task.WhenAll(enqueueSetupTask, dequeueSetupTask);
            await Task.WhenAll(dequeueTasks);
            await Task.WhenAll(enqueueTasks);

            if (target.Count != 0)
            {
                System.Console.WriteLine("BufferBlock - Not empty");
            }

        }

        public static async Task RunAsyncQueueAsync(int count)
        {
            var cancellationToken = CancellationToken.None;

            var target = new AsyncQueue<string>(count, count);

            var enqueueTasks = new Task[count];
            var dequeueTasks = new Task<string>[count];

            var enqueueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var item = "HVQFSW613ACT65DOS2ILBA4G4QB3UP3K4PPZPB7UZ771SX9TX7DVNZR82W1TSHWDBZHIE8V6CGILADRFQ3QA76BOYA4T3XS7A8OQ3I2FCT8X04L2GXR3RY23WB2A0ZLNT58WCMZTY54PRPOVVENCMOJMCZC6D85H9HPGJ58BBOHN7PJ0G3QTDSB8K4ACT26QXG5D30WI";
                    enqueueTasks[i] = Task.Run(() => target.Enqueue(item));
                }
            });

            var dequeueSetupTask = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    dequeueTasks[i] = target.DequeueAsync(cancellationToken);
                }
            });

            await Task.WhenAll(enqueueSetupTask, dequeueSetupTask);
            await Task.WhenAll(dequeueTasks);
            await Task.WhenAll(enqueueTasks);

            if (target.BufferCount != 0 ||
                target.PromisesCount != 0)
            {
                System.Console.WriteLine("AsyncQueue - Not empty");
            }
        }
    }
}
