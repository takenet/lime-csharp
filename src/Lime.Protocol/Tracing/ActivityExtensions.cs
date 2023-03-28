using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Lime.Protocol.Network;

namespace Lime.Protocol.Tracing
{
    /// <summary>
    /// Extensions methods for <see cref="Activity"/> class to make it easier to create and start activities based on <see cref="Envelope"/> and other Lime objects.
    /// </summary>
    public static class ActivityExtensions
    {
        /// <summary>
        /// Copies the Trace Context from a dictionary to another.
        /// </summary>
        /// <param name="from">Dictionary to copy the Trace Context from if available</param>
        /// <param name="to">Dictionary to copy the Trace Context to</param>
        public static void CopyTraceParent(this IDictionary<string, string> from, IDictionary<string, string> to)
        {
            if (from == null || to == null || to.IsReadOnly)
            {
                return;
            }

            if (!from.ContainsW3CTraceContext())
            {
                return;
            }

            to[TraceContext.TraceParent] = from[TraceContext.TraceParent];

            if (from.TryGetValue(TraceContext.TraceState, out var traceState) && !string.IsNullOrWhiteSpace(traceState))
            {
                to[TraceContext.TraceState] = traceState;
            }
        }

        /// <summary>
        /// Checks if the dictionary contains the W3C Trace Context.
        /// </summary>
        /// <param name="from">Envelope to copy the Trace Context from if available</param>
        /// <param name="to">Envelope to copy the Trace Context to</param>
        public static void CopyTraceParent(this Envelope from, Envelope to)
        {
            if (from == null || to == null)
            {
                return;
            }

            if (!from.ContainsW3CTraceContext())
            {
                return;
            }

            to.Metadata ??= new Dictionary<string, string>();

            from.Metadata.CopyTraceParent(to.Metadata);
        }

        /// <summary>
        /// Checks if the dictionary contains the W3C Trace Context.
        /// </summary>
        /// <param name="dictionary"></param>
        public static bool ContainsW3CTraceContext(this IDictionary<string, string> dictionary)
        {
            return dictionary?.ContainsKey(TraceContext.TraceParent) ?? false;
        }

        /// <summary>
        /// Checks if Envelope's metadata contains the W3C Trace Context.
        /// </summary>
        /// <param name="envelope">The envelope to check for the Trace Context</param>
        public static bool ContainsW3CTraceContext(this Envelope envelope)
        {
            return envelope?.Metadata?.ContainsW3CTraceContext() ?? false;
        }

#nullable enable
        /// <summary>
        /// Creates and starts a new activity based on the current activity or a dictionary containing the W3C Trace Context.
        /// <param name="dictionary">The dictionary to retrieve W3C Trace Context</param>
        /// <param name="name">The name of the activity</param>
        /// <param name="kind">The <see cref="ActivityKind">ActivityKind</see> to be used in the new Activity</param>
        /// <param name="activitySource">The <see cref="ActivitySource">ActivitySource</see> to be used to create the new Activity. It will use <see cref="LimeActivitySource.Instance">LimeTracing.ActivitySource</see> if not specified.</param>
        /// <param name="prioritizeDictionaryActivity">If true and if the dictionary contains a valid W3C Trace Context, it will be used as context instead of the current Activity</param>
        /// </summary>
        /// <remarks>
        /// It will not set new trace context to the dictionary.
        /// If there is a current active Activity, it will be used as parent of the new Activity instead of the dictionary trace context, you can modify this behavior with the ignoreCurrentActivity parameter.
        /// </remarks>
        public static Activity? StartActivity(
            this IDictionary<string, string>? dictionary,
            [CallerMemberName] string name = "",
            ActivityKind kind = ActivityKind.Internal,
            ActivitySource? activitySource = null,
            bool prioritizeDictionaryActivity = false)
        {
            if (dictionary == null)
            {
                return null;
            }

            activitySource ??= LimeActivitySource.Instance;

            if (Activity.Current != null && !prioritizeDictionaryActivity)
            {
                return activitySource.StartActivity(name);
            }

            dictionary.TryGetValue(TraceContext.TraceParent, out var traceParent);
            if (string.IsNullOrWhiteSpace(traceParent))
            {
                return null;
            }

            dictionary.TryGetValue(TraceContext.TraceState, out var traceState);

            var success = ActivityContext.TryParse(traceParent, traceState, out var resultContext);

            return success ? activitySource.StartActivity(name, kind, resultContext) : activitySource.StartActivity(name);
        }

        /// <summary>
        /// Creates and starts a new activity based on the current activity or the envelope metadata.
        /// The newly created Activity will have its parent from the trace parent and trace state of the envelope metadata if available.
        /// </summary>
        /// <param name="envelope">The envelope to use as context</param>
        /// <param name="name">The name of the activity</param>
        /// <param name="kind">The <see cref="ActivityKind">ActivityKind</see> to be used in the new Activity</param>
        /// <param name="activitySource">The <see cref="ActivitySource">ActivitySource</see> to be used to create the new Activity. It will use <see cref="LimeActivitySource.Instance">LimeTracing.ActivitySource</see> if not specified.</param>
        /// <param name="prioritizeEnvelopeActivity">If true and if envelope's metadata contains an activity, it will be used as context instead of the current Activity</param>
        /// <returns>A new Activity created or null if there is no listener on the ActivitySource.</returns>
        /// <remarks>
        /// If the envelope is a Command with '/ping' Uri it will always return null.
        /// If there is a current active Activity, it will be used as parent of the new Activity instead of the dictionary trace context, you can modify this behavior with the ignoreCurrentActivity parameter.
        /// </remarks>
        public static Activity? StartActivity(
            this Envelope? envelope,
            [CallerMemberName] string name = "",
            ActivityKind kind = ActivityKind.Internal,
            ActivitySource? activitySource = null,
            bool prioritizeEnvelopeActivity = false
        )
        {
            switch (envelope)
            {
                case null:
                case Command possiblePing when (possiblePing.Type?.ToString() == "application/vnd.lime.ping+json" || possiblePing.Uri?.ToString() == "/ping"):
                    return null;
            }

            activitySource ??= LimeActivitySource.Instance;

            Activity? activity = null;

            if (Activity.Current == null || prioritizeEnvelopeActivity)
            {
                activity = StartActivity(envelope.Metadata, name, kind, activitySource, prioritizeDictionaryActivity: prioritizeEnvelopeActivity);
            }

            activity ??= activitySource.StartActivity(name);

            activity?.SetEnvelopeTags(envelope);
            activity?.InjectTraceParent(envelope);

            return activity;
        }
#nullable disable
        /// <summary>
        /// It will set the trace parent and trace state of the activity into the dictionary using W3C Trace Context format.
        /// </summary>
        /// <param name="activity">The activity to get the trace parent and trace state</param>
        /// <param name="dictionary">The dictionary to set the trace parent and trace state</param>
        /// <remarks>
        /// The dictionary must be mutable.
        /// </remarks>
        public static void InjectTraceParent(this Activity activity, IDictionary<string, string> dictionary)
        {
            if (activity == null || string.IsNullOrWhiteSpace(activity.Id) || dictionary == null || dictionary.IsReadOnly)
            {
                return;
            }

            dictionary[TraceContext.TraceParent] = activity.Id;

            if (string.IsNullOrWhiteSpace(activity.TraceStateString))
            {
                dictionary.Remove(TraceContext.TraceState);
            }
            else
            {
                dictionary[TraceContext.TraceState] = activity.TraceStateString;
            }
        }

        /// <summary>
        /// It will set the trace parent and trace state of the activity into envelope's metadata
        /// </summary>
        /// <param name="activity">The activity to get the trace parent and trace state</param>
        /// <param name="envelope">The envelope to set the trace parent and trace state</param>
        public static void InjectTraceParent(this Activity activity, Envelope envelope)
        {
            if (activity == null || envelope == null)
            {
                return;
            }

            envelope.Metadata ??= new Dictionary<string, string>();

            activity.InjectTraceParent(envelope.Metadata);
        }

        /// <summary>
        /// It will set the trace parent and trace state of the activity into envelope's metadata only if the envelope's metadata does not have a trace parent
        /// </summary>
        /// <param name="activity">The activity to get the trace parent and trace state</param>
        /// <param name="envelope">The envelope to set the trace parent and trace state</param>
        public static void InjectTraceParentIfAbsent(this Activity activity, Envelope envelope)
        {
            if (activity == null || envelope == null)
            {
                return;
            }

            if (envelope.ContainsW3CTraceContext())
            {
                return;
            }

            activity.InjectTraceParent(envelope);
        }

        /// <summary>
        /// Include envelope tags in the activity
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="envelope"></param>
        public static void SetEnvelopeTags(this Activity activity, Envelope envelope)
        {
            if (activity == null || envelope == null)
            {
                return;
            }

            activity.SetTag("lime.envelope.type", envelope.GetType().Name);
            activity.SetTag("lime.envelope.id", envelope.Id);
            activity.SetTag("lime.envelope.from", envelope.From?.ToString());
            activity.SetTag("lime.envelope.to", envelope.To?.ToString());

            switch (envelope)
            {
                case Command command:
                    activity.SetCommandTags(command);
                    break;
                case Notification notification:
                    activity.SetNotificationTags(notification);
                    break;
                case Message message:
                    activity.SetMessageTags(message);
                    break;
                case Session session:
                    activity.SetSessionTags(session);
                    break;
            }
        }

        /// <summary>
        /// Include command tags in the activity
        /// </summary>
        /// <param name="activity">The current activity to include command info</param>
        /// <param name="notification">The notification to be traced</param>
        public static void SetNotificationTags(this Activity activity, Notification notification)
        {
            if (activity == null || notification == null)
            {
                return;
            }

            activity.SetTag("lime.notification.event", notification.Event.ToString());

            activity.SetNotificationStatusTag(notification);
        }

        /// <summary>
        /// Include command tags in the activity
        /// </summary>
        /// <param name="activity">The current activity to include command info</param>
        /// <param name="command">The command to be traced</param>
        public static void SetCommandTags(this Activity activity, Command command)
        {
            if (activity == null || command == null)
            {
                return;
            }

            activity.SetTag("lime.command.uri", command.Uri?.ToString());
            activity.SetTag("lime.command.method", command.Method.ToString());
            activity.SetTag("lime.command.type", command.Type?.ToString());

            activity.SetCommandStatusTag(command);
        }

        /// <summary>
        /// Include command tags in the activity
        /// </summary>
        /// <param name="activity">The current activity to include command info</param>
        /// <param name="session">The session to be traced</param>
        public static void SetSessionTags(this Activity activity, Session session)
        {
            if (activity == null || session == null)
            {
                return;
            }

            activity.SetTag("lime.session.state", session.State.ToString());

            activity.SetSessionStatusTag(session);
        }

        /// <summary>
        /// Include command tags in the activity
        /// </summary>
        /// <param name="activity">The current activity to include command info</param>
        /// <param name="message">The message to be traced</param>
        public static void SetMessageTags(this Activity activity, Message message)
        {
            if (activity == null || message == null)
            {
                return;
            }

            activity.SetTag("lime.message.type", message.Type?.ToString());
        }

        public static void SetCommandStatusTag(this Activity activity, Command command)
        {
            if (activity == null || command == null)
            {
                return;
            }

            activity.SetTag("lime.command.status", command.Status.ToString());

            switch (command.Status)
            {
                case CommandStatus.Failure:
                    activity.SetStatus(ActivityStatusCode.Error, $"Code {command.Reason?.Code}: {command.Reason?.Description}");
                    break;
                case CommandStatus.Success:
                    activity.SetStatus(ActivityStatusCode.Ok);
                    break;
                case CommandStatus.Pending:
                default:
                    activity.SetStatus(ActivityStatusCode.Unset);
                    break;
            }
        }

        public static void SetNotificationStatusTag(this Activity activity, Notification notification)
        {
            if (activity == null || notification == null)
            {
                return;
            }

            switch (notification.Event)
            {
                case Event.Failed:
                    activity.SetStatus(ActivityStatusCode.Error, $"Code {notification.Reason?.Code}: {notification.Reason?.Description}");
                    break;
                default:
                    activity.SetStatus(ActivityStatusCode.Ok);
                    break;
            }
        }

        public static void SetSessionStatusTag(this Activity activity, Session session)
        {
            if (activity == null || session == null)
            {
                return;
            }

            switch (session.State)
            {
                case SessionState.Failed:
                    activity.SetStatus(ActivityStatusCode.Error, $"Code {session.Reason?.Code}: {session.Reason?.Description}");
                    break;
                case SessionState.New:
                    activity.SetStatus(ActivityStatusCode.Unset);
                    break;
                default:
                    activity.SetStatus(ActivityStatusCode.Ok);
                    break;
            }
        }

        /// <summary>
        /// Gets a string to be used as activity name based on the envelope type
        /// </summary>
        /// <param name="envelope"></param>
        public static string GetActivityName(this Envelope envelope)
        {
            if (envelope == null)
            {
                return null;
            }

            var name = envelope.GetType().Name;

            if (envelope is Session session)
            {
                return $"{name} {session.State}";
            }

            return name;
        }

        /// <summary>
        /// Sets the transport tags in the activity
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="transport"></param>
        public static void SetTransportTags(this Activity activity, ITransport transport)
        {
            if (activity == null || transport == null)
            {
                return;
            }

            activity.SetTag("lime.transport.local_endpoint", transport.LocalEndPoint);
            activity.SetTag("lime.transport.remote_endpoint", transport.RemoteEndPoint);
            activity.SetTag("lime.transport.type", transport.GetType().Name);
        }

        /// <summary>
        /// Sets the node tags in the activity
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="node"></param>
        public static void SetNodeTags(this Activity activity, Node node)
        {
            if (activity == null || node == null)
            {
                return;
            }

            activity.SetTag("lime.node", node.ToString());
        }

        /// <summary>
        /// Sets the channel tags in the activity
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="channel"></param>
        public static void AddChannelTags(this Activity activity, IChannel channel)
        {
            if (channel == null)
            {
                return;
            }

            activity?.AddTag("lime.channel.local_node", channel.LocalNode?.ToString());
            activity?.AddTag("lime.channel.remote_node", channel.RemoteNode?.ToString());
        }
    }
}