using System.Collections.ObjectModel;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public sealed class BeamParametersViewModel : PageViewModelBase
{
    public BeamParametersViewModel(ObservableCollection<BeamParameterItem> parameters)
        : base("Thông số dầm thép chữ I", "Nhập hình học, vật liệu và nội lực thiết kế theo TCVN 5575:2012.")
    {
        Parameters = parameters;
    }

    public ObservableCollection<BeamParameterItem> Parameters { get; }
}
