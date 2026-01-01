using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Net.ServerSentEvents;
using Microsoft.Extensions.AI;

namespace NcpAdminBlazor.Client.HttpClientServices;

public class AiChatService(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async IAsyncEnumerable<ChatResponseUpdate> SendMessageStreamAsync(string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestUrl = $"api/ai/chat?message={Uri.EscapeDataString(message)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var response =
            await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var parser = SseParser.Create(stream);
        await foreach (var item in parser.EnumerateAsync(cancellationToken))
        {
            if (!string.IsNullOrEmpty(item.EventType) &&
                !string.Equals(item.EventType, "update", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Data))
            {
                continue;
            }

            var update = JsonSerializer.Deserialize<ChatResponseUpdate>(item.Data, JsonOptions);
            if (update is null)
            {
                continue;
            }

            yield return update;
        }
    }
}