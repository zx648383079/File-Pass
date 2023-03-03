using ZoDream.FileTransfer.Repositories;

namespace ZoDream.FileTransfer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Repository ??= new AppRepository();

            MainPage = new AppShell();
            _ = Repository.InitializeAsync(VersionTracking.Default.IsFirstLaunchEver);
        }


        protected override void CleanUp()
        {
            Repository?.Dispose();
            base.CleanUp();
        }

        internal static AppRepository Repository { get; private set; }
    }
}