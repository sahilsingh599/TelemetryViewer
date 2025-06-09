namespace TelemetryViewer.Models
{
    public class TelemetryPoint
    {
        public double Time { get; set; }
        public double Speed { get; set; }
        public double Throttle { get; set; }
        public double Brake { get; set; }
        public int Gear { get; set; }
    }
}
