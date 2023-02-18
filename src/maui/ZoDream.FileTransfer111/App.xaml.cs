namespace ZoDream.FileTransfer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Current.RequestedThemeChanged += (s, a) =>
            {
                var mergedDictionaries = Current.Resources.MergedDictionaries;
                if (mergedDictionaries != null)
                {
                    mergedDictionaries.Clear();
                    mergedDictionaries.Add(a.RequestedTheme == AppTheme.Dark ? new Skins.DarkTheme() : new Skins.LightTheme());
                }
            };
            MainPage = new MainPage();
        }
    }
}