using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace NcpAdminBlazor.Client.HttpClientServices;

public class AiChatService(HttpClient client)
{
    public async IAsyncEnumerable<string> SendMessageStreamAsync(string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestUrl = $"api/ai/chat?message={Uri.EscapeDataString(message)}";
        using var response =
            await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        // FastEndpoints Send.EventStreamAsync serializes payloads as JSON.
        // When the server yields a string, the SSE data is a JSON string token (quoted),
        // which would otherwise appear as: "你好""！"...
        var parser = SseParser.Create(stream, (eventType, data) => SseDataDecoder.Decode(data));

        await foreach (var data in parser.EnumerateAsync(cancellationToken).Select(item => item.Data)
                           .WithCancellation(cancellationToken))
        {
            if (string.IsNullOrEmpty(data))
                continue;

            yield return data;
        }
    }
}