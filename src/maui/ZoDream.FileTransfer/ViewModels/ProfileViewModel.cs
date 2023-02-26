using CommunityToolkit.Mvvm.ComponentModel;
using ZoDream.FileTransfer.Models;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class ProfileViewModel: ObservableObject, IQueryAttributable
    {
		public ProfileViewModel() 
		{
        }

		private UserItem user;

		public UserItem User
		{
			get { return user; }
			set { 
				user = value;
				OnPropertyChanged();
			}
		}

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("user", out object value))
			{
				User = App.Repository.ChatHub.Get((string)value);
			}
        }
    }
}
