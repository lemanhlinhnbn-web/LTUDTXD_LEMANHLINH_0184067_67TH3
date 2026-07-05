using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public interface IBeamCalculationService
{
    BeamCalculationResult Calculate(BeamDesignInput input);
}
