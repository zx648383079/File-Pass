using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class SettingViewModel : ObservableObject
    {

        public SettingViewModel()
        {
			Option = App.Repository.Option;
			PropertyChanged += (s, e) => 
			{
				App.Repository.ChangeOptionAsync(Option);
			};
        }

		public string IpTitle
		{
			get
			{
				return $"{Ip}:{Port}";
			}
			set
			{
				OnPropertyChanged();
			}
		}

        private string name;

		public string Name
		{
			get { return name; }
			set { 
				name = value;
				OnPropertyChanged();
			}
		}

		private string ip;

		public string Ip
		{
			get { return ip; }
			set { ip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IpTitle));
            }
		}


		private int port = Constants.DEFAULT_PORT;

		public int Port
		{
			get { return port; }
			set { port = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IpTitle));
            }
		}

		private bool isHideClient;

		public bool IsHideClient
		{
			get { return isHideClient; }
			set { isHideClient = value;
                OnPropertyChanged();
            }
		}

		private bool isOpenLink;

		public bool IsOpenLink
		{
			get { return isOpenLink; }
			set { isOpenLink = value;
                OnPropertyChanged();
            }
		}

		private bool isSaveFile;

		public bool IsSaveFile
		{
			get { return isSaveFile; }
			set { isSaveFile = value;
                OnPropertyChanged();
            }
		}


		private AppOption option;

		public AppOption Option {
			get {
				option.Ip = Ip;
				option.Port = Port;
				option.IsHideClient = IsHideClient;
				option.IsOpenLink = IsOpenLink;
				option.IsSaveFile = IsSaveFile;
				option.Name = Name;
				return option; 
			}
			set { 
				option = value;
                Ip = option.Ip;
                Port = option.Port;
                Name = option.Name;
                IsHideClient = option.IsHideClient;
                IsOpenLink = option.IsOpenLink;
                IsSaveFile = option.IsSaveFile;
            }
		}

	}
}
