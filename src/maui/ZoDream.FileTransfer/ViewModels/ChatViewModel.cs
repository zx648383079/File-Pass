﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZoDream.FileTransfer.Controls;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Network;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class ChatViewModel: ObservableObject, IQueryAttributable
    {
		public ChatViewModel() 
		{
			ProfileCommand = new AsyncRelayCommand(GoToProfile);
			SendCommand = new AsyncRelayCommand(SendAsync);
			MoreButtonCommand = new AsyncRelayCommand<MessageMoreItem>(TapMoreButtonAsync);
			MoreCommand = new RelayCommand(TapMore);
            MessageCommand = new AsyncRelayCommand<MessageTapEventArg>(TapMessageAsync);
            App.Repository.ChatHub.NewMessage += Repository_NewMessage;
			MoreItems.Add(new MessageMoreItem("image", "发送图片", "\ue68b"));
			MoreItems.Add(new MessageMoreItem("video", "发送视频", "\ue68c"));
			MoreItems.Add(new MessageMoreItem("file", "发送文件", "\ue68d"));
			MoreItems.Add(new MessageMoreItem("voice", "发送语音", "\ue6e0"));
			MoreItems.Add(new MessageMoreItem("camera", "视频通话", "\ue639"));
			MoreItems.Add(new MessageMoreItem("folder", "发送文件夹", "\ue696"));
			MoreItems.Add(new MessageMoreItem("sync", "同步文件夹", "\ue67b"));
			MoreItems.Add(new MessageMoreItem("user", "推荐好友", "\ue751"));
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

		private ObservableCollection<MessageMoreItem> moreItems = new();

		public ObservableCollection<MessageMoreItem> MoreItems {
			get => moreItems;
			set {
				moreItems = value;
				OnPropertyChanged();
			}
		}

		private bool moreVisible;

		public bool MoreVisible {
			get { return moreVisible; }
			set { 
				moreVisible = value;
				OnPropertyChanged();
			}
		}

        private bool moreToolVisible = true;

        public bool MoreToolVisible {
            get { return moreToolVisible; }
            set {
                moreToolVisible = value;
                OnPropertyChanged();
            }
        }

        private bool moreIconVisible;

        public bool MoreIconVisible {
            get { return moreIconVisible; }
            set {
                moreIconVisible = value;
				MoreToolVisible = !value;
                OnPropertyChanged();
            }
        }

		public StoragePicker StoragePicker { get; internal set; }
		public UserPicker UserPicker { get; internal set; }

        public ICommand ProfileCommand { get; private set; }
        public ICommand MoreCommand { get; private set; }
        public ICommand MoreButtonCommand { get; private set; }
        public ICommand SendCommand { get; private set; }

		public ICommand MessageCommand { get; private set; }

        private async Task TapMessageAsync(MessageTapEventArg arg)
		{
			switch (arg.EventType) {
				case MessageTapEvent.Cancel:
					await App.Repository.ChatHub.CancelMessageAsync(User, arg.Data);
					break;
				case MessageTapEvent.Confirm:
					
                    await App.Repository.ChatHub.ConfirmMessageAsync(User, arg.Data);
                    break;
				default:
					break;
			}
		}

		private void TapMore()
		{
			MoreVisible = !MoreVisible;
		}

        private async Task TapMoreButtonAsync(MessageMoreItem button)
		{
			switch (button.Name)
			{
				case "image":
				case "file":
				case "video":
                    await PickFileAsync();
					break;
                case "folder":
                    await PickFolderAsync();
                    break;
                case "user":
                    await PickUserAsync();
                    break;
                default:
					break;
			}
		}

        private async Task SendAsync()
		{
			if (string.IsNullOrWhiteSpace(Content))
			{
				return;
			}
			MessageItems.Add(new TextMessageItem()
			{
				IsSender = true,
				Content = content,
				CreatedAt = DateTime.Now,
				IsSuccess = false
			});
			var message = await App.Repository.ChatHub.SendTextAsync(User, Content);
			MessageItems.Add(message);
			Content = string.Empty;
		}

        private async Task PingAsync()
        {
            var message = await App.Repository.ChatHub.SendPingAsync(User);
            MessageItems.Add(message);
        }

		private async Task PickFileAsync()
		{
			var res = await FilePicker.Default.PickMultipleAsync();
            foreach (var item in res)
            {
                MessageItems.Add(new FileMessageItem()
                {
                    IsSender = true,
                    FileName = item.FileName,
                    CreatedAt = DateTime.Now,
                    IsSuccess = false
                });
                var message = await App.Repository.ChatHub.SendFileAsync(User, item.FileName);
                MessageItems.Add(message);
            }
        }

		private async Task PickFolderAsync()
		{
            StoragePicker.IsFolderPicker = true;
			if (!await StoragePicker.ShowAsync())
			{
				return;
			}
			var folder = StoragePicker.SelectedItem;
        }

        private async Task PickUserAsync()
        {
            UserPicker.Items = App.Repository.ChatHub.UserItems.Where(i => i.Id != User.Id)
				.Select(i => new UserInfoOption(i)).ToList();
            if (!await UserPicker.ShowAsync())
            {
                return;
            }
            var users = UserPicker.SelectedItems;
        }

        private async Task SyncFolderAsync()
		{
            StoragePicker.IsFolderPicker = true;
            if (!await StoragePicker.ShowAsync())
            {
                return;
            }
            var folder = StoragePicker.SelectedItem;
        }

        private async Task GoToProfile()
		{
            await Shell.Current.GoToAsync("Profile");
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("user"))
			{
				User = App.Repository.ChatHub.Get((string)query["user"]);
				Title = $"与 {User.Name} 聊天中";
				_ = LoadMessageAsync();
			}
        }

		private async Task LoadMessageAsync()
		{
			var items = await App.Repository.DataHub.GetMessagesAsync(User);
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
