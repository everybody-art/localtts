using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LocalTTS.Services;

public class TtsService
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly AppSettings _settings;

    public TtsService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<byte[]> SynthesizeAsync(string text)
    {
        var payload = new
        {
            model = "kokoro",
            voice = _settings.Voice,
            input = text
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"http://localhost:{_settings.Port}/v1/audio/speech";
        var response = await _client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync();
    }
}
