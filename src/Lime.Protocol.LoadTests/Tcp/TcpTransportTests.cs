﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Serialization;
using Lime.Protocol.Serialization.Newtonsoft;
using Lime.Protocol.UnitTests;
using Lime.Transport.Tcp;
using Shouldly;
using Xunit;

namespace Lime.Protocol.LoadTests.Tcp
{

    public class TcpTransportTests : IDisposable
    {
        private Uri _uri;
        private CancellationToken _cancellationToken;
        private IEnvelopeSerializer _envelopeSerializer;
        private TcpTransportListener _tcpTransportListener;
        private TcpTransport _clientTcpTransport;
        private TcpTransport _serverTcpTransport;



        public TcpTransportTests()
        {
            _uri = new Uri("net.tcp://localhost:55321");
            _cancellationToken = TimeSpan.FromSeconds(30).ToCancellationToken();
            _envelopeSerializer = new FakeEnvelopeSerializer(10);
            _tcpTransportListener = new TcpTransportListener(_uri, null, _envelopeSerializer);
            _tcpTransportListener.StartAsync(_cancellationToken).Wait();
            var serverTcpTransportTask = _tcpTransportListener.AcceptTransportAsync(_cancellationToken);
            _clientTcpTransport = new TcpTransport(_envelopeSerializer);
            _clientTcpTransport.OpenAsync(_uri, _cancellationToken).Wait();
            _serverTcpTransport = (TcpTransport)serverTcpTransportTask.Result;
            _serverTcpTransport.OpenAsync(_uri, _cancellationToken).Wait();
        }

        public void Dispose()
        {
            _clientTcpTransport.CloseAsync(CancellationToken.None).Wait();
            _serverTcpTransport.CloseAsync(CancellationToken.None).Wait();
            _tcpTransportListener.StopAsync(_cancellationToken).Wait();
        }


        [Fact]
        [Trait("tcp", "Receive10kEnvelopes")]
        public async Task Send10000EnvelopesAsync()
        {
            // Arrange
            var count = 10000;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTcpTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTcpTransport.SendAsync(envelope, _cancellationToken);
            }

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

        [Fact]
        [Trait("tcp", "Send500Envelopes")]
        public async Task Send500EnvelopesAsync()
        {
            // Arrange
            var count = 500;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTcpTransport.SendAsync(envelope, _cancellationToken);
            }

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTcpTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }

        [Fact]
        [Trait("tcp", "Send200Envelopes")]
        public async Task Send200EnvelopesAsync()
        {
            // Arrange
            var count = 200;
            var envelopes = Enumerable
                .Range(0, count)
                .Select(i => Dummy.CreateMessage(Dummy.CreateTextContent()));

            // Act
            var sw = Stopwatch.StartNew();
            foreach (var envelope in envelopes)
            {
                await _clientTcpTransport.SendAsync(envelope, _cancellationToken);
            }

            var receivedEnvelopes = Enumerable
                .Range(0, count)
                .Select(i => _serverTcpTransport.ReceiveAsync(_cancellationToken))
                .ToArray();

            await Task.WhenAll(receivedEnvelopes);
            sw.Stop();

            // Assert
            sw.ElapsedMilliseconds.ShouldBeLessThan(count * 2);
        }
    }
}
