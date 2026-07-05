using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
