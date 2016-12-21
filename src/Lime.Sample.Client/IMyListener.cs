using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging;
using Lime.Messaging.Contents;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Lime.Protocol.Listeners;
using Lime.Protocol.Network;

namespace Lime.Sample.Client
{
    public class MyListener
    {
        public MyListener()
        {
            Registrator.RegisterDocuments();
        }

        [MessageReceiver]
        public Task ReceiveAsync(string value1, int value2, bool value3)
        {
            return Task.CompletedTask;
        }

        [MessageReceiver]
        public Task ReceiveWebLinkAsync(WebLink content, Node @from, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [MessageReceiver(ContentType = "application/json")]
        public Task ReceiveJsonAsync(JsonDocument content, Node @from, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [MessageReceiver(ContentType = "text/any")]
        public Task ReceiveAnyContentAsync(PlainDocument content, Node @from, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [MessageReceiver]
        public async Task ReceivePlaintextAsync(PlainText content, Node @from, IMessageSenderChannel messageSenderChannel, CancellationToken cancellationToken)
        {
            await messageSenderChannel.SendMessageAsync(new Message()
            {
                To = from,
                Content = new PlainText() { Text = $"Pong for {content.Text}" }
            });
        }

        [CommandProcessor(Method = CommandMethod.Set, UriTemplate = "/presence")]
        public Task SetPresenceAsync(Presence presence, Node @from, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [CommandProcessor(Method = CommandMethod.Get, UriTemplate = "/presence/{identity}")]
        public Task<Presence> GetPresenceAsync(Uri uri, CancellationToken cancellationToken)
        {
            return new Presence().AsCompletedTask();
        }
    }
}