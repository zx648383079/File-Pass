using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Security.Cryptography;
using ZoDream.FileTransfer.Loggers;

namespace ZoDream.FileTransfer.Controls;

public class NotifyDialog : ContentView
{
	public NotifyDialog()
	{
		InnerPanel = new VerticalStackLayout();
        Content = InnerPanel;
        Loaded += NotifyDialog_Loaded;
        var logger = App.Repository.Logger;
        if (logger is EventLogger l)
        {
            l.OnLog += L_OnLog;
        }
    }

    private void NotifyDialog_Loaded(object? sender, EventArgs e)
    {
		IsBooted = true;
		var i = Math.Max(0, UnloadMessage.Count - 5);
		for (; i < UnloadMessage.Count; i++)
		{
            Add(LogLevel.Info, UnloadMessage[i]);
        }
		UnloadMessage.Clear();
	}

    private void L_OnLog(string message, LogLevel level)
    {
		if (!IsBooted)
		{
			if (level >= LogLevel.Info)
			{
				UnloadMessage.Add(message);
			}
			return;
		}
		MainThread.BeginInvokeOnMainThread(() => {
			Add(level, message);
		});
    }

	private bool IsBooted = false;
	private List<string> UnloadMessage = new();
    private readonly VerticalStackLayout InnerPanel;
	private int NotifyIndex = 0;
	private readonly int Timeout = 6 * 1000;
	private readonly Tuple<Color, Color>[] ColorItems =
	{
        new Tuple<Color, Color>(Colors.White, Colors.Black),
        new Tuple<Color, Color>(Colors.Green, Colors.White),
        new Tuple<Color, Color>(Colors.Red, Colors.White),
        new Tuple<Color, Color>(Colors.Gray, Colors.Black),
        new Tuple<Color, Color>(Colors.Yellow, Colors.Black),
        new Tuple<Color, Color>(Colors.Orange, Colors.White),
    };

    public void Add(View item)
    {
		if (InnerPanel.Children.Count > 5)
		{
			InnerPanel.RemoveAt(0);
        }
		item.Opacity = .1;
		InnerPanel.Add(item);
		OpacityTo(item, .1, 1);
		item.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() => {
                Remove(item);
            })
		});
		if (Timeout <= 0)
		{
			return;
		}
		Task.Factory.StartNew(() => {
			Thread.Sleep(Timeout);
			MainThread.BeginInvokeOnMainThread(() => {
				Remove(item);
			});
		});
    }

	public void Remove(View item)
	{
		if (!InnerPanel.Contains(item))
		{
			return;
		}
		OpacityTo(item, 1, .1, _ => {
            InnerPanel.Remove(item);
        });
	}

    public void Add(string message)
	{
        if (!IsBooted)
        {
            UnloadMessage.Add(message);
            return;
        }
        Add(CreateNotifyItem(message));
	}

    public void Add(string icon, string message)
    {
		if (!IsBooted)
		{
			UnloadMessage.Add(message);
			return;
		}
        Add(CreateNotifyItem(icon, message));
    }

    public void Add(LogLevel level, string message)
    {
		Add(level switch
		{
			LogLevel.Error or LogLevel.Fatal or LogLevel.Audit => "\ue73f",
			LogLevel.Warn => "\ue67c",
			LogLevel.Debug => "\ue6ff",
            _ => "\ue67f",
		}, message);
    }

	private Task<bool> OpacityTo(View view, double from, double to, Action<View>? cb = null, Easing? easing = null)
	{
        if (view == null)
		{
            throw new ArgumentNullException(nameof(view));
        }

        easing ??= Easing.Linear;
        var tcs = new TaskCompletionSource<bool>();
        var weakView = new WeakReference<VisualElement>(view);
        Action<double> opacity = f =>
        {
            if (weakView.TryGetTarget(out VisualElement? v))
                v.Opacity = f;
        };
        new Animation(opacity, from, to, easing: easing)
			.Commit(view, nameof(OpacityTo), 16, 250, easing, (f, a) => {
				tcs.SetResult(a);
                cb?.Invoke(view);
            });

        return tcs.Task;
    }

    private Tuple<Color, Color> GetFillAndColor()
	{
		NotifyIndex++;
		if (NotifyIndex >= ColorItems.Length * 100)
		{
			NotifyIndex = 0;
		}
		return ColorItems[NotifyIndex % ColorItems.Length];
	}

	private View CreateNotifyItem(string message)
	{
		var color = GetFillAndColor();
		return CreateOuterBox(color.Item1, CreateLabel(color.Item2, message));
	}

    private View CreateNotifyItem(string icon, string message)
    {
        var color = GetFillAndColor();
		var label = CreateLabel(color.Item2, message);
		Grid.SetColumn(label, 1);
        return CreateOuterBox(color.Item1, new Grid
        {
			ColumnDefinitions = new ColumnDefinitionCollection
			{
				new ColumnDefinition(new GridLength(20)),
				new ColumnDefinition(),
            },
            Children =
                {
                    new Label { Text = icon,
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = color.Item2,
                        FontFamily = "Iconfont" },
                    label
                }
        });
    }

	private Frame CreateOuterBox(Color color, View inner)
	{
        return new Frame
        {
            BackgroundColor = color,
            Shadow = GetShadow(),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            MinimumWidthRequest = 160,
            MaximumWidthRequest = Math.Max(200, Width),
            HorizontalOptions = LayoutOptions.End,
			Content = inner
        };
    }

	private View CreateLabel(Color color, string message)
	{
		return new Label
		{
			Text = message,
			TextColor = color,
			LineBreakMode = LineBreakMode.WordWrap,
			MaxLines = 4
		};
    }


    private Shadow GetShadow()
	{
		return new Shadow
		{
			Brush = new SolidColorBrush(Colors.Black),
			Offset = new Point(20, 20),
			Radius = 40,
			Opacity = .8f
		};
	}
	

}