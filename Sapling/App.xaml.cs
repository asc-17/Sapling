namespace Sapling
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "Sapling" };

#if WINDOWS
            // The Windows head exists only for quick testing, so it opens at phone proportions.
            window.Width = 430;
            window.Height = 880;
            window.MinimumWidth = 360;
            window.MaximumWidth = 560;
#endif

            return window;
        }
    }
}
