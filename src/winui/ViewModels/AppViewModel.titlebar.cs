using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics;
using ZoDream.FileTransfer.Controls;
using Windows.UI;
using Windows.Foundation;
using ZoDream.FileTransfer.Pages;

namespace ZoDream.FileTransfer.ViewModels
{
    internal partial class AppViewModel
    {
        public AppTitleBar? TitleBar { get; private set; }

        public ICommand? BackCommand { get; private set; }

        public bool BackEnabled {
            set {
                if (TitleBar is not null)
                {
                    TitleBar.BackVisible = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public void InitializeTitleBar()
        {
            if (_appWindow is null)
            {
                return;
            }
            var bar = _appWindow.TitleBar;
            bar.ExtendsContentIntoTitleBar = true;
            if (Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                bar.BackgroundColor = Colors.White;
                bar.InactiveBackgroundColor = Colors.White;
                bar.ButtonBackgroundColor = Color.FromArgb(255, 240, 243, 249);
                bar.ButtonForegroundColor = Colors.DarkGray;
                bar.ButtonHoverBackgroundColor = Colors.LightGray;
                bar.ButtonHoverForegroundColor = Colors.DarkGray;
                bar.ButtonPressedBackgroundColor = Colors.Gray;
                bar.ButtonPressedForegroundColor = Colors.DarkGray;
                bar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 240, 243, 249);
                bar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                bar.BackgroundColor = Color.FromArgb(255, 240, 243, 249);
                bar.InactiveBackgroundColor = Colors.Black;
                bar.ButtonBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                bar.ButtonForegroundColor = Colors.White;
                bar.ButtonHoverBackgroundColor = Color.FromArgb(255, 20, 20, 20);
                bar.ButtonHoverForegroundColor = Colors.White;
                bar.ButtonPressedBackgroundColor = Color.FromArgb(255, 40, 40, 40);
                bar.ButtonPressedForegroundColor = Colors.White;
                bar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 32, 32, 32);
                bar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }

        internal void SetTitleBar(UIElement titleBar)
        {
            _baseWindow.ExtendsContentIntoTitleBar = true;
            _baseWindow.SetTitleBar(titleBar);
            if (titleBar is AppTitleBar obj)
            {
                SetTitleBar(obj);
            }
        }

        internal void SetTitleBar(AppTitleBar titleBar)
        {
            TitleBar = titleBar;
            TitleBar.BackCommand = BackCommand;
        }

        private void TapBack()
        {
            _rootFrame.GoBack();
        }


        private void Router_RouteChanged(object sender, RoutedEventArgs e)
        {
            if (TitleBar is null)
            {
                return;
            }
            TitleBar.BackVisible = _rootFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        }

        internal void SetTitleBar(FrameworkElement titleBar, double buttonWidth)
        {
            SetTitleBar(titleBar);
            SetNonClientRegionOnTitleBar(0, 0, buttonWidth, 48);
        }
        /// <summary>
        /// 设置 title bar 可点击控件
        /// </summary>
        /// <param name="exceptElements"></param>
        internal void SetNonClientRegionElementsOnTitleBar(params FrameworkElement[] exceptElements)
        {
            var dpiScaleFactor = GetDpiScaleFactorFromWindow();

            if (dpiScaleFactor <= 0)
            {
                return;
            }
            var elementPoint = TitleBar!.TransformToVisual(_baseWindow.Content).TransformPoint(new Point(0, 0));
            var draggableRect = new RectInt32((int)(elementPoint.X * dpiScaleFactor),
                (int)(elementPoint.Y * dpiScaleFactor),
                (int)(TitleBar.ActualWidth * dpiScaleFactor), (int)(TitleBar.ActualHeight * dpiScaleFactor));

            if (InputNonClientPointerSource.GetForWindowId(_appWindow.Id) is
                InputNonClientPointerSource nonClientSource)
            {
                nonClientSource.ClearRegionRects(NonClientRegionKind.Caption);
                nonClientSource.SetRegionRects(NonClientRegionKind.Caption,
                    [draggableRect]);

                if (exceptElements.Length > 0)
                {
                    var exceptRectItems = new List<RectInt32>(exceptElements.Length);

                    foreach (var ExceptElement in exceptElements)
                    {
                        var ExceptElementPoint = ExceptElement.TransformToVisual(_baseWindow.Content).TransformPoint(new Point(0, 0));
                        exceptRectItems.Add(new RectInt32((int)(ExceptElementPoint.X * dpiScaleFactor),
                            (int)(ExceptElementPoint.Y * dpiScaleFactor),
                            (int)(ExceptElement.ActualWidth * dpiScaleFactor),
                            (int)(ExceptElement.ActualHeight * dpiScaleFactor)));
                    }

                    nonClientSource.ClearRegionRects(NonClientRegionKind.Passthrough);
                    nonClientSource.SetRegionRects(NonClientRegionKind.Passthrough, exceptRectItems.Distinct().ToArray());
                }
            }
        }

        internal void SetNonClientRegionOnTitleBar(double x, double y, double width, double height)
        {
            var dpiScaleFactor = GetDpiScaleFactorFromWindow();
            if (dpiScaleFactor <= 0)
            {
                return;
            }
            var elementPoint = TitleBar!.TransformToVisual(_baseWindow.Content).TransformPoint(new Point(0, 0));
            var draggableRect = new RectInt32((int)Math.Round(elementPoint.X * dpiScaleFactor),
                (int)Math.Round(elementPoint.Y * dpiScaleFactor),
                (int)Math.Round(TitleBar.ActualWidth * dpiScaleFactor),
                (int)Math.Round(TitleBar.ActualHeight * dpiScaleFactor));

            if (InputNonClientPointerSource.GetForWindowId(_appWindow.Id) is
                InputNonClientPointerSource nonClientSource)
            {
                nonClientSource.ClearRegionRects(NonClientRegionKind.Passthrough);
                nonClientSource.SetRegionRects(NonClientRegionKind.Passthrough,
                    [new((int)Math.Round((elementPoint.X + x) * dpiScaleFactor),
                    (int)Math.Round((elementPoint.Y + y) * dpiScaleFactor),
                    (int)Math.Round(width * dpiScaleFactor),
                    (int)Math.Round(height * dpiScaleFactor)
                    )]);
            }
        }
    }
}
