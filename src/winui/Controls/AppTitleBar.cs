using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ZoDream.FileTransfer.Controls
{
    public sealed class AppTitleBar : Control
    {
        public AppTitleBar()
        {
            this.DefaultStyleKey = typeof(AppTitleBar);
        }

        public Visibility BackVisible {
            get { return (Visibility)GetValue(BackVisibleProperty); }
            set { SetValue(BackVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackVisibleProperty =
            DependencyProperty.Register(nameof(BackVisible), typeof(Visibility),
                typeof(AppTitleBar), new PropertyMetadata(Visibility.Collapsed));



        public string Title {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(AppTitleBar), new PropertyMetadata(string.Empty));





        public ImageSource Icon {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(AppTitleBar), new PropertyMetadata(null));





        public ICommand BackCommand {
            get { return (ICommand)GetValue(BackCommandProperty); }
            set { SetValue(BackCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackCommandProperty =
            DependencyProperty.Register(nameof(BackCommand), typeof(ICommand), typeof(AppTitleBar), new PropertyMetadata(null));


        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            App.ViewModel.SetTitleBar(this, 50);
        }
    }
}
