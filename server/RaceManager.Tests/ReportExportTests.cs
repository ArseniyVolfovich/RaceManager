using System.IO.Compression;
using System.Reflection;
using RaceManager.Api.Controllers;
using RaceManager.Application.Services;

namespace RaceManager.Tests;

public sealed class ReportExportTests
{
    [Fact]
    public void BuildXlsx_CreatesRealOpenXmlWorkbook()
    {
        var document = new ReportDocument(
            "22RT Time Attack Demo",
            "22RT",
            "Автодром Стайки",
            DateTime.UtcNow,
            new[] { "Позиция", "Пилот", "Очки" },
            new IReadOnlyList<string>[] { new[] { "1", "Алексей Морозов", "25" } },
            new[] { "Участников: 22" });
        var method = typeof(ReportsController).GetMethod("BuildXlsx", BindingFlags.NonPublic | BindingFlags.Static);

        var bytes = Assert.IsType<byte[]>(method!.Invoke(null, new object[] { document }));
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
        Assert.NotNull(archive.GetEntry("[Content_Types].xml"));
    }
}
