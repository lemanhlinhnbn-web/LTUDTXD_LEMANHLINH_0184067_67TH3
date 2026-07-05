using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public interface IReportExportService
{
    void ExportBeamReport(
        BeamDesignInput input,
        BeamCalculationResult result,
        string etabsModelName,
        string connectionStatus);
}
