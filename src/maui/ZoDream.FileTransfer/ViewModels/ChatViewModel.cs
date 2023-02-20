using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
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
            App.Repository.NewMessage += Repository_NewMessage;
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
			var success = await App.Repository.SendTextAsync(User.Id, Content);
			MessageItems.Add(new TextMessageItem()
			{
				IsSender = true,
				Content = Content,
				CreatedAt = DateTime.Now,
				IsSuccess = success
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
				_ = LoadMessageAsync();
			}
        }

		private async Task LoadMessageAsync()
		{
			var items = await App.Repository.LoadMessageAsync(User);
			if (items == null)
			{
				return;
			}
            foreach (var item in items)
            {
				MessageItems.Add(item);
            }
			
        }

        private void Repository_NewMessage(string userId, MessageItem message)
        {
            if (userId != User?.Id)
			{
				return;
			}
			message.IsSender = false;
			message.IsSuccess = true;
			MessageItems.Add(message);
        }
    }
}
