using System.Windows.Input;

namespace ZoDream.FileTransfer.Controls
{
    public partial class LongPressedGestureRecognizer: GestureRecognizer
    {

        private DateTime LastPressedTime = DateTime.MinValue;

        public int PressedScale {
            get { return (int)GetValue(PressedScaleProperty); }
            set { SetValue(PressedScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PressedScale.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty PressedScaleProperty =
            BindableProperty.Create(nameof(PressedScale), typeof(int), typeof(LongPressedGestureRecognizer), 3);



        public ICommand Command {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LongPressedGestureRecognizer), null);



        public object CommandParameter {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommandProperty.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressedGestureRecognizer), null);


        public void OnMouseDown()
        {
            LastPressedTime = DateTime.Now;
        }

        public void OnMouseUp()
        {
            var now = DateTime.Now;
            var diff = now - LastPressedTime;
            if (diff.TotalSeconds > PressedScale)
            {
                Command?.Execute(CommandParameter);
            }
        }
    }
}
