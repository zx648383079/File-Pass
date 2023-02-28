#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
#endif

namespace ZoDream.FileTransfer.Controls
{
    public partial class LongPressedGestureRecognizer: GestureRecognizer
    {


        protected override void OnParentChanged()
        {
            base.OnParentChanged();
            if (Parent is null)
            {
                return;
            }
            Parent.HandlerChanged += Parent_HandlerChanged;

        }

        private void Parent_HandlerChanged(object sender, EventArgs e)
        {
            if (Parent is null || Parent.Handler is null)
            {
                return;
            }
#if WINDOWS
            var box = Parent.Handler.PlatformView as FrameworkElement;
            if (box == null)
            {
                return;
            }

            box.PointerPressed += OnMouseDown;
            box.PointerReleased += OnMouseUp;
#endif
        }
#if WINDOWS
        private void OnMouseDown(object sender, PointerRoutedEventArgs arg)
        {
            OnMouseDown();
        }

        private void OnMouseUp(object sender, PointerRoutedEventArgs arg)
        {
            OnMouseUp();
        }
#endif
    }
}