using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
