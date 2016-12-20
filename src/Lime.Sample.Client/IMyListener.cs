using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging;
using Lime.Messaging.Contents;
using Lime.Protocol;
using Lime.Protocol.Listeners;

namespace Lime.Sample.Client
{
    public interface IMyListener
    {

    }

    public class MyListener : IMyListener
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
        public Task ReceivePlaintextAsync(PlainText content, Node @from, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}