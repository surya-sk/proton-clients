namespace Proton.Mail.Models;

// Placeholder view-layer models. These stand in for what will eventually be populated from
// Proton.Core's Mail API client (conversations/messages/labels) once that layer exists; for now
// the view models below seed them with sample data so the UI shell is exercisable on its own.

public sealed class ConversationSummary
{
    public required string Id { get; init; }
    public required string SenderName { get; init; }
    public required string Subject { get; init; }
    public required string Preview { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public bool IsUnread { get; init; }
    public int MessageCount { get; init; } = 1;

    public string UnreadMarker => IsUnread ? "• " : string.Empty;
}

public sealed class MailMessageDetail
{
    public required string Id { get; init; }
    public required string SenderName { get; init; }
    public required string SenderAddress { get; init; }
    public required DateTimeOffset SentAt { get; init; }
    public required string Body { get; init; }
}

public sealed class MailFolder
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int UnreadCount { get; init; }
}
