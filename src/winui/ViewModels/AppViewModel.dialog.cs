using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRT.Interop;
using ZoDream.FileTransfer.Dialogs;

namespace ZoDream.FileTransfer.ViewModels
{
    internal partial class AppViewModel
    {
        public XamlRoot BaseXamlRoot => _baseWindow!.Content.XamlRoot;
        
        public void InitializePicker(object target)
        {
            InitializeWithWindow.Initialize(target, _baseWindowHandle);
        }

        public async Task<bool> ConfirmAsync(string message, string title = "提示")
        {
            var dialog = new ConfirmDialog
            {
                Title = title,
                Content = message
            };
            return await OpenDialogAsync(dialog) == ContentDialogResult.Primary;
        }

        public IAsyncOperation<ContentDialogResult> OpenDialogAsync(ContentDialog target)
        {
            target.XamlRoot = BaseXamlRoot;
            return target.ShowAsync();
        }
    }
}
