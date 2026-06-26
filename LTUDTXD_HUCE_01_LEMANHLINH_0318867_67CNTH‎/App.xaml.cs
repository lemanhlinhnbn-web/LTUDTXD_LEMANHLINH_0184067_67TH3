using System.Windows;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Services;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3;

public partial class App : Application
{
    private IEtabsService? _etabsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _etabsService = new EtabsService();

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(_etabsService)
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _etabsService?.Dispose();
        base.OnExit(e);
    }
}
