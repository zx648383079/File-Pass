using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class ChatViewModel: ObservableObject, IQueryAttributable
    {
		public ChatViewModel() 
		{
			ProfileCommand = new AsyncRelayCommand(GoToProfile);
			SendCommand = new AsyncRelayCommand(SendAsync);
        }

		private UserItem User;

		private string title = "加载中。。。";

		public string Title
		{
			get { return title; }
			set { 
				title = value;
				OnPropertyChanged();
			}
		}


		private string content = string.Empty;

		public string Content
		{
			get { return content; }
			set { 
				content = value;
				OnPropertyChanged();
			}
		}


		private ObservableCollection<MessageItem> messageItems = new();

		public ObservableCollection<MessageItem> MessageItems
		{
			get => messageItems;
			set
			{
				messageItems = value;
				OnPropertyChanged();
			}
		}


		public ICommand ProfileCommand { get; private set; }
		public ICommand SendCommand { get; private set; }

		private async Task SendAsync()
		{
			if (string.IsNullOrWhiteSpace(Content))
			{
				return;
			}
			MessageItems.Add(new TextMessageItem()
			{
				IsSender = true,
				Content = Content,
				CreatedAt = DateTime.Now,
			});
			Content = string.Empty;
		}

        private async Task GoToProfile()
		{
            await Shell.Current.GoToAsync("Profile");
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("user"))
			{
				User = App.Repository.Get((string)query["user"]);
				Title = $"与 {User.Name} 聊天中";
			}
        }
    }
}
