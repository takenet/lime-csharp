using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lime.Protocol;

namespace Lime.Messaging.Contents
{
    public class Select : Document
    {
        public const string MIME_TYPE = "application/vnd.lime.select+json";
        public const string LATITUDE_KEY = "latitude";
        public const string LONGITUDE_KEY = "longitude";
        public const string ALTITUDE_KEY = "altitude";
        public const string COURSE_KEY = "course";
        public const string SPEED_KEY = "speed";
        public const string ACCURACY_KEY = "accuracy";

        public static readonly MediaType MediaType = MediaType.Parse(MIME_TYPE);

        public Select() 
            : base(MediaType)
        {
        }
    }
}
