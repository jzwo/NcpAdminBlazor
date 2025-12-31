using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;

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
        // 2. 使用 SseParser 解析流
        var parser = SseParser.Create(stream, (eventType, data) =>
        {
            if (data.IsEmpty) return string.Empty;
            var bytes = data.ToArray();
            return Encoding.UTF8.GetString(bytes);
        });
        await foreach (var data in parser.EnumerateAsync(cancellationToken).Select(item => item.Data)
                           .WithCancellation(cancellationToken))
        {
            if (string.IsNullOrEmpty(data))
                continue;

            yield return data;
        }
    }
}