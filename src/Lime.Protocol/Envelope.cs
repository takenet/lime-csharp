using Lime.Protocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lime.Protocol
{
    /// <summary>
    /// Base class to all 
    /// communication documents
    /// </summary>
    [DataContract(Namespace = "http://limeprotocol.org/2014")]
    public abstract class Envelope : IJsonWritable, IJsonSerializable
    {
        public const string ID_KEY = "id";
        public const string FROM_KEY = "from";
        public const string PP_KEY = "pp";
        public const string TO_KEY = "to";
        public const string METADATA_KEY = "metadata";

        #region Constructor

        public Envelope()
            : this(Guid.NewGuid())
        {
        }

        protected Envelope(Guid? id)
        {
            this.Id = id;
        }

        #endregion

        /// <summary>
        /// Unique identifier of the envelope
        /// </summary>
        [DataMember(Name = ID_KEY)]
        public Guid? Id { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = FROM_KEY)]
        public Node From { get; set; }

        /// <summary>
        /// Sender of the envelope
        /// </summary>
        [DataMember(Name = PP_KEY)]
        public Node Pp { get; set; }

        /// <summary>
        /// Destination of the envelope
        /// </summary>
        [DataMember(Name = TO_KEY)]
        public Node To { get; set; }

        /// <summary>
        /// Additional information to be 
        /// delivered with the envelope
        /// </summary>
        [DataMember(Name = METADATA_KEY)]
        public IDictionary<string, string> Metadata { get; set; }


        #region Serialization Members

        #region IJsonWritable Members

        /// <summary>
        /// Writes the JSON representation
        /// of the object to the writer
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteJson(IJsonWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.WriteProperty(ID_KEY, this.Id);
            writer.WriteProperty(FROM_KEY, this.From);
            writer.WriteProperty(PP_KEY, this.Pp);
            writer.WriteProperty(TO_KEY, this.To);
            writer.WriteDictionaryProperty(METADATA_KEY, this.Metadata);
        }

        #endregion

        #region IJsonSerializable Members

        /// <summary>
        /// Serializes the instance value
        /// to a JSON string representation
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            using (var writer = new JsonWriter())
            {
                WriteJson(writer);
                return writer.ToString();
            }
        }

        #endregion

        protected static void PopulateFromJsonObject(Envelope envelope, JsonObject jsonObject)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException("envelope");
            }

            if (jsonObject == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            envelope.Id = jsonObject.GetValueOrDefault<Guid?>(Envelope.ID_KEY, v => new Guid((string)v));
            envelope.From = jsonObject.GetValueOrDefault(Envelope.FROM_KEY, v => Node.Parse((string)v));
            envelope.Pp = jsonObject.GetValueOrDefault(Envelope.PP_KEY, v => Node.Parse((string)v));
            envelope.To = jsonObject.GetValueOrDefault(Envelope.TO_KEY, v => Node.Parse((string)v));
            envelope.Metadata = jsonObject.GetValueOrDefault<IDictionary<string, string>>(Envelope.METADATA_KEY, v => ((IDictionary<string, object>)v).ToDictionary(e => e.Key, e => (string)e.Value));
        }

        #endregion

    }
}