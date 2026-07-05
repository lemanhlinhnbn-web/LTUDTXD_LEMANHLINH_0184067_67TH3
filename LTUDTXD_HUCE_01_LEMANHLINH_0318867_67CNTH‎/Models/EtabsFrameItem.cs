namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

public sealed class EtabsFrameItem
{
    public string Name { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public double SpanLength { get; set; }
    public string SpanLengthText => $"{SpanLength:0.###} m";
}
