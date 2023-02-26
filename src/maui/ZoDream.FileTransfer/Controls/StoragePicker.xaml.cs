using ZoDream.FileTransfer.ViewModels;

namespace ZoDream.FileTransfer.Controls;

public partial class StoragePicker : ContentView
{
	public StoragePicker()
	{
		InitializeComponent();
        (BindingContext as PickerViewModel).Context = this;
    }

	public bool IsFolderPicker {
		get { return (bool)GetValue(IsFolderPickerProperty); }
		set { SetValue(IsFolderPickerProperty, value); }
	}

	// Using a DependencyProperty as the backing store for IsFolderPicker.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty IsFolderPickerProperty =
        BindableProperty.Create(nameof(IsFolderPicker), typeof(bool), typeof(StoragePicker), false);




	public bool IsMultiple {
		get { return (bool)GetValue(IsMultipleProperty); }
		set { SetValue(IsMultipleProperty, value); }
	}

	// Using a DependencyProperty as the backing store for IsMultiple.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty IsMultipleProperty =
        BindableProperty.Create(nameof(IsMultiple), typeof(bool), typeof(StoragePicker), false);



	/// <summary>
	/// 通过正则表达式筛选
	/// </summary>
	public string Filter {
		get { return (string)GetValue(FilterProperty); }
		set { SetValue(FilterProperty, value); }
	}

	// Using a DependencyProperty as the backing store for Filter.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty FilterProperty =
        BindableProperty.Create("Filter", typeof(string), typeof(StoragePicker), string.Empty);



}