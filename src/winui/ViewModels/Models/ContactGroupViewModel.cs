using System.Collections.ObjectModel;

namespace ZoDream.FileTransfer.ViewModels
{
    public class ContactGroupViewModel(string header) : ObservableCollection<ContactItemViewModel>
    {

        public string Header { get; set; } = header;
    }
}
