using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Models
{
    public class FilePickerOption: BindableObject
    {
        public string Name { get; set; }

        public string FileName { get; set; }

        public string Icon { get; set; }

        private bool isFolder;

        public bool IsFolder {
            get { return isFolder; }
            set { 
                isFolder = value;
                Icon = value ? "\ue696" : "\ue68d";
            }
        }


        private bool isChecked;

        public bool IsChecked {
            get { return isChecked; }
            set { 
                isChecked = value;
                OnPropertyChanged();
            }
        }
    }
}
