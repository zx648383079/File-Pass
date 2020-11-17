using System.ComponentModel;

namespace FilePass
{
    public class FileItem : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private string status;

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Status"));
                }
            }
        }


        public string FileName { get; set; }

        private long length;

        public long Length
        {
            get { return length; }
            set { 
                length = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Length"));
                }
            }
        }

        private long progress;

        public long Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Progress"));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
