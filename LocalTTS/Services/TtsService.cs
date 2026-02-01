using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LocalTTS.Services;

public class TtsService
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(60) };
    private const string BaseUrl = "http://localhost:8880";

    public async Task<byte[]> SynthesizeAsync(string text)
    {
        var payload = new
        {
            model = "kokoro",
            voice = "af_heart",
            input = text
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"{BaseUrl}/v1/audio/speech", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
}
