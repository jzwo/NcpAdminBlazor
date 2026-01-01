using System.Runtime.CompilerServices;
using FastEndpoints;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace NcpAdminBlazor.ApiService.Endpoints.AiChat;

public record AiChatRequest(string Message);

/// <summary>
/// AI Chat endpoint that uses the registered IChatClient to process messages.
/// </summary>
public class AiChatEndpoint([FromKeyedServices("systemAssister")] AIAgent agent)
    : Endpoint<AiChatRequest>
{
    public override void Configure()
    {
        Get("/api/ai/chat");
        AllowAnonymous(); // Remove this or change to RequireAuthorization() for production
        Summary(s =>
        {
            s.Summary = "Send a message to the AI and get a response";
            s.Description = "Uses the configured AI chat client to generate a response for the given message";
            s.ExampleRequest = new AiChatRequest("Tell me a short story about a brave knight");
        });
    }

    public override async Task HandleAsync(AiChatRequest req, CancellationToken ct)
    {
        await Send.EventStreamAsync("update", EventStreamAsync(req.Message, ct), ct);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> EventStreamAsync(string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in agent.RunStreamingAsync(message, cancellationToken: ct))
        {
            yield return update.AsChatResponseUpdate();
        }
    }
}