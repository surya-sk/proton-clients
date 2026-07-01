using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Proton.Mail.Models;

namespace Proton.Mail.ViewModels;

public partial class ConversationViewModel : ObservableObject
{
    [ObservableProperty]
    private string subject = string.Empty;

    public ObservableCollection<MailMessageDetail> Messages { get; } = new();

    /// <summary>Populates the thread for the conversation navigated to. Sample data for now.</summary>
    public void Load(string conversationId)
    {
        Subject = conversationId switch
        {
            "2" => "Project sync tomorrow",
            "3" => "Re: Invoice #4471",
            _ => "Welcome to Proton Mail",
        };

        Messages.Clear();
        Messages.Add(new MailMessageDetail
        {
            Id = $"{conversationId}-1",
            SenderName = "Proton Team",
            SenderAddress = "team@proton.me",
            SentAt = DateTimeOffset.Now.AddHours(-2),
            Body = "This is the first message in the thread. Conversation threading will pull real message bodies once the Mail API client lands in Proton.Core.",
        });

        if (conversationId == "2")
        {
            Messages.Add(new MailMessageDetail
            {
                Id = $"{conversationId}-2",
                SenderName = "Alice Example",
                SenderAddress = "alice@example.com",
                SentAt = DateTimeOffset.Now.AddHours(-1),
                Body = "Can we move the sync to 3pm instead? I have a conflict at 2.",
            });
        }
    }
}
