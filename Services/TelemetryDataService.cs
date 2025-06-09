using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TelemetryViewer.Models;
using System.Text.Json.Serialization;

namespace TelemetryViewer.Services
{
    public class TelemetryDataService
    {
        public async Task<LapData> LoadFromFileAsync(string path)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var stream = File.OpenRead(path);
            var lap = await JsonSerializer.DeserializeAsync<LapData>(stream, options);
            return lap!;
        }
    }
}
