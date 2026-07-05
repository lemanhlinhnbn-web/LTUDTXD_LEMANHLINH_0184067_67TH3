using System.Collections.ObjectModel;
using System.Windows.Input;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.ViewModels;

public sealed class BeamCheckViewModel : PageViewModelBase
{
    public BeamCheckViewModel(
        ObservableCollection<BeamCheckResult> strengthChecks,
        ObservableCollection<BeamCheckResult> stabilityChecks,
        ICommand runStrengthCheckCommand,
        ICommand runStabilityCheckCommand)
        : base("Kiểm tra dầm thép", "Kiểm tra điều kiện bền, điều kiện ổn định và độ mảnh theo TCVN 5575:2012.")
    {
        StrengthChecks = strengthChecks;
        StabilityChecks = stabilityChecks;
        RunStrengthCheckCommand = runStrengthCheckCommand;
        RunStabilityCheckCommand = runStabilityCheckCommand;
    }

    public ObservableCollection<BeamCheckResult> StrengthChecks { get; }

    public ObservableCollection<BeamCheckResult> StabilityChecks { get; }

    public ICommand RunStrengthCheckCommand { get; }

    public ICommand RunStabilityCheckCommand { get; }
}
