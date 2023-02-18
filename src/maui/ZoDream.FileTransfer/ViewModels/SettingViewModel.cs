using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.ViewModels
{
    internal class SettingViewModel : ObservableObject
    {

        public SettingViewModel()
        {
            
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
            }
		}


		private int port;

		public int Port
		{
			get { return port; }
			set { port = value;
                OnPropertyChanged();
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

	}
}
