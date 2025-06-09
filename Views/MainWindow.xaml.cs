using System.Windows;
using TelemetryViewer.ViewModels;

namespace TelemetryViewer.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (DataContext is MainViewModel vm)
                    await vm.LoadTelemetryFromFileAsync("");
            };
        }
    }
}
