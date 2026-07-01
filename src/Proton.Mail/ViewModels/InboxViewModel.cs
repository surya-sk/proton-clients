using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Proton.Mail.Models;

namespace Proton.Mail.ViewModels;

public partial class InboxViewModel : ObservableObject
{
    public ObservableCollection<ConversationSummary> Conversations { get; } = new();

    [ObservableProperty]
    private ConversationSummary? selectedConversation;

    public InboxViewModel()
    {
        // Sample data until this is wired to a Mail-specific Proton.Core API client.
        Conversations.Add(new ConversationSummary
        {
            Id = "1",
            SenderName = "Proton Team",
            Subject = "Welcome to Proton Mail",
            Preview = "Thanks for setting up the WinUI 3 client scaffold...",
            ReceivedAt = DateTimeOffset.Now.AddHours(-1),
            IsUnread = true,
        });
        Conversations.Add(new ConversationSummary
        {
            Id = "2",
            SenderName = "Alice Example",
            Subject = "Project sync tomorrow",
            Preview = "Can we move the sync to 3pm instead? I have...",
            ReceivedAt = DateTimeOffset.Now.AddHours(-5),
            MessageCount = 3,
        });
        Conversations.Add(new ConversationSummary
        {
            Id = "3",
            SenderName = "Bob Example",
            Subject = "Re: Invoice #4471",
            Preview = "Confirmed, payment has been sent. Let me know...",
            ReceivedAt = DateTimeOffset.Now.AddDays(-1),
        });
    }
}
