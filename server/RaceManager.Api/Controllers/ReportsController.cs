using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Security;
using RaceManager.Application.Services;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(ReportService reports, JudgeResultService resultService, EventService eventService, EventJudgeService eventJudgeService, StartListService startListService) : ControllerBase
{
    [HttpGet("events/{eventId}")]
    public async Task<IActionResult> EventReport(string eventId, [FromQuery] string format = "txt", CancellationToken cancellationToken = default) =>
        Export(await reports.BuildEventReportAsync(eventId, cancellationToken), format, $"event-{eventId}-report");

    [HttpGet("pilots/{userId}")]
    public async Task<IActionResult> PilotReport(string userId, [FromQuery] string format = "txt", CancellationToken cancellationToken = default) =>
        Export(await reports.BuildPilotReportAsync(userId, cancellationToken), format, $"pilot-{userId}-report");

    [HttpGet("teams/{teamName}")]
    public async Task<IActionResult> TeamReport(string teamName, [FromQuery] string format = "txt", CancellationToken cancellationToken = default) =>
        Export(await reports.BuildTeamReportAsync(teamName, cancellationToken), format, $"team-{SafeName(teamName)}-report");

    [HttpGet("championships/{idOrName}")]
    public async Task<IActionResult> ChampionshipReport(string idOrName, [FromQuery] string format = "txt", CancellationToken cancellationToken = default) =>
        Export(await reports.BuildChampionshipReportAsync(idOrName, cancellationToken), format, $"championship-{SafeName(idOrName)}-report");

    [HttpGet("events/{eventId}/start-list")]
    public async Task<IActionResult> StartListReport(string eventId, [FromQuery] string format = "txt", CancellationToken cancellationToken = default)
    {
        var raceEvent = await eventService.GetByIdAsync(eventId, cancellationToken);
        var entries = await startListService.GetAsync(eventId, cancellationToken);
        var rows = entries.OrderBy(entry => entry.StartPosition).Select(entry => (IReadOnlyList<string>)new[]
        {
            entry.StartPosition.ToString(), entry.StartNumber.ToString(), entry.DriverName,
            entry.TeamName, entry.CarName, entry.ClassName
        }).ToList();
        var document = new ReportDocument(
            $"Стартовый список: {raceEvent.Title}",
            string.IsNullOrWhiteSpace(raceEvent.OrganizerName) ? "Не указан" : raceEvent.OrganizerName,
            raceEvent.Track,
            DateTime.UtcNow,
            new[] { "Стартовая позиция", "Стартовый номер", "Пилот", "Команда", "Автомобиль", "Класс" },
            rows,
            new[] { $"Участников: {rows.Count}", $"Дата события: {raceEvent.Date}" });
        return Export(document, format, $"event-{eventId}-start-list");
    }

    [HttpPost("import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportPreview([FromForm] IFormFile file, [FromForm] string? eventId, [FromForm] bool saveResults, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequest(new { message = "Файл пустой." });
        await using var stream = file.OpenReadStream();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string text;
        try
        {
            text = extension switch
            {
                ".txt" => await ReadTxtAsync(stream, cancellationToken),
                ".docx" => ReadDocx(stream),
                ".pdf" => ReadPdfPreview(stream),
                _ => throw new InvalidOperationException("Поддерживаются только TXT, DOCX и PDF.")
            };
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }

        var rows = ParseImportedResults(text).ToList();
        var imported = 0;
        if (saveResults)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return BadRequest(new { message = "Для сохранения результатов выберите событие." });
            var userId = User.RequireUserId();
            if (User.IsInRole("Организатор")) await eventService.RequireOrganizerAccessAsync(eventId, userId, cancellationToken);
            else if (User.IsInRole("Судья"))
            {
                if (!DemoAccess.IsGlobalJudge(userId) &&
                    !await eventJudgeService.IsAssignedAsync(eventId, userId, cancellationToken)) return Forbid();
            }
            else return Forbid();

            foreach (var row in rows)
            {
                await resultService.UpsertAsync(new UpsertRaceResultRequest(
                    eventId,
                    null,
                    null,
                    row.DriverName,
                    row.Position,
                    row.FinalTime,
                    row.BestLap,
                    row.Points,
                    row.Status,
                    User.IsInRole("Судья") ? userId : null,
                    null,
                    null,
                    null,
                    ParseTimeMs(row.Penalty),
                    ParseTimeMs(row.FinalTime),
                    row.ClassName,
                    row.CarName,
                    row.DriverNumber), cancellationToken);
                imported++;
            }
        }

        return Ok(new
        {
            file = file.FileName,
            extension,
            characters = text.Length,
            preview = text.Length > 4000 ? text[..4000] : text,
            parsedRows = rows,
            imported
        });
    }


    private sealed record ImportedResultRow(
        int Position,
        string DriverNumber,
        string DriverName,
        string CarName,
        string ClassName,
        string BestLap,
        string Penalty,
        string FinalTime,
        int Points,
        string Status);

    private static IEnumerable<ImportedResultRow> ParseImportedResults(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length < 3) continue;
            if (line.Contains("Позиция", StringComparison.OrdinalIgnoreCase) || line.Contains("Driver", StringComparison.OrdinalIgnoreCase)) continue;
            var cells = SplitImportLine(line);
            if (cells.Length < 3) continue;

            var position = ParseInt(cells.ElementAtOrDefault(0));
            if (position <= 0) continue;
            var driverNumber = cells.ElementAtOrDefault(1) ?? string.Empty;
            var driverName = cells.ElementAtOrDefault(2) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(driverName)) continue;
            var carName = cells.ElementAtOrDefault(3) ?? string.Empty;
            var className = cells.ElementAtOrDefault(4) ?? string.Empty;
            var bestLap = cells.ElementAtOrDefault(5) ?? string.Empty;
            var penalty = cells.ElementAtOrDefault(6) ?? string.Empty;
            var finalTime = cells.ElementAtOrDefault(7) ?? bestLap;
            var points = ParseInt(cells.ElementAtOrDefault(8));
            var status = cells.ElementAtOrDefault(9) ?? "Финишировал";
            yield return new ImportedResultRow(position, driverNumber, driverName, carName, className, bestLap, penalty, finalTime, points, status);
        }
    }

    private static string[] SplitImportLine(string line)
    {
        if (line.Contains('\t')) return line.Split('\t', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (line.Contains(';')) return line.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return Regex.Split(line, @"\s{2,}").Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()).ToArray();
    }

    private static int ParseInt(string? value) => int.TryParse(Regex.Match(value ?? string.Empty, @"\d+").Value, out var number) ? number : 0;

    private static int? ParseTimeMs(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—") return null;
        var text = value.Trim().Replace(',', '.');
        var match = Regex.Match(text, @"(?:(\d+):)?(\d+(?:\.\d+)?)");
        if (!match.Success) return null;
        var minutes = match.Groups[1].Success ? decimal.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
        var seconds = decimal.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        return (int)Math.Round((minutes * 60 + seconds) * 1000);
    }

    private IActionResult Export(ReportDocument document, string format, string fileName)
    {
        var normalized = format.Trim().ToLowerInvariant();
        return normalized switch
        {
            "docx" or "word" => File(BuildDocx(document), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"{fileName}.docx"),
            "xlsx" or "excel" => File(BuildXlsx(document), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileName}.xlsx"),
            "pdf" => File(BuildPdf(document), "application/pdf", $"{fileName}.pdf"),
            _ => File(Encoding.UTF8.GetBytes(BuildTxt(document)), "text/plain; charset=utf-8", $"{fileName}.txt")
        };
    }

    private static string BuildTxt(ReportDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine(document.Title);
        builder.AppendLine($"Дата формирования: {document.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
        builder.AppendLine($"Организатор: {document.Organizer}");
        builder.AppendLine($"Трасса: {document.Track}");
        foreach (var line in document.Summary) builder.AppendLine(line);
        builder.AppendLine();
        builder.AppendLine(string.Join('\t', document.Headers));
        foreach (var row in document.Rows) builder.AppendLine(string.Join('\t', row));
        return builder.ToString();
    }

    private static byte[] BuildDocx(ReportDocument document)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>
                """);
            WriteEntry(archive, "word/document.xml", BuildWordDocumentXml(document));
        }
        return output.ToArray();
    }

    private static string BuildWordDocumentXml(ReportDocument document)
    {
        var body = new StringBuilder();
        Paragraph(body, document.Title);
        Paragraph(body, $"Дата формирования: {document.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}");
        Paragraph(body, $"Организатор: {document.Organizer}");
        Paragraph(body, $"Трасса: {document.Track}");
        foreach (var line in document.Summary) Paragraph(body, line);
        body.Append("<w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\"/></w:tblPr>");
        TableRow(body, document.Headers);
        foreach (var row in document.Rows) TableRow(body, row);
        body.Append("</w:tbl>");
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>{body}<w:sectPr/></w:body></w:document>";
    }

    private static byte[] BuildXlsx(ReportDocument document)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(document));
        }
        return output.ToArray();
    }

    private static string BuildWorksheetXml(ReportDocument document)
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { document.Title },
            new[] { $"Дата формирования: {document.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}" },
            new[] { $"Организатор: {document.Organizer}" },
            new[] { $"Трасса: {document.Track}" }
        };
        rows.AddRange(document.Summary.Select(line => (IReadOnlyList<string>)new[] { line }));
        rows.Add(Array.Empty<string>());
        rows.Add(document.Headers);
        rows.AddRange(document.Rows);

        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append("<row r=\"").Append(rowIndex + 1).Append("\">");
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                builder.Append("<c r=\"").Append(ColumnName(columnIndex + 1)).Append(rowIndex + 1).Append("\" t=\"inlineStr\"><is><t>").Append(Xml(rows[rowIndex][columnIndex])).Append("</t></is></c>");
            }
            builder.Append("</row>");
        }
        builder.Append("</sheetData></worksheet>");
        return builder.ToString();
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }
        return name;
    }

    private static byte[] BuildPdf(ReportDocument document)
    {
        var lines = BuildTxt(document).Split('\n').Select(ToPdfSafe).Take(42).ToList();
        var content = new StringBuilder("BT /F1 10 Tf 40 800 Td 14 TL\n");
        foreach (var line in lines) content.Append('(').Append(EscapePdf(line)).Append(") Tj T*\n");
        content.Append("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {stream.Length} >> stream\n{content}\nendstream endobj\n"
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects) { offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString())); pdf.Append(obj); }
        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) pdf.Append(offset.ToString("0000000000")).Append(" 00000 n \n");
        pdf.Append($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static async Task<string> ReadTxtAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static string ReadDocx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidOperationException("DOCX не содержит document.xml.");
        using var reader = new StreamReader(entry.Open());
        var xml = reader.ReadToEnd();
        return Regex.Replace(xml.Replace("</w:p>", "\n"), "<[^>]+>", " ").Replace("  ", " ").Trim();
    }

    private static string ReadPdfPreview(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var raw = Encoding.Latin1.GetString(memory.ToArray());
        var matches = Regex.Matches(raw, @"\(([^)]{1,500})\)\s*Tj").Select(match => match.Groups[1].Value).ToArray();
        return matches.Length == 0 ? "PDF загружен. Текст не найден простым извлечением." : string.Join("\n", matches.Select(UnescapePdf));
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
    }

    private static void Paragraph(StringBuilder body, string text) => body.Append("<w:p><w:r><w:t>").Append(Xml(text)).Append("</w:t></w:r></w:p>");
    private static void TableRow(StringBuilder body, IEnumerable<string> cells)
    {
        body.Append("<w:tr>");
        foreach (var cell in cells) body.Append("<w:tc><w:p><w:r><w:t>").Append(Xml(cell)).Append("</w:t></w:r></w:p></w:tc>");
        body.Append("</w:tr>");
    }

    private static string Xml(string? value) => SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
    private static string SafeName(string value) => Regex.Replace(value, @"[^\p{L}\p{N}]+", "-").Trim('-');
    private static string ToPdfSafe(string value) => Regex.Replace(value.Normalize(NormalizationForm.FormD), @"[^\u0000-\u007F]+", "?").Trim();
    private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "").Replace("\n", " ");
    private static string UnescapePdf(string value) => value.Replace("\\(", "(").Replace("\\)", ")").Replace("\\\\", "\\");
}
