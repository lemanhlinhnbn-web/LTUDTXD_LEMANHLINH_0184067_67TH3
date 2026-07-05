using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Xml;
using LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Models;
using Microsoft.Win32;

namespace LTUDTXD_HUCE_LEMANHLINH_0184067_67CNTH.Services;

public sealed class ExcelReportExportService : IReportExportService
{
    public void ExportBeamReport(
        BeamDesignInput input,
        BeamCalculationResult result,
        string etabsModelName,
        string connectionStatus)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu báo cáo Excel",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"Bao_cao_kiem_tra_dam_thep_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        CreateWorkbook(dialog.FileName, input, result, etabsModelName, connectionStatus);
        MessageBox.Show(
            Application.Current.MainWindow,
            $"Đã lưu báo cáo Excel:\n{dialog.FileName}",
            "Lưu báo cáo",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static void CreateWorkbook(
        string filePath,
        BeamDesignInput input,
        BeamCalculationResult result,
        string etabsModelName,
        string connectionStatus)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        WriteTextEntry(archive, "[Content_Types].xml", GetContentTypesXml());
        WriteTextEntry(archive, "_rels/.rels", GetRootRelationshipsXml());
        WriteTextEntry(archive, "docProps/app.xml", GetAppPropertiesXml());
        WriteTextEntry(archive, "docProps/core.xml", GetCorePropertiesXml());
        WriteTextEntry(archive, "xl/workbook.xml", GetWorkbookXml());
        WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", GetWorkbookRelationshipsXml());
        WriteTextEntry(archive, "xl/styles.xml", GetStylesXml());
        WriteTextEntry(archive, "xl/worksheets/sheet1.xml", GetWorksheetXml(input, result, etabsModelName, connectionStatus));
    }

    private static string GetWorksheetXml(
        BeamDesignInput input,
        BeamCalculationResult result,
        string etabsModelName,
        string connectionStatus)
    {
        using var stream = new MemoryStream();
        using (XmlWriter writer = XmlWriter.Create(stream, CreateXmlSettings()))
        {
            writer.WriteStartDocument(true);
            writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteStartElement("cols");
            WriteColumn(writer, 1, 1, 24);
            WriteColumn(writer, 2, 2, 18);
            WriteColumn(writer, 3, 3, 16);
            WriteColumn(writer, 4, 4, 28);
            WriteColumn(writer, 5, 5, 16);
            WriteColumn(writer, 6, 6, 16);
            WriteColumn(writer, 7, 7, 28);
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData");
            int row = 1;
            WriteRow(writer, row++, Cell.Text("A", "BÁO CÁO KIỂM TRA DẦM THÉP", 1));
            WriteRow(writer, row++, Cell.Text("A", $"Tiêu chuẩn: {input.Standard}", 3), Cell.Text("D", $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}", 3));
            row++;

            WriteSection(writer, ref row, "THÔNG TIN MÔ HÌNH");
            WriteRow(writer, row++, Cell.Text("A", "Tên mô hình ETABS", 4), Cell.Text("B", string.IsNullOrWhiteSpace(etabsModelName) ? "Chưa có dữ liệu" : etabsModelName, 2));
            WriteRow(writer, row++, Cell.Text("A", "Trạng thái kết nối", 4), Cell.Text("B", connectionStatus, 2));
            WriteRow(writer, row++, Cell.Text("A", "Kết luận chung", 4), Cell.Text("B", result.IsPassed ? "Đạt" : "Không đạt", result.IsPassed ? 8 : 9));
            WriteRow(writer, row++, Cell.Text("A", "Điều kiện chi phối", 4), Cell.Text("B", result.GoverningCondition, 2));
            row++;

            WriteSection(writer, ref row, "THÔNG SỐ ĐẦU VÀO");
            WriteHeader(writer, row++, "Thông số", "Giá trị", "Đơn vị", "Ghi chú");
            WriteParameter(writer, ref row, "Mác thép", input.SteelGrade, string.Empty, "Vật liệu");
            WriteParameter(writer, ref row, "Chiều cao h", input.Height, "mm", "Chiều cao tiết diện");
            WriteParameter(writer, ref row, "Bề rộng cánh b", input.FlangeWidth, "mm", "Bản cánh");
            WriteParameter(writer, ref row, "Chiều dày bụng tw", input.WebThickness, "mm", "Bản bụng");
            WriteParameter(writer, ref row, "Chiều dày cánh tf", input.FlangeThickness, "mm", "Bản cánh");
            WriteParameter(writer, ref row, "Nhịp dầm L", input.SpanLength, "m", "Chiều dài tính toán");
            WriteParameter(writer, ref row, "Chiều dài không giằng Lb", input.UnbracedLength, "m", "Ổn định tổng thể");
            WriteParameter(writer, ref row, "Cường độ Ry", input.DesignStrength, "MPa", "Cường độ tính toán");
            WriteParameter(writer, ref row, "Mô đun đàn hồi E", input.ElasticModulus, "MPa", "Vật liệu");
            WriteParameter(writer, ref row, "Mô men thiết kế M", input.DesignMoment, "kN.m", "Nội lực");
            WriteParameter(writer, ref row, "Lực cắt thiết kế V", input.DesignShear, "kN", "Nội lực");
            WriteParameter(writer, ref row, "Hệ số điều kiện làm việc γc", input.WorkingConditionFactor, string.Empty, "TCVN 2275:2024");
            WriteParameter(writer, ref row, "Hệ số ổn định φb", input.LateralTorsionalBucklingFactor, string.Empty, "Uốn xoắn ngang");
            row++;

            WriteSection(writer, ref row, "ĐẶC TRƯNG HÌNH HỌC TIẾT DIỆN");
            WriteHeader(writer, row++, "Đặc trưng", "Giá trị", "Đơn vị", "Ghi chú");
            WriteParameter(writer, ref row, "Diện tích A", result.Section.Area, "mm²", "Tiết diện chữ I");
            WriteParameter(writer, ref row, "Chiều cao bụng hw", result.Section.WebHeight, "mm", "h - 2tf");
            WriteParameter(writer, ref row, "Mô men quán tính Ix", result.Section.Ix, "mm⁴", "Trục mạnh");
            WriteParameter(writer, ref row, "Mô men quán tính Iy", result.Section.Iy, "mm⁴", "Trục yếu");
            WriteParameter(writer, ref row, "Mô men chống uốn Wx", result.Section.Wx, "mm³", "Theo trục x");
            WriteParameter(writer, ref row, "Bán kính quán tính ix", result.Section.RadiusX, "mm", "Trục x");
            WriteParameter(writer, ref row, "Bán kính quán tính iy", result.Section.RadiusY, "mm", "Trục y");
            WriteParameter(writer, ref row, "Diện tích chịu cắt Av", result.Section.ShearArea, "mm²", "Bản bụng");
            row++;

            WriteSection(writer, ref row, "KẾT QUẢ KIỂM TRA BỀN");
            WriteCheckTable(writer, ref row, result.StrengthChecks);
            row++;

            WriteSection(writer, ref row, "KẾT QUẢ KIỂM TRA ỔN ĐỊNH");
            WriteCheckTable(writer, ref row, result.StabilityChecks);
            row++;

            WriteSection(writer, ref row, "TỔNG HỢP");
            WriteRow(writer, row++, Cell.Text("A", "Hệ số sử dụng lớn nhất về bền", 4), Cell.Number("B", result.GoverningStrengthRatio, 7));
            WriteRow(writer, row++, Cell.Text("A", "Hệ số sử dụng lớn nhất về ổn định", 4), Cell.Number("B", result.GoverningStabilityRatio, 7));
            WriteRow(writer, row++, Cell.Text("A", "Trạng thái", 4), Cell.Text("B", result.IsPassed ? "Đạt yêu cầu" : "Không đạt yêu cầu", result.IsPassed ? 8 : 9));

            writer.WriteEndElement();
            writer.WriteStartElement("mergeCells");
            writer.WriteAttributeString("count", "7");
            WriteMerge(writer, "A1:G1");
            WriteMerge(writer, "A4:G4");
            WriteMerge(writer, "A10:G10");
            WriteMerge(writer, "A26:G26");
            WriteMerge(writer, "A37:G37");
            WriteMerge(writer, "A43:G43");
            WriteMerge(writer, "A49:G49");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCheckTable(XmlWriter writer, ref int row, IEnumerable<BeamCheckResult> checks)
    {
        WriteHeader(writer, row++, "Nhóm", "Nội dung", "Giới hạn", "Giá trị hiện tại", "Hiệu suất", "Trạng thái", "Ghi chú");
        foreach (BeamCheckResult check in checks)
        {
            WriteRow(
                writer,
                row++,
                Cell.Text("A", check.Group, 2),
                Cell.Text("B", check.Name, 2),
                Cell.Text("C", check.Limit, 2),
                Cell.Text("D", check.CurrentValue, 2),
                Cell.Number("E", check.UtilizationPercent / 100, 6),
                Cell.Text("F", check.Status, check.UtilizationPercent <= 100 ? 8 : 9),
                Cell.Text("G", check.Note, 2));
        }
    }

    private static void WriteParameter(XmlWriter writer, ref int row, string name, double value, string unit, string note)
    {
        WriteRow(
            writer,
            row++,
            Cell.Text("A", name, 2),
            Cell.Number("B", value, 7),
            Cell.Text("C", unit, 2),
            Cell.Text("D", note, 2));
    }

    private static void WriteParameter(XmlWriter writer, ref int row, string name, string value, string unit, string note)
    {
        WriteRow(
            writer,
            row++,
            Cell.Text("A", name, 2),
            Cell.Text("B", value, 2),
            Cell.Text("C", unit, 2),
            Cell.Text("D", note, 2));
    }

    private static void WriteSection(XmlWriter writer, ref int row, string title)
    {
        WriteRow(writer, row++, Cell.Text("A", title, 5));
    }

    private static void WriteHeader(XmlWriter writer, int row, params string[] headers)
    {
        var cells = new List<Cell>();
        for (int index = 0; index < headers.Length; index++)
        {
            cells.Add(Cell.Text(GetColumnName(index + 1), headers[index], 4));
        }

        WriteRow(writer, row, cells.ToArray());
    }

    private static void WriteRow(XmlWriter writer, int rowIndex, params Cell[] cells)
    {
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", rowIndex.ToString(CultureInfo.InvariantCulture));
        foreach (Cell cell in cells)
        {
            cell.Write(writer, rowIndex);
        }

        writer.WriteEndElement();
    }

    private static void WriteColumn(XmlWriter writer, int min, int max, double width)
    {
        writer.WriteStartElement("col");
        writer.WriteAttributeString("min", min.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", max.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteMerge(XmlWriter writer, string range)
    {
        writer.WriteStartElement("mergeCell");
        writer.WriteAttributeString("ref", range);
        writer.WriteEndElement();
    }

    private static string GetColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            int modulo = (index - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            index = (index - modulo) / 26;
        }

        return name;
    }

    private static void WriteTextEntry(ZipArchive archive, string path, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static XmlWriterSettings CreateXmlSettings()
    {
        return new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false
        };
    }

    private static string GetContentTypesXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/docProps/app.xml"" ContentType=""application/vnd.openxmlformats-officedocument.extended-properties+xml""/>
  <Override PartName=""/docProps/core.xml"" ContentType=""application/vnd.openxmlformats-package.core-properties+xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";

    private static string GetRootRelationshipsXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"" Target=""docProps/core.xml""/>
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"" Target=""docProps/app.xml""/>
</Relationships>";

    private static string GetWorkbookXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Báo cáo kiểm tra"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";

    private static string GetWorkbookRelationshipsXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/>
</Relationships>";

    private static string GetAppPropertiesXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Properties xmlns=""http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"" xmlns:vt=""http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"">
  <Application>LTUDTXD HUCE</Application>
</Properties>";

    private static string GetCorePropertiesXml() =>
        $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<cp:coreProperties xmlns:cp=""http://schemas.openxmlformats.org/package/2006/metadata/core-properties"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:dcterms=""http://purl.org/dc/terms/"" xmlns:dcmitype=""http://purl.org/dc/dcmitype/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <dc:title>Báo cáo kiểm tra dầm thép</dc:title>
  <dc:creator>LTUDTXD HUCE</dc:creator>
  <dcterms:created xsi:type=""dcterms:W3CDTF"">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created>
</cp:coreProperties>";

    private static string GetStylesXml() =>
        @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""4"">
    <font><sz val=""11""/><color rgb=""FF202229""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""18""/><color rgb=""FFFFFFFF""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Segoe UI""/></font>
    <font><b/><sz val=""11""/><color rgb=""FF20C9B0""/><name val=""Segoe UI""/></font>
  </fonts>
  <fills count=""6"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF101116""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF173F3B""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF202E39""/><bgColor indexed=""64""/></patternFill></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFFCE8E6""/><bgColor indexed=""64""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border><left style=""thin""><color rgb=""FFB8C4D1""/></left><right style=""thin""><color rgb=""FFB8C4D1""/></right><top style=""thin""><color rgb=""FFB8C4D1""/></top><bottom style=""thin""><color rgb=""FFB8C4D1""/></bottom><diagonal/></border>
  </borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""10"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1"" applyAlignment=""1""><alignment vertical=""center"" wrapText=""1""/></xf>
    <xf numFmtId=""0"" fontId=""3"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <xf numFmtId=""0"" fontId=""2"" fillId=""4"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1""><alignment vertical=""center"" wrapText=""1""/></xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""10"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>
    <xf numFmtId=""4"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""5"" borderId=""1"" xfId=""0"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1""><alignment horizontal=""center"" vertical=""center""/></xf>
  </cellXfs>
  <cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles>
</styleSheet>";

    private sealed class Cell
    {
        private readonly string _column;
        private readonly string _value;
        private readonly int _style;
        private readonly bool _isNumber;

        private Cell(string column, string value, int style, bool isNumber)
        {
            _column = column;
            _value = value;
            _style = style;
            _isNumber = isNumber;
        }

        public static Cell Text(string column, string value, int style) => new(column, value, style, false);

        public static Cell Number(string column, double value, int style) =>
            new(column, value.ToString("0.############", CultureInfo.InvariantCulture), style, true);

        public void Write(XmlWriter writer, int rowIndex)
        {
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", $"{_column}{rowIndex}");
            writer.WriteAttributeString("s", _style.ToString(CultureInfo.InvariantCulture));
            if (_isNumber)
            {
                writer.WriteStartElement("v");
                writer.WriteString(_value);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteAttributeString("t", "inlineStr");
                writer.WriteStartElement("is");
                writer.WriteStartElement("t");
                writer.WriteString(_value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
