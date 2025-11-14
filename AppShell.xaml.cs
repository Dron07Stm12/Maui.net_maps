namespace MauiGpsDemo
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("aboutpage", typeof(AboutPage));
        }
    }
}
