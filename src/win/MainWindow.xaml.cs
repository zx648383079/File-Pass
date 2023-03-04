using System.Collections.Generic;
using System.Windows;
using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainViewModel? ViewModel => DataContext as MainViewModel;


        private void IpTb_GotFocus(object sender, RoutedEventArgs e)
        {
            IpTb.Focus();
            IpTb.SelectAll();
        }


        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }

        private void FileBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;
        }

        private void FileBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            var items = (IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop);
            if (items == null)
            {
                return;
            }
            ViewModel?.DragFile(items);
        }

    }
}
