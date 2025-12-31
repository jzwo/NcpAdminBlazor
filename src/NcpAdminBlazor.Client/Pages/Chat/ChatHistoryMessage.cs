using Microsoft.Extensions.AI;

namespace NcpAdminBlazor.Client.Pages.Chat;

public sealed record ChatHistoryMessage(ChatMessage Message, DateTimeOffset CreatedAt);
