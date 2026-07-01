using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Proton.Mail.Models;

namespace Proton.Mail.ViewModels;

public partial class FoldersViewModel : ObservableObject
{
    public ObservableCollection<MailFolder> Folders { get; } = new();

    public FoldersViewModel()
    {
        Folders.Add(new MailFolder { Id = "inbox", Name = "Inbox", UnreadCount = 2 });
        Folders.Add(new MailFolder { Id = "sent", Name = "Sent" });
        Folders.Add(new MailFolder { Id = "drafts", Name = "Drafts", UnreadCount = 1 });
        Folders.Add(new MailFolder { Id = "archive", Name = "Archive" });
        Folders.Add(new MailFolder { Id = "trash", Name = "Trash" });
        Folders.Add(new MailFolder { Id = "custom-work", Name = "Work" });
    }
}
