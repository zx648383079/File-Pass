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
            if (query.ContainsKey("user"))
			{
				User = App.Repository.Get((string)query["user"]);
			}
        }
    }
}
