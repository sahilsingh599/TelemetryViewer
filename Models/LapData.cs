using System.Collections.Generic;

namespace TelemetryViewer.Models
{
    public class LapData
    {
        public string driver { get; set; }
        public int lapNumber { get; set; }
        public List<TelemetryPoint> data { get; set; }
    }
}
