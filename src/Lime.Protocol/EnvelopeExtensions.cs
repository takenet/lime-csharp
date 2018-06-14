using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lime.Protocol
{
    /// <summary>
    /// Utility extension methods for the <see cref="Envelope"/> class.
    /// </summary>
    public static class EnvelopeExtensions
    {
        public const string COMMAND_MIME_TYPE = "application/vnd.lime.command+json";
        public const string MESSAGE_MIME_TYPE = "application/vnd.lime.message+json";
        public const string NOTIFICATION_MIME_TYPE = "application/vnd.lime.notification+json";

        public static readonly MediaType CommandMediaType = MediaType.Parse(COMMAND_MIME_TYPE);
        public static readonly MediaType MessageMediaType = MediaType.Parse(MESSAGE_MIME_TYPE);
        public static readonly MediaType NotificationMediaType = MediaType.Parse(NOTIFICATION_MIME_TYPE);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Command"/>.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Command command, JsonSerializer jsonSerializer) => ToDocument(command, CommandMediaType, jsonSerializer);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Message message, JsonSerializer jsonSerializer) => ToDocument(message, MessageMediaType, jsonSerializer);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Notification"/>.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Notification notification, JsonSerializer jsonSerializer) => ToDocument(notification, NotificationMediaType, jsonSerializer);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="envelope">The envelope.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static JsonDocument ToDocument<T>(T envelope, MediaType mediaType, JsonSerializer jsonSerializer) where T : Envelope, new()
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));
            var jObject = JObject.FromObject(envelope, jsonSerializer);
            var dictionary = jObject.ToObject<Dictionary<string, object>>();
            return new JsonDocument(dictionary, mediaType);
        }

        /// <summary>
        /// Creates a <see cref="Message"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Message ToMessage(this JsonDocument jsonDocument, JsonSerializer jsonSerializer) => (Message)ToEnvelope(jsonDocument, MessageMediaType, jsonSerializer);

        /// <summary>
        /// Creates a <see cref="Command"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Command ToCommand(this JsonDocument jsonDocument, JsonSerializer jsonSerializer) => (Command)ToEnvelope(jsonDocument, CommandMediaType, jsonSerializer);

        /// <summary>
        /// Creates a <see cref="Notification"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Notification ToNotification(this JsonDocument jsonDocument, JsonSerializer jsonSerializer) => (Notification)ToEnvelope(jsonDocument, NotificationMediaType, jsonSerializer);

        /// <summary>
        /// Creates an <see cref="Envelope"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <param name="jsonSerializer"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Unknown envelope media type</exception>
        public static Envelope ToEnvelope(this JsonDocument jsonDocument, MediaType mediaType, JsonSerializer jsonSerializer)
        {
            Type envelopeType;
            if (mediaType.Equals(CommandMediaType)) envelopeType = typeof(Command);
            else if (mediaType.Equals(MessageMediaType)) envelopeType = typeof(Message);
            else if (mediaType.Equals(NotificationMediaType)) envelopeType = typeof(Notification);
            else throw new ArgumentException("Unknown envelope media type");

            var jObject = JObject.FromObject(jsonDocument, jsonSerializer);
            return (Envelope)jObject.ToObject(envelopeType, jsonSerializer);
        }

        /// <summary>
        /// Gets a shallow copy of the current <see cref="Envelope"/>.
        /// </summary>
        /// <typeparam name="TEnvelope"></typeparam>
        /// <returns></returns>
        public static TEnvelope ShallowCopy<TEnvelope>(this TEnvelope envelope) where TEnvelope : Envelope, new()
        {
            return (TEnvelope)envelope.MemberwiseClone();
        }

        /// <summary>
        /// Gets the sender node of the envelope.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <returns></returns>
        public static Node GetSender(this Envelope envelope)
        {
            return envelope.Pp ?? envelope.From;
        }
    }
}