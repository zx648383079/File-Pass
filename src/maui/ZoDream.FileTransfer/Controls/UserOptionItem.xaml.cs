using System.Windows.Input;

namespace ZoDream.FileTransfer.Controls;

public partial class UserOptionItem : ContentView
{
	public UserOptionItem()
	{
		InitializeComponent();
	}


	public ICommand YesCommand
	{
		get { return (ICommand)GetValue(YesCommandProperty); }
		set { SetValue(YesCommandProperty, value); }
	}

	// Using a DependencyProperty as the backing store for YesCommand.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty YesCommandProperty =
		BindableProperty.Create(nameof(YesCommand), typeof(ICommand), 
			typeof(UserOptionItem), null);

    public ICommand NoCommand
    {
        get { return (ICommand)GetValue(NoCommandProperty); }
        set { SetValue(NoCommandProperty, value); }
    }

    // Using a DependencyProperty as the backing store for YesCommand.  This enables animation, styling, binding, etc...
    public static readonly BindableProperty NoCommandProperty =
        BindableProperty.Create(nameof(NoCommand), typeof(ICommand),
            typeof(UserOptionItem), null);



	public object CommandParameter
    {
		get { return GetValue(CommandParameterProperty); }
		set { SetValue(CommandParameterProperty, value); }
	}

	// Using a DependencyProperty as the backing store for CommandParameter.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), 
			typeof(UserOptionItem), null);

    private void YesBtn_Clicked(object sender, EventArgs e)
    {
		YesCommand?.Execute(CommandParameter);
    }

    private void NoBtn_Clicked(object sender, EventArgs e)
    {
		NoCommand?.Execute(CommandParameter);
    }
}