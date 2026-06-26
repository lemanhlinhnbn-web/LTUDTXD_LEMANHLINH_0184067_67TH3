using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using CSiAPIv1;
using LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Models;

namespace LTUDTXD_HUCE_01_LEMANHLINH_0318867_67TH3.Services;

public sealed class EtabsService : IEtabsService
{
    private const string EtabsProgId = "CSI.ETABS.API.ETABSObject";

    private cHelper? _helper;
    private cOAPI? _etabsObject;
    private cSapModel? _sapModel;

    public bool IsConnected => _etabsObject is not null && _sapModel is not null;

    public EtabsConnectionResult ConnectToRunningInstance()
    {
        Disconnect();
        string stage = "khởi tạo";

        try
        {
            stage = "lấy phiên ETABS đang chạy";
            _etabsObject = GetRunningEtabsObject();

            if (_etabsObject is null)
            {
                return EtabsConnectionResult.Failure(
                    "ETABS Helper không trả về đối tượng ứng dụng.");
            }

            stage = "lấy SapModel";
            _sapModel = _etabsObject.SapModel;

            if (_sapModel is null)
            {
                Disconnect();
                return EtabsConnectionResult.Failure(
                    "Đã tìm thấy ETABS nhưng không truy cập được SapModel.");
            }

            string version = string.Empty;
            double versionNumber = 0;
            stage = "đọc phiên bản ETABS";
            int returnCode = _sapModel.GetVersion(ref version, ref versionNumber);

            if (returnCode != 0)
            {
                version = "Không xác định";
            }

            string modelPath = TryGetModelPath(_sapModel);
            string modelName = string.IsNullOrWhiteSpace(modelPath)
                ? "Mô hình chưa lưu"
                : Path.GetFileName(modelPath);

            return EtabsConnectionResult.Success(modelName, modelPath, version);
        }
        catch (COMException exception)
        {
            Disconnect();
            string permissionHint = IsRunningAsAdministrator()
                ? "Ứng dụng đang chạy Administrator. Hãy mở ETABS bằng Administrator hoặc chạy Visual Studio/ứng dụng ở chế độ thường."
                : "Hãy bảo đảm ETABS và ứng dụng chạy cùng mức quyền.";

            return EtabsConnectionResult.Failure(
                $"ETABS API từ chối kết nối (0x{exception.HResult:X8}). " +
                permissionHint);
        }
        catch (Exception exception)
        {
            Disconnect();
            return EtabsConnectionResult.Failure(
                $"Không thể kết nối ETABS tại bước '{stage}': {exception.Message}");
        }
    }

    public void Disconnect()
    {
        ReleaseComObject(_sapModel);
        ReleaseComObject(_etabsObject);
        ReleaseComObject(_helper);
        _sapModel = null;
        _etabsObject = null;
        _helper = null;
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }

    private cOAPI GetRunningEtabsObject()
    {
        _helper = new Helper();

        Process[] etabsProcesses = Process.GetProcessesByName("ETABS")
            .OrderByDescending(process => process.Id)
            .ToArray();

        foreach (Process process in etabsProcesses)
        {
            try
            {
                cOAPI etabs = _helper.GetObjectProcess(EtabsProgId, process.Id);
                if (etabs is not null)
                {
                    return etabs;
                }
            }
            catch (COMException)
            {
                // Thử phiên ETABS khác nếu máy đang mở nhiều phiên.
            }
            finally
            {
                process.Dispose();
            }
        }

        cOAPI activeEtabs = _helper.GetObject(EtabsProgId);
        if (activeEtabs is null)
        {
            throw new COMException("ETABS chưa đăng ký phiên API đang chạy.");
        }

        return activeEtabs;
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is not null && Marshal.IsComObject(comObject))
        {
            Marshal.FinalReleaseComObject(comObject);
        }
    }

    private static string TryGetModelPath(cSapModel sapModel)
    {
        try
        {
            return sapModel.GetModelFilename(true) ?? string.Empty;
        }
        catch (NullReferenceException)
        {
            // ETABS 22 có thể ném lỗi nội bộ khi mô hình mới chưa được lưu.
            return string.Empty;
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
