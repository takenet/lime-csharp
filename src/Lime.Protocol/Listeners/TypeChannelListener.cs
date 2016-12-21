#if NETSTANDARD1_6 || NET461

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol.Network;
using Lime.Protocol.Util;
using static Lime.Protocol.Envelope;
using static Lime.Protocol.Message;

namespace Lime.Protocol.Listeners
{
    public class TypeChannelListener : IChannelListener
    {
        public const string MESSAGE_KEY = "message";
        public const string CANCELLATION_TOKEN_KEY = "cancellationToken";
        public const string MESSAGE_SENDER_CHANNEL_KEY = "messageSenderChannel";
        public const string NOTIFICATION_SENDER_CHANNEL_KEY = "notificationSenderChannel";
        public const string COMMAND_SENDER_CHANNEL_KEY = "commandSenderChannel";
        public const string COMMAND_PROCESSOR_KEY = "commandProcessor";

        private static readonly IDictionary<string, Type> MessageParameters = new Dictionary<string, Type>
        {
            { ID_KEY, typeof(string) },
            { FROM_KEY, typeof(Node) },
            { PP_KEY, typeof(Node) },
            { TO_KEY, typeof(Node) },
            { CONTENT_KEY, typeof(IDocument) },
            { METADATA_KEY, typeof(IDictionary<string, string>) },
            { MESSAGE_KEY, typeof(Message) },
            { CANCELLATION_TOKEN_KEY, typeof(CancellationToken) },
            { MESSAGE_SENDER_CHANNEL_KEY, typeof(IMessageSenderChannel) },
            { NOTIFICATION_SENDER_CHANNEL_KEY, typeof(INotificationSenderChannel) },
            { COMMAND_SENDER_CHANNEL_KEY, typeof(ICommandSenderChannel) },
            { COMMAND_PROCESSOR_KEY, typeof(ICommandProcessor) },
        };

        private readonly object _listener;
        private readonly ChannelListener _channelListener;
        private readonly IDictionary<string, MethodInfo> _contentTypeMethodDictionary;
        private readonly IDictionary<string, object> _internalValuesDictionary;

        public TypeChannelListener(object listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            _listener = listener;
            MessageNotMatchedHandlers = new List<Func<Message, Task>>();
            MessageConsumeFailedHandlers = new List<Func<Message, Exception, Task>>();
            _contentTypeMethodDictionary = new Dictionary<string, MethodInfo>();
            _internalValuesDictionary = new Dictionary<string, object>();
            _channelListener = new ChannelListener(
                ConsumeMessageAsync,
                ConsumeNotificationAsync,
                ConsumeCommandAsync);
            RegisterListenerMethods();
        }

        public void Start(IEstablishedReceiverChannel establishedReceiverChannel)
        {
            _internalValuesDictionary[MESSAGE_SENDER_CHANNEL_KEY] = establishedReceiverChannel as IMessageSenderChannel;
            _internalValuesDictionary[NOTIFICATION_SENDER_CHANNEL_KEY] = establishedReceiverChannel as INotificationSenderChannel;
            _internalValuesDictionary[COMMAND_SENDER_CHANNEL_KEY] = establishedReceiverChannel as ICommandSenderChannel;
            _internalValuesDictionary[COMMAND_PROCESSOR_KEY] = establishedReceiverChannel as ICommandProcessor;
            _channelListener.Start(establishedReceiverChannel);
        }

        public void Stop()
        {
            _channelListener.Stop();
        }

        public Task<Message> MessageListenerTask => _channelListener.MessageListenerTask;

        public Task<Notification> NotificationListenerTask => _channelListener.NotificationListenerTask;

        public Task<Command> CommandListenerTask => _channelListener.CommandListenerTask;

        public ICollection<Func<Message, Task>> MessageNotMatchedHandlers { get; }

        public ICollection<Func<Message, Exception, Task>> MessageConsumeFailedHandlers { get; }

        public void Dispose()
        {
            _channelListener.Dispose();
        }

        private void RegisterListenerMethods()
        {
            var messageReceiverMethods = _listener
                .GetType()
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var method in messageReceiverMethods)
            {
                var messageReceiver = method.GetCustomAttribute<MessageReceiverAttribute>(true);
                if (messageReceiver == null) continue;

                if (method.ReturnType != typeof(Task))
                {
                    throw new ArgumentException($"The method '{method.Name}' must return a 'Task'");
                }

                var parameters = method.GetParameters().Where(p => p.Name != CANCELLATION_TOKEN_KEY).ToArray();
                if (parameters.Length == 0) throw new ArgumentException($"The method '{method.Name}' doesn't have any value parameter");

                var unconventionalParameters = parameters.Where(p => !MessageParameters.ContainsKey(p.Name)).ToArray();

                // Validate the method parameters
                foreach (var parameter in parameters.Where(p => !p.Name.Equals(CONTENT_KEY)))
                {
                    Type parameterType;
                    if (MessageParameters.TryGetValue(parameter.Name, out parameterType))
                    {
                        if (!parameter.ParameterType.GetTypeInfo().IsAssignableFrom(parameterType))
                        {
                            throw new ArgumentException(
                                $"The method '{method.Name}' convention argument '{parameter.Name}' must be of type '{parameterType.Name}' or have a different name");
                        }
                    }
                }

                // Check for the 'content' parameter
                var contentParameter = parameters.FirstOrDefault(c => c.Name.Equals(CONTENT_KEY));
                if (contentParameter != null)
                {
                    // If present, only conventional parameters are allowed in the method.
                    var unconventionalParameter = unconventionalParameters.FirstOrDefault();
                    if (unconventionalParameter != null)
                    {
                        throw new ArgumentException(
                            $"The method '{method.Name}' already defines a '{CONTENT_KEY}' parameter and should define only convention arguments. The invalid argument is '{unconventionalParameter.Name}'.");
                    }

                    if (contentParameter.ParameterType == typeof(JsonDocument))
                    {
                        if (string.IsNullOrWhiteSpace(messageReceiver.ContentType))
                        {
                            messageReceiver.ContentType = "application/json";
                        }

                        var mediaType = MediaType.Parse(messageReceiver.ContentType);
                        if (!mediaType.IsJson)
                        {
                            throw new ArgumentException(
                                $"The method '{method.Name}' argument '{contentParameter.Name}' content type must be of JSON subtype.");
                        }
                    }
                    else if (contentParameter.ParameterType == typeof(PlainDocument))
                    {
                        if (string.IsNullOrWhiteSpace(messageReceiver.ContentType))
                        {
                            messageReceiver.ContentType = "text/plain";
                        }

                        var mediaType = MediaType.Parse(messageReceiver.ContentType);
                        if (mediaType.IsJson)
                        {
                            throw new ArgumentException(
                                $"The method '{method.Name}' argument '{contentParameter.Name}' content type must be of plain type.");
                        }
                    }
                    else if (typeof(IDocument).GetTypeInfo().IsAssignableFrom(contentParameter.ParameterType))
                    {
                        var document = (IDocument)Activator.CreateInstance(contentParameter.ParameterType);
                        var documentContentType = document.GetMediaType().ToString();

                        if (string.IsNullOrWhiteSpace(messageReceiver.ContentType))
                        {
                            messageReceiver.ContentType = documentContentType;
                        }
                        else if (!messageReceiver.ContentType.ToLowerInvariant().Equals(documentContentType))
                        {
                            throw new ArgumentException(
                                $"The method '{method.Name}' argument '{contentParameter.Name}' content type is different from the declared. Expected is '{messageReceiver.ContentType}' and actual is '{documentContentType}'. Change or remove the content type definition in the method.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"The method '{method.Name}' argument '{contentParameter.Name}' must inherit 'IDocument'.");
                    }
                }
                else
                {
                    // Create a document type for the method
                    messageReceiver.ContentType =
                        $"application/x-{method.DeclaringType.FullName}-{method.Name}+json";
                }

                if (_contentTypeMethodDictionary.ContainsKey(messageReceiver.ContentType.ToLowerInvariant()))
                {
                    throw new ArgumentException(
                        $"There's more than a method for processing the '{messageReceiver.ContentType}' content type. Try change the method name or specify the '{nameof(MessageReceiverAttribute.ContentType)}' value in the '{nameof(MessageReceiverAttribute)}.");
                }

                _contentTypeMethodDictionary.Add(messageReceiver.ContentType.ToLowerInvariant(), method);
            }
        }

        private async Task<bool> ConsumeMessageAsync(Message message)
        {
            try
            {
                MethodInfo method;
                if (_contentTypeMethodDictionary.TryGetValue(message.Type.ToString().ToLowerInvariant(), out method))
                {
                    var conventionMessageValues = new Dictionary<string, object>
                    {
                        {ID_KEY, message.Id},
                        {FROM_KEY, message.From},
                        {PP_KEY, message.Pp},
                        {TO_KEY, message.To},
                        {TYPE_KEY, message.Type},
                        {CONTENT_KEY, message.Content},
                        {METADATA_KEY, message.Metadata},
                        {MESSAGE_KEY, message},
                        {CANCELLATION_TOKEN_KEY, CancellationToken.None}
                    };

                    var jsonDocumentContent = message.Content as JsonDocument;
                    if (jsonDocumentContent != null)
                    {
                        foreach (var keyValue in jsonDocumentContent)
                        {
                            conventionMessageValues.Add(keyValue.Key, keyValue.Value);
                        }
                    }

                    var arguments = method.GetParameters()
                        .Select(p =>
                        {
                            object value;
                            if (!conventionMessageValues.TryGetValue(p.Name, out value) &&
                                !_internalValuesDictionary.TryGetValue(p.Name, out value))
                            {
                                throw new ArgumentException($"Could not find the value for the argument '{p.Name}' for the method '{method.Name}'", nameof(message));
                            }

                            if (!p.ParameterType.GetTypeInfo().IsInstanceOfType(value))
                            {
                                try
                                {
                                    value = Convert.ChangeType(value, p.ParameterType);
                                }
                                catch
                                {
                                    throw new ArgumentException(
                                        $"The argument '{p.Name}' of method '{method.Name}' expects a different type than the available. The expected type is '{p.ParameterType.Name}' and available is '{value.GetType().Name}'.",
                                        nameof(message));
                                }
                            }

                            return value;
                        }).ToArray();
                    await (Task) method.Invoke(_listener, arguments);
                }
                else
                {
                    if (MessageNotMatchedHandlers.Count > 0)
                    {
                        await Task.WhenAll(
                            MessageNotMatchedHandlers.ToList().Select(h => h(message)));
                    }                
                }
            }
            catch (Exception ex)
            {
                if (MessageConsumeFailedHandlers.Count > 0)
                {
                    await Task.WhenAll(
                        MessageConsumeFailedHandlers.ToList()
                            .Select(h => h(message, ex)));
                }
            }

            return true;
        }

        private Task<bool> ConsumeNotificationAsync(Notification arg)
        {
            return TaskUtil.TrueCompletedTask;
        }

        private Task<bool> ConsumeCommandAsync(Command arg)
        {
            return TaskUtil.TrueCompletedTask;
        }        
    }
}
#endif


