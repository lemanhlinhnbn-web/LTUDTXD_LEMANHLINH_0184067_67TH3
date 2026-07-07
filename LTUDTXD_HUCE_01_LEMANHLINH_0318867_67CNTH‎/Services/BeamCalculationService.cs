using System;
using System.Collections.Generic;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public sealed class BeamCalculationService : IBeamCalculationService
{
    public BeamCalculationResult Calculate(BeamDesignInput input)
    {
        Validate(input);

        double h = input.Height;                       // Chiều cao dầm (mm)
        double b = input.FlangeWidth;                  // Bề rộng cánh (mm) 
        double tw = input.WebThickness;                // Chiều dày bụng (mm)
        double tf = input.FlangeThickness;             // Chiều dày cánh (mm)
        double hw = h - 2 * tf;                        // Chiều cao bản bụng (mm)

        double area = 2 * b * tf + hw * tw;            // Diện tích tiết diện (mm2)
        double ix = 2 * (b * Math.Pow(tf, 3) / 12      // Moment quán tính theo trục X (Ix)
            + b * tf * Math.Pow(h / 2 - tf / 2, 2))
            + tw * Math.Pow(hw, 3) / 12;
        double iy = 2 * tf * Math.Pow(b, 3) / 12       // Moment quán tính theo trục Y (Iy)
            + hw * Math.Pow(tw, 3) / 12;
        double wx = ix / (h / 2);                      // Moment kháng uốn (Wx)
        double radiusX = Math.Sqrt(ix / area);          
        double radiusY = Math.Sqrt(iy / area);
        double shearArea = hw * tw;                    // Diện tích chịu cắt

        var section = new BeamSectionProperties
        {
            Area = area,
            WebHeight = hw,
            Ix = ix,
            Iy = iy,
            Wx = wx,
            RadiusX = radiusX,
            RadiusY = radiusY,
            ShearArea = shearArea
        };

        double ry = input.DesignStrength;               // Cường độ chảy của thép ry                         
        double gammaC = input.WorkingConditionFactor;   // Hệ số điều kiện làm việc γc
        double rs = 0.58 * ry;                          // Cường độ chịu cắt tính toán rs
        // Đổi đơn vị kNm sang Nmm
        double momentNmm = Math.Abs(input.DesignMoment) * 1_000_000;
        // Đổi đơn vị kN sang N
        double shearN = Math.Abs(input.DesignShear) * 1_000;
        // V tại tiết diện có M max, dùng cho kiểm tra ứng suất tương đương.
        // Nếu người dùng nhập tay và bỏ trống, lấy an toàn bằng V thiết kế.
        double shearAtmomentN = input.ShearAtMaxMoment > 0
            ? Math.Abs(input.ShearAtMaxMoment) * 1_000
            : shearN;

        // Mômen tĩnh nửa tiết diện đối với trục trung hoà (τ max giữa bản bụng).
        double staticMomentHalf = b * tf * (h - tf) / 2 + tw * hw * hw / 8;
        // Môment tĩnh riêng bản cánh (τ tại chỗ tiếp giáp bụng - cánh).
        double staticMomentFlange = b * tf * (h - tf) / 2;

        double bendingStress = momentNmm / wx;
        double bendingRatio = bendingStress / (ry * gammaC);

        // Bền cắt theo TCVN 5575: τ = V.S/(I.tw) ≤ Rs.γc, với V max toàn dầm.
        double shearStress = shearN * staticMomentHalf / (ix * tw);        
        double shearRatio = shearStress / (rs * gammaC);

        // Ứng suất tương đương tại chỗ tiếp giáp bụng - cánh, cùng tiết diện M max:
        // σtđ = sqrt(σ1² + 3·τ1²) ≤ 1,15·Ry·γc (TCVN 5575).
        double sigmal = momentNmm / ix * (hw / 2);
        double tau1 = shearAtmomentN * staticMomentFlange / (ix * tw);
        double equivalentStress = Math.Sqrt(sigmal * sigmal + 3 * tau1 * tau1);
        double interactionRatio = equivalentStress / (1.15 * ry * gammaC);

        var strength = new List<BeamCheckResult>
        {
            CreateResult("Bền uốn", "Ứng suất pháp do mô men",
                "σ ≤ Ry·γc", bendingRatio,
                $"{bendingStress:0.00} / {ry * gammaC:0.00} MPa", "M max toàn dầm"),
            CreateResult("Bền cắt", "Ứng suất tiếp bản bụng",
                "τ = V·S/(I·tw) ≤ Rs·γc", shearRatio,
                $"{shearStress:0.00} / {rs * gammaC:0.00} MPa",
                "V max toàn dầm; Rs = 0,58Ry"),
            CreateResult("Tổ hợp", "Ứng suất tương đương bụng - cánh",
                "σtđ ≤ 1,15·Ry·γc", interactionRatio,
                $"{equivalentStress:0.00} / {1.15 * ry * gammaC:0.00} MPa",
                "σtđ = √(σ1² + 3τ1²); M và V cùng tiết diện")
        };

        double phiB = input.LateralTorsionalBucklingFactor;
        if (phiB <= 0 || phiB > 1)
        {
            throw new ArgumentException("Hệ số ổn định tổng thể φb phải lớn hơn 0 và không vượt quá 1.");
        }

        double lateralRatio = bendingRatio / phiB;
        double normalizedFactor = Math.Sqrt(ry / input.ElasticModulus);
        double flangeOutstand = (b - tw) / 2;
        double flangeSlenderness = flangeOutstand / tf * normalizedFactor;
        double webSlenderness = hw / tw * normalizedFactor;

        // Giới hạn độ mảnh quy ước λ của bản cánh chịu nén theo TCVN 5575:2024, bảng 17 
        const double flangeLimit = 0.50;
        double flangeRatio = flangeSlenderness / flangeLimit;
        // Giới hạn độ mảnh quy ước λ của bản bụng chịu nén theo TCVN 5575:2024 
        const double webLimit = 3.20;        
        double webRatio = webSlenderness / webLimit;

        var stability = new List<BeamCheckResult>
        {
            CreateResult("Ổn định tổng thể", "Uốn xoắn ngang của dầm",
                "M ≤ φb·Ry·Wx·γc", lateralRatio,
                $"η = {lateralRatio:0.000}; φb = {phiB:0.000}",
                $"Lb = {input.UnbracedLength:0.###} m"),
            CreateResult("Ổn định cục bộ", "Bản cánh nén",
                "λ̄f ≤ 0,50", flangeRatio,
                $"λ̄f = {flangeSlenderness:0.000}",
                $"bef/tf = {flangeOutstand / tf:0.00}"),
            CreateResult("Ổn định cục bộ", "Bản bụng",
                "λ̄w ≤ 3,20", webRatio,
                $"λ̄w = {webSlenderness:0.000}",
                $"hw/tw = {hw / tw:0.00}")
        };

        double governingStrength = Math.Max(bendingRatio, Math.Max(shearRatio, interactionRatio));
        double governingStability = Math.Max(lateralRatio, Math.Max(flangeRatio, webRatio));
        string governing = governingStrength >= governingStability
            ? GetGoverningName(strength)
            : GetGoverningName(stability);

        return new BeamCalculationResult
        {
            Section = section,
            StrengthChecks = strength,
            StabilityChecks = stability,
            GoverningStrengthRatio = governingStrength,
            GoverningStabilityRatio = governingStability,
            GoverningCondition = governing,
            IsPassed = governingStrength <= 1 && governingStability <= 1
        };
    }

    private static BeamCheckResult CreateResult(
        string group,
        string name,
        string limit,
        double ratio,
        string currentValue,
        string note)
    {
        string status = ratio <= 1 ? "Đạt" : "Không đạt";
        string brush = ratio <= 0.85 ? "#20C9B0" : ratio <= 1 ? "#F59E0B" : "#F43F5E";

        return new BeamCheckResult
        {
            Group = group,
            Name = name,
            Limit = limit,
            CurrentValue = currentValue,
            UtilizationPercent = ratio * 100,
            Status = status,
            StatusBrush = brush,
            Note = note
        };
    }

    private static string GetGoverningName(IEnumerable<BeamCheckResult> results)
    {
        BeamCheckResult? governing = null;
        foreach (BeamCheckResult result in results)
        {
            if (governing is null || result.UtilizationPercent > governing.UtilizationPercent)
            {
                governing = result;
            }
        }

        return governing?.Name ?? string.Empty;
    }

    private static void Validate(BeamDesignInput input)
    {
        if (input.Height <= 0 || input.FlangeWidth <= 0
            || input.WebThickness <= 0 || input.FlangeThickness <= 0)
        {
            throw new ArgumentException("Các kích thước tiết diện phải lớn hơn 0.");
        }

        if (input.Height <= 2 * input.FlangeThickness)
        {
            throw new ArgumentException("Chiều cao dầm phải lớn hơn hai lần chiều dày cánh.");
        }

        if (input.FlangeWidth <= input.WebThickness)
        {
            throw new ArgumentException("Bề rộng cánh phải lớn hơn chiều dày bụng.");
        }

        if (input.DesignStrength <= 0 || input.ElasticModulus <= 0
            || input.WorkingConditionFactor <= 0)
        {
            throw new ArgumentException("Thông số vật liệu và hệ số làm việc phải lớn hơn 0.");
        }

        if (input.SpanLength <= 0 || input.UnbracedLength <= 0
            || input.UnbracedLength > input.SpanLength)
        {
            throw new ArgumentException("Chiều dài không giằng phải lớn hơn 0 và không vượt quá nhịp dầm.");
        }

        if (Math.Abs(input.DesignMoment) <= 0 && Math.Abs(input.DesignShear) <= 0 )
        {
            throw new ArgumentException(
                "Mômen và lực cắt thiết kế đang đồng thời bằng 0. "
                + "Hãy nhập nội lực hoặc đồng bộ từ Etabs trước khi kiểm tra.");
        }
    }
}
