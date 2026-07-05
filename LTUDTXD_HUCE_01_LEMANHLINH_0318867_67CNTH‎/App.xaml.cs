using System.Windows;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH;

public partial class App : Application
{
    private IEtabsService? _etabsService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _etabsService = new EtabsService();
        IBeamCalculationService calculationService = new BeamCalculationService();
        IReportExportService reportExportService = new ExcelReportExportService();
        IWindowService windowService = new WindowService();

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(_etabsService, calculationService, reportExportService, windowService)
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
