using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TelemetryViewer.Models;

namespace TelemetryViewer.Services
{
    public class OpenF1SectorData
    {
        public int session_key { get; set; }
        public int driver_number { get; set; }
        public int sector { get; set; }
        public double? sector_time { get; set; }
        public int lap_number { get; set; }
    }

    public class OpenF1TelemetryData
    {
        public int session_key { get; set; }
        public int driver_number { get; set; }
        public double? speed { get; set; }
        public double? throttle { get; set; }
        public double? brake { get; set; }
        public double? drs { get; set; }
        public double? time { get; set; }
        public int? lap_number { get; set; }
    }

    public class OpenF1LapSummary
    {
        public int? lap_number { get; set; }
        public double? lap_duration { get; set; }
        public int? driver_number { get; set; }
        public int? session_key { get; set; }
        // Add more fields as needed
    }

    public class OpenF1Service
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://api.openf1.org/v1/";

        public async Task<List<OpenF1SectorData>> GetSectorDataAsync(int sessionKey, int driverNumber)
        {
            string url = $"{BaseUrl}sectors?session_key={sessionKey}&driver_number={driverNumber}";
            var response = await client.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<OpenF1SectorData>>(response);
        }

        public async Task<List<OpenF1TelemetryData>> GetTelemetryDataAsync(int sessionKey, int driverNumber, int lapNumber)
        {
            string url = $"{BaseUrl}laps?session_key={sessionKey}&driver_number={driverNumber}&lap_number={lapNumber}";
            var response = await client.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<OpenF1TelemetryData>>(response);
        }

        public async Task<List<OpenF1LapSummary>> GetLapSummariesAsync(int sessionKey, int driverNumber)
        {
            string url = $"{BaseUrl}laps?session_key={sessionKey}&driver_number={driverNumber}";
            var response = await client.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<OpenF1LapSummary>>(response);
        }

        public async Task<List<SessionInfo>> GetSessionsAsync(string country, string sessionType, int year)
        {
            string url = $"{BaseUrl}sessions?country_name={Uri.EscapeDataString(country)}&session_name={Uri.EscapeDataString(sessionType)}&year={year}";
            var response = await client.GetStringAsync(url);
            return JsonSerializer.Deserialize<List<SessionInfo>>(response);
        }
    }
}
