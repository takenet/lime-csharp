using System;
using System.Collections.Generic;
using Lime.Protocol.Serialization.Newtonsoft;
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

        private static readonly Newtonsoft.Json.JsonSerializer JsonSerializer = Newtonsoft.Json.JsonSerializer.Create(JsonNetSerializer.Settings);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Command"/>.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Command command) => ToDocument(command, CommandMediaType);


        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Message message) => ToDocument(message, MessageMediaType);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="Notification"/>.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <returns></returns>
        public static JsonDocument ToDocument(this Notification notification) => ToDocument(notification, NotificationMediaType);

        /// <summary>
        /// Creates a <see cref="JsonDocument"/> from the specified <see cref="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="envelope">The envelope.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static JsonDocument ToDocument<T>(T envelope, MediaType mediaType) where T : Envelope, new()
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (mediaType == null) throw new ArgumentNullException(nameof(mediaType));
            var jObject = JObject.FromObject(envelope, JsonSerializer);
            var dictionary = jObject.ToObject<Dictionary<string, object>>();
            return new JsonDocument(dictionary, mediaType);
        }

        /// <summary>
        /// Creates a <see cref="Message"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Message ToMessage(this JsonDocument jsonDocument) => (Message)ToEnvelope(jsonDocument, MessageMediaType);

        /// <summary>
        /// Creates a <see cref="Command"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Command ToCommand(this JsonDocument jsonDocument) => (Command)ToEnvelope(jsonDocument, CommandMediaType);

        /// <summary>
        /// Creates a <see cref="Notification"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <returns></returns>
        public static Notification ToNotification(this JsonDocument jsonDocument) => (Notification)ToEnvelope(jsonDocument, NotificationMediaType);

        /// <summary>
        /// Creates an <see cref="Envelope"/> from the specified <see cref="JsonDocument"/>.
        /// </summary>
        /// <param name="jsonDocument">The json document.</param>
        /// <param name="mediaType">Type of the media.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Unknown envelope media type</exception>
        public static Envelope ToEnvelope(this JsonDocument jsonDocument, MediaType mediaType)
        {
            Type envelopeType;
            if (mediaType.Equals(CommandMediaType)) envelopeType = typeof(Command);
            else if (mediaType.Equals(MessageMediaType)) envelopeType = typeof(Message);
            else if (mediaType.Equals(NotificationMediaType)) envelopeType = typeof(Notification);
            else throw new ArgumentException("Unknown envelope media type");

            var jObject = JObject.FromObject(jsonDocument, JsonSerializer);
            return (Envelope)jObject.ToObject(envelopeType, JsonSerializer);
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