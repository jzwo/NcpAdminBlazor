using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace NcpAdminBlazor.Client.Infrastructure.ApiProxies;

/// <summary>
/// AI 聊天服务
/// 通过 Server-Sent Events 接收流式响应
/// </summary>
public class AiChatService(HttpClient client)
{
    private const string ChatEndpointFormat = "api/ai/chat?message={0}";
    private const string UpdateEventType = "update";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 发送消息并以流式方式接收响应
    /// </summary>
    public async IAsyncEnumerable<ChatResponseUpdate> SendMessageStreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestUrl = string.Format(ChatEndpointFormat, Uri.EscapeDataString(message));

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parser = SseParser.Create(stream);

        await foreach (var item in parser.EnumerateAsync(cancellationToken))
        {
            if (!IsValidUpdateEvent(item))
            {
                continue;
            }

            var update = JsonSerializer.Deserialize<ChatResponseUpdate>(item.Data, JsonOptions);
            if (update is not null)
            {
                yield return update;
            }
        }
    }

    private static bool IsValidUpdateEvent(SseItem<string> item)
    {
        if (string.IsNullOrWhiteSpace(item.Data))
        {
            return false;
        }

        return string.IsNullOrEmpty(item.EventType) ||
               string.Equals(item.EventType, UpdateEventType, StringComparison.OrdinalIgnoreCase);
    }
}