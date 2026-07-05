using System;
using System.Collections.Generic;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public sealed class BeamCalculationService : IBeamCalculationService
{
    public BeamCalculationResult Calculate(BeamDesignInput input)
    {
        Validate(input);

        double h = input.Height;
        double b = input.FlangeWidth;
        double tw = input.WebThickness;
        double tf = input.FlangeThickness;
        double hw = h - 2 * tf;

        double area = 2 * b * tf + hw * tw;
        double ix = 2 * (b * Math.Pow(tf, 3) / 12
            + b * tf * Math.Pow(h / 2 - tf / 2, 2))
            + tw * Math.Pow(hw, 3) / 12;
        double iy = 2 * tf * Math.Pow(b, 3) / 12
            + hw * Math.Pow(tw, 3) / 12;
        double wx = ix / (h / 2);
        double radiusX = Math.Sqrt(ix / area);
        double radiusY = Math.Sqrt(iy / area);
        double shearArea = hw * tw;

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

        double ry = input.DesignStrength;
        double gammaC = input.WorkingConditionFactor;
        double rs = 0.58 * ry;
        double momentNmm = Math.Abs(input.DesignMoment) * 1_000_000;
        double shearN = Math.Abs(input.DesignShear) * 1_000;

        double bendingStress = momentNmm / wx;
        double shearStress = shearN / shearArea;
        double bendingRatio = bendingStress / (ry * gammaC);
        double shearRatio = shearStress / (rs * gammaC);
        double interactionRatio = Math.Sqrt(
            bendingRatio * bendingRatio + shearRatio * shearRatio);

        var strength = new List<BeamCheckResult>
        {
            CreateResult("Bền uốn", "Ứng suất pháp do mô men",
                "σ ≤ Ry·γc", bendingRatio,
                $"{bendingStress:0.00} / {ry * gammaC:0.00} MPa", "Mô men thiết kế"),
            CreateResult("Bền cắt", "Ứng suất tiếp bản bụng",
                "τ ≤ Rs·γc", shearRatio,
                $"{shearStress:0.00} / {rs * gammaC:0.00} MPa", "Rs = 0,58Ry"),
            CreateResult("Tổ hợp", "Tương tác uốn và cắt",
                "η ≤ 1,00", interactionRatio,
                $"{interactionRatio:0.000}", "Căn bậc hai tổng bình phương")
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

        // Giới hạn độ mảnh quy ước dùng cho dầm chữ I chịu uốn trong phạm vi đề tài.
        const double flangeLimit = 0.50;
        const double webLimit = 3.20;
        double flangeRatio = flangeSlenderness / flangeLimit;
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
    }
}
