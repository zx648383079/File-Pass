using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ZoDream.FileTransfer.Models;
using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class SettingViewModel : ObservableObject
    {

        public SettingViewModel()
        {
			ResetCommand = new AsyncRelayCommand(TapResetAsync);
            ClearCommand = new AsyncRelayCommand(TapClearAsync);
            Option = App.Repository.Option;
			PropertyChanged += (s, e) => 
			{
				if (e.PropertyName == nameof(IpTitle))
				{
					return;
				}
				IsUpdated = true;
			};
        }

		private bool IsUpdated = false;

		public ICommand ResetCommand { get; set; }
		public ICommand ClearCommand { get; set; }

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

        private string name = string.Empty;

		public string Name
		{
			get { return name; }
			set { 
				name = value;
				OnPropertyChanged();
			}
		}

		private string ip = string.Empty;

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

		private bool whenSaveCheckFolder;

		public bool WhenSaveCheckFolder {
			get { return whenSaveCheckFolder; }
			set { 
				whenSaveCheckFolder = value;
                OnPropertyChanged();
            }
		}



		private AppOption option = new();

		public AppOption Option {
			get {
				option.Ip = Ip;
				option.Port = Port;
				option.IsHideClient = IsHideClient;
				option.IsOpenLink = IsOpenLink;
				option.IsSaveFile = IsSaveFile;
				option.WhenSaveCheckFolder = WhenSaveCheckFolder;
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
				WhenSaveCheckFolder = option.WhenSaveCheckFolder;
            }
		}

		private async Task TapResetAsync()
		{
			await App.Repository.ResetAsync();
		}

        private async Task TapClearAsync()
        {
			await App.Repository.ClearMessageAsync();
        }


		public void AutoSave()
		{
			if (!IsUpdated)
			{
				return;
			}
			IsUpdated = true;
            App.Repository.ChangeOptionAsync(Option);
        }
    }
}
