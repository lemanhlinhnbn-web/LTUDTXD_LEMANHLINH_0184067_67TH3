using System.Windows;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public sealed class WindowService : IWindowService
{
    public void MinimizeMainWindow()
    {
        if (Application.Current.MainWindow is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    public void ToggleMaximizeMainWindow()
    {
        if (Application.Current.MainWindow is Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    public void CloseMainWindow()
    {
        Application.Current.MainWindow?.Close();
    }
}
