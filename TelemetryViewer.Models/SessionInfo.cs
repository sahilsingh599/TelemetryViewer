namespace TelemetryViewer.Models
{
    public class SessionInfo
    {
        public int session_key { get; set; }
        public string country_name { get; set; }
        public int year { get; set; }
        public string session_name { get; set; }
        public override string ToString() => $"{year} {country_name} {session_name}";
    }
}
