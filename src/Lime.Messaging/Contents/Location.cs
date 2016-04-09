using System.Runtime.Serialization;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    /// <summary>
    /// Represents a geographic location information.
    /// </summary>
    /// <seealso cref="Lime.Protocol.Document" />
    public class Location : Document    
    {
        public const string MIME_TYPE = "application/vnd.lime.location+json";
        public const string LATITUDE_KEY = "latitude";
        public const string LONGITUDE_KEY = "longitude";
        public const string ALTITUDE_KEY = "altitude";        
        public const string COURSE_KEY = "course";
        public const string SPEED_KEY = "speed";
        public const string ACCURACY_KEY = "accuracy";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> class.
        /// </summary>
        public Location()
            : base(MediaType)
        {
        }

        /// <summary>
        /// Gets or sets the latitude, in degrees.
        /// Latitude can range from -90.0 to 90.0.Latitude is measured in degrees north or south from the equator. Positive values are north of the equator and negative values are south of the equator.        
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        /// <seealso cref="https://msdn.microsoft.com/en-us/library/system.device.location.geocoordinate.latitude(v=vs.110).aspx"/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Geographic_coordinate_system"/>
        [DataMember(Name = LATITUDE_KEY)]
        public double? Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude, in degrees.
        /// The longitude can range from -180.0 to 180.0.Longitude is measured in degrees east or west of the prime meridian. Negative values are west of the prime meridian, and positive values are east of the prime meridian.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        /// <seealso cref="https://msdn.microsoft.com/en-us/library/system.device.location.geocoordinate.longitude(v=vs.110).aspx"/>
        /// <seealso cref="https://en.wikipedia.org/wiki/Geographic_coordinate_system"/>
        [DataMember(Name = LONGITUDE_KEY)]
        public double? Longitude { get; set; }

        /// <summary>
        /// Gets or sets the altitude, in meters.
        /// </summary>
        /// <value>
        /// The altitude.
        /// </value>
        [DataMember(Name = ALTITUDE_KEY)]
        public double? Altitude { get; set; }

        /// <summary>
        /// Gets or sets the course, in degrees.
        /// The course can range from 0 to 360.
        /// </summary>
        /// <value>
        /// The course.
        /// </value>
        [DataMember(Name = COURSE_KEY)]
        public int? Course { get; set; }

        /// <summary>
        /// Gets or sets the speed, in meters per second.
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        [DataMember(Name = SPEED_KEY)]
        public double? Speed { get; set; }

        /// <summary>
        /// Gets or sets the location accuracy, in meters.
        /// </summary>
        /// <value>
        /// The accuracy.
        /// </value>
        [DataMember(Name = ACCURACY_KEY)]
        public double? Accuracy { get; set; }
    }
}
