using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using CSiAPIv1;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

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

    public IReadOnlyList<EtabsFrameItem> GetFrameList()
    {
        if (_sapModel is null)
        {
            throw new InvalidOperationException("Chưa kết nối với ETABS.");
        }

        int count = 0;
        string[] frameNames = [];
        int result = _sapModel.FrameObj.GetNameList(ref count, ref frameNames);
        if (result != 0)
        {
            throw new InvalidOperationException("Không lấy được danh sách dầm Frame trong ETABS.");
        }

        eUnits previousUnits = _sapModel.GetPresentUnits();
        _sapModel.SetPresentUnits(eUnits.N_mm_C);

        try
        {
            List<EtabsFrameItem> frames = new(count);
            foreach (string frameName in frameNames.OrderBy(name => name))
            {
                frames.Add(ReadFrameListItem(frameName));
            }

            return frames;
        }
        finally
        {
            _sapModel.SetPresentUnits(previousUnits);
        }
    }

    public EtabsForceReadResult ReadSelectedFrameForces()
    {
        if (_sapModel is null)
        {
            return EtabsForceReadResult.Failure("Chưa kết nối với ETABS.");
        }

        try
        {
            int numberItems = 0;
            int[] objectTypes = [];
            string[] objectNames = [];
            int selectedResult = _sapModel.SelectObj.GetSelected(
                ref numberItems,
                ref objectTypes,
                ref objectNames);

            if (selectedResult != 0 || numberItems == 0)
            {
                return EtabsForceReadResult.Failure(
                    "Hãy chọn một dầm Frame trong ETABS trước khi đọc dữ liệu.");
            }

            string? frameName = null;
            for (int index = 0; index < numberItems; index++)
            {
                if (objectTypes[index] == 2)
                {
                    if (frameName is not null)
                    {
                        return EtabsForceReadResult.Failure(
                            "Chỉ được chọn một phần tử dầm Frame trong ETABS.");
                    }

                    frameName = objectNames[index];
                }
            }

            if (frameName is null)
            {
                return EtabsForceReadResult.Failure(
                    "Đối tượng đang chọn không phải phần tử Frame.");
            }

            return ReadFrameForces(frameName);
        }
        catch (Exception exception)
        {
            return EtabsForceReadResult.Failure(
                $"Không thể đọc nội lực ETABS: {exception.Message}");
        }
    }

    public EtabsForceReadResult ReadFrameForces(string frameName)
    {
        if (_sapModel is null)
        {
            return EtabsForceReadResult.Failure("Chưa kết nối với ETABS.");
        }

        if (string.IsNullOrWhiteSpace(frameName))
        {
            return EtabsForceReadResult.Failure("Hãy chọn một dầm trong bảng danh sách dầm.");
        }

        try
        {
            FrameDesignData frameData = ReadFrameDesignData(frameName);
            SelectOutputCombinationsForDesign();

            eUnits previousUnits = _sapModel.GetPresentUnits();
            _sapModel.SetPresentUnits(eUnits.kN_m_C);

            try
            {
                int count = 0;
                string[] obj = [];
                double[] objSta = [];
                string[] elm = [];
                double[] elmSta = [];
                string[] loadCase = [];
                string[] stepType = [];
                double[] stepNum = [];
                double[] p = [];
                double[] v2 = [];
                double[] v3 = [];
                double[] t = [];
                double[] m2 = [];
                double[] m3 = [];

                int result = _sapModel.Results.FrameForce(
                    frameName,
                    eItemTypeElm.ObjectElm,
                    ref count,
                    ref obj,
                    ref objSta,
                    ref elm,
                    ref elmSta,
                    ref loadCase,
                    ref stepType,
                    ref stepNum,
                    ref p,
                    ref v2,
                    ref v3,
                    ref t,
                    ref m2,
                    ref m3);

                if (result != 0 || count == 0)
                {
                    return EtabsForceReadResult.Failure(
                        "ETABS chưa có kết quả nội lực cho phần tử. Hãy Run Analysis trước.");
                }

                // 1) Vị trí có |M3| lớn nhất (xét mọi tổ hợp) - dùng cho điều kiện bền uốn
                int governingMomentIndex = 0;
                // 2) Vị trí có |V2| lớn nhất (xét mọi tổ hợp) - dùng cho điều kiện bền cắt
                int governingShearIndex =0;
                for (int index = 1; index < count; index++)
                {
                    if (Math.Abs(m3[index]) > Math.Abs(m3[governingMomentIndex]))
                    {
                        governingMomentIndex = index;
                    }

                    if (Math.Abs(v2[index]) > Math.Abs(v2[governingShearIndex]))
                    {
                        governingShearIndex = index;
                    }
                }

                double designMoment = m3[governingMomentIndex];
                double shearAtMaxMoment = v2[governingShearIndex];                
                double accompanyingSecondaryShear = v3[governingMomentIndex];
                double accompanyingAxialForce = p[governingMomentIndex];
                double governingStation = objSta[governingMomentIndex];
                string governingLoadCase = loadCase[governingMomentIndex];
                double designShear = v2[governingShearIndex];
                double governingShearStation = objSta[governingShearIndex];
                string governingShearCase = loadCase[governingShearIndex];
                int minimumMomentIndex = governingMomentIndex;
                List<EtabsForceDiagramPoint> diagramPoints = new();
                for (int index = 0; index < count; index++)
                {
                    if (!loadCase[index].Equals(governingLoadCase, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (m3[index] < m3[minimumMomentIndex])
                    {
                        minimumMomentIndex = index;
                    }

                    diagramPoints.Add(new EtabsForceDiagramPoint
                    {
                        Station = objSta[index],
                        Moment = m3[index],
                        Shear = v2[index],
                        SecondaryShear = v3[index]
                    });
                }

                diagramPoints = diagramPoints
                    .OrderBy(point => point.Station)
                    .ToList();

                return EtabsForceReadResult.Success(
                    frameName,
                    designMoment,
                    m3[minimumMomentIndex],
                    designShear,
                    shearAtMaxMoment,
                    governingShearStation,
                    accompanyingSecondaryShear,
                    accompanyingAxialForce,
                    governingStation,
                    governingLoadCase,
                    governingLoadCase,
                    frameData.SectionName,
                    frameData.MaterialName,
                    frameData.Height,
                    frameData.FlangeWidth,
                    frameData.WebThickness,
                    frameData.FlangeThickness,
                    frameData.SpanLength,
                    frameData.YieldStrength,
                    frameData.ElasticModulus,
                    frameData.SuggestedPhiB,
                    diagramPoints);
            }
            finally
            {
                _sapModel.SetPresentUnits(previousUnits);
            }
        }
        catch (Exception exception)
        {
            return EtabsForceReadResult.Failure(
                $"Không thể đọc nội lực ETABS cho dầm '{frameName}': {exception.Message}");
        }
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

    private static string NaturalSortKey(string name)
    {
        // Đệm số về 8 chữ số để "B2" đứng trước "B10" khi sắp xếp chuỗi.
        var builder = new System.Text.StringBuilder(name.Length + 8);
        int index = 0;
        while (index < name.Length)
        {
            if (char.IsDigit(name[index]))
            {
                int start = index;
                while (index < name.Length && char.IsDigit(name[index]))
                {
                    index++;
                }

                builder.Append(name.Substring(start, index - start).PadLeft(8, '0'));
            }
            else
            {
                builder.Append(name[index]);
                index++;
            }
        }
        
        return builder.ToString();
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

    private EtabsFrameItem ReadFrameListItem(string frameName)
    {
        if (_sapModel is null)
        {
            throw new InvalidOperationException("Chưa kết nối ETABS.");
        }

        string sectionName = string.Empty;
        string autoSection = string.Empty;
        _sapModel.FrameObj.GetSection(frameName, ref sectionName, ref autoSection);

        string point1 = string.Empty;
        string point2 = string.Empty;
        _sapModel.FrameObj.GetPoints(frameName, ref point1, ref point2);

        double x1 = 0;
        double y1 = 0;
        double z1 = 0;
        double x2 = 0;
        double y2 = 0;
        double z2 = 0;
        _sapModel.PointObj.GetCoordCartesian(point1, ref x1, ref y1, ref z1);
        _sapModel.PointObj.GetCoordCartesian(point2, ref x2, ref y2, ref z2);

        double lengthMm = Math.Sqrt(
            Math.Pow(x2 - x1, 2)
            + Math.Pow(y2 - y1, 2)
            + Math.Pow(z2 - z1, 2));

        return new EtabsFrameItem
        {
            Name = frameName,
            SectionName = string.IsNullOrWhiteSpace(sectionName) ? autoSection : sectionName,
            SpanLength = lengthMm / 1000
        };
    }

    private void SelectOutputCombinationsForDesign()
    {
        if (_sapModel is null)
        {
            return;
        }

        _sapModel.Results.Setup.DeselectAllCasesAndCombosForOutput();

        int comboCount = 0;
        string[] combos = [];
        _sapModel.RespCombo.GetNameList(ref comboCount, ref combos);
        foreach (string combo in combos)
        {
            _sapModel.Results.Setup.SetComboSelectedForOutput(combo);
        }

        if (comboCount > 0)
        {
            return;
        }

        // Fallback for unfinished ETABS models that do not define response combinations yet.
        for (int typeValue = 1; typeValue <= 17; typeValue++)
        {
            int caseCount = 0;
            string[] cases = [];
            _sapModel.LoadCases.GetNameList(
                ref caseCount,
                ref cases,
                (eLoadCaseType)typeValue);

            foreach (string loadCase in cases)
            {
                _sapModel.Results.Setup.SetCaseSelectedForOutput(loadCase);
            }
        }
    }

    private FrameDesignData ReadFrameDesignData(string frameName)
    {
        if (_sapModel is null)
        {
            throw new InvalidOperationException("Chưa kết nối ETABS.");
        }

        eUnits previousUnits = _sapModel.GetPresentUnits();
        _sapModel.SetPresentUnits(eUnits.N_mm_C);

        try
        {
            string sectionName = string.Empty;
            string autoSection = string.Empty;
            if (_sapModel.FrameObj.GetSection(
                    frameName,
                    ref sectionName,
                    ref autoSection) != 0
                || string.IsNullOrWhiteSpace(sectionName))
            {
                throw new InvalidOperationException(
                    "Không đọc được tiết diện của Frame đang chọn.");
            }

            string fileName = string.Empty;
            string materialName = string.Empty;
            double height = 0;
            double topWidth = 0;
            double topThickness = 0;
            double webThickness = 0;
            double bottomWidth = 0;
            double bottomThickness = 0;
            double filletRadius = 0;
            int color = 0;
            string notes = string.Empty;
            string guid = string.Empty;

            int sectionResult = _sapModel.PropFrame.GetISection_1(
                sectionName,
                ref fileName,
                ref materialName,
                ref height,
                ref topWidth,
                ref topThickness,
                ref webThickness,
                ref bottomWidth,
                ref bottomThickness,
                ref filletRadius,
                ref color,
                ref notes,
                ref guid);

            if (sectionResult != 0)
            {
                throw new InvalidOperationException(
                    $"Tiết diện '{sectionName}' không phải tiết diện chữ I mà chương trình hỗ trợ.");
            }

            string materialOverwrite = string.Empty;
            if (_sapModel.FrameObj.GetMaterialOverwrite(
                    frameName,
                    ref materialOverwrite) == 0
                && !string.IsNullOrWhiteSpace(materialOverwrite)
                && !materialOverwrite.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                materialName = materialOverwrite;
            }

            double fy = 0;
            double fu = 0;
            double eFy = 0;
            double eFu = 0;
            int stressStrainType = 0;
            int hysteresisType = 0;
            double strainAtHardening = 0;
            double strainAtMaximumStress = 0;
            double strainAtRupture = 0;
            double finalSlope = 0;
            int materialResult = _sapModel.PropMaterial.GetOSteel_1(
                materialName,
                ref fy,
                ref fu,
                ref eFy,
                ref eFu,
                ref stressStrainType,
                ref hysteresisType,
                ref strainAtHardening,
                ref strainAtMaximumStress,
                ref strainAtRupture,
                ref finalSlope,
                0);

            if (materialResult != 0 || fy <= 0)
            {
                throw new InvalidOperationException(
                    $"Không đọc được cường độ chảy của vật liệu '{materialName}'.");
            }

            double elasticModulus = 0;
            double poissonRatio = 0;
            double thermalCoefficient = 0;
            double shearModulus = 0;
            if (_sapModel.PropMaterial.GetMPIsotropic(
                    materialName,
                    ref elasticModulus,
                    ref poissonRatio,
                    ref thermalCoefficient,
                    ref shearModulus,
                    0) != 0
                || elasticModulus <= 0)
            {
                throw new InvalidOperationException(
                    $"Không đọc được mô đun đàn hồi của vật liệu '{materialName}'.");
            }

            string point1 = string.Empty;
            string point2 = string.Empty;
            if (_sapModel.FrameObj.GetPoints(
                    frameName,
                    ref point1,
                    ref point2) != 0)
            {
                throw new InvalidOperationException(
                    "Không đọc được hai đầu của Frame.");
            }

            double x1 = 0;
            double y1 = 0;
            double z1 = 0;
            double x2 = 0;
            double y2 = 0;
            double z2 = 0;
            _sapModel.PointObj.GetCoordCartesian(point1, ref x1, ref y1, ref z1);
            _sapModel.PointObj.GetCoordCartesian(point2, ref x2, ref y2, ref z2);

            double lengthMm = Math.Sqrt(
                Math.Pow(x2 - x1, 2)
                + Math.Pow(y2 - y1, 2)
                + Math.Pow(z2 - z1, 2));
            double flangeWidth = (topWidth + bottomWidth) / 2;
            double flangeThickness = (topThickness + bottomThickness) / 2;
            double webHeight = height - topThickness - bottomThickness;
            double area = topWidth * topThickness
                + bottomWidth * bottomThickness
                + webHeight * webThickness;
            double iy = topThickness * Math.Pow(topWidth, 3) / 12
                + bottomThickness * Math.Pow(bottomWidth, 3) / 12
                + webHeight * Math.Pow(webThickness, 3) / 12;
            double radiusY = Math.Sqrt(iy / area);
            double slendernessY = radiusY > 0 ? lengthMm / radiusY : 0;

            // Giá trị đề xuất bảo thủ để tự động hóa dữ liệu ban đầu.
            // Người thiết kế vẫn phải đối chiếu φb theo TCVN cho sơ đồ giữ cánh thực tế.
            double suggestedPhiB = Math.Max(
                0.20,
                Math.Min(1.0, 1.0 / (1.0 + Math.Pow(slendernessY / 120.0, 2))));

            return new FrameDesignData
            {
                SectionName = sectionName,
                MaterialName = materialName,
                Height = height,
                FlangeWidth = flangeWidth,
                WebThickness = webThickness,
                FlangeThickness = flangeThickness,
                SpanLength = lengthMm / 1000,
                YieldStrength = fy,
                ElasticModulus = elasticModulus,
                SuggestedPhiB = suggestedPhiB
            };
        }
        finally
        {
            _sapModel.SetPresentUnits(previousUnits);
        }
    }

    private sealed class FrameDesignData
    {
        public string SectionName { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public double Height { get; set; }
        public double FlangeWidth { get; set; }
        public double WebThickness { get; set; }
        public double FlangeThickness { get; set; }
        public double SpanLength { get; set; }
        public double YieldStrength { get; set; }
        public double ElasticModulus { get; set; }
        public double SuggestedPhiB { get; set; }
    }
}
