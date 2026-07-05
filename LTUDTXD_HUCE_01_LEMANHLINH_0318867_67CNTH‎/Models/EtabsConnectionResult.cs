namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

public sealed class EtabsConnectionResult
{
    private EtabsConnectionResult(bool isSuccess, string message, string modelName, string modelPath, string version)
    {
        IsSuccess = isSuccess;
        Message = message;
        ModelName = modelName;
        ModelPath = modelPath;
        Version = version;
    }

    public bool IsSuccess { get; }
    public string Message { get; }
    public string ModelName { get; }
    public string ModelPath { get; }
    public string Version { get; }

    public static EtabsConnectionResult Success(string modelName, string modelPath, string version) =>
        new(true, "Đã kết nối ETABS thành công.", modelName, modelPath, version);

    public static EtabsConnectionResult Failure(string message) =>
        new(false, message, string.Empty, string.Empty, string.Empty);
}
