using System;

namespace Lime.Protocol.Network
{
    public class EnvelopeTooLargeException : Exception
    {
        public EnvelopeTooLargeException()
            : base()
        {
        }

        public EnvelopeTooLargeException(string message)
            : base(message)
        {
        }

        public EnvelopeTooLargeException(string message, Envelope envelope)
            : base(message)
        {
            AddEnvelopePropertiesToData(envelope);
        }

        public EnvelopeTooLargeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public EnvelopeTooLargeException(string message, Envelope envelope, Exception innerException)
            : base(message, innerException)
        {
            AddEnvelopePropertiesToData(envelope);
        }

        private void AddEnvelopePropertiesToData(Envelope envelope)
        {
            Data["EnvelopeType"] = envelope.GetType().Name;
            Data[nameof(envelope.Id)] = envelope.Id?.ToString();
            Data[nameof(envelope.To)] = envelope.To?.ToString();
            Data[nameof(envelope.From)] = envelope.From?.ToString();
            Data[nameof(envelope.Pp)] = envelope.Pp?.ToString();
            Data[nameof(envelope.Metadata)] = envelope.Metadata;
        }
    }
}
