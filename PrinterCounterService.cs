using System.Globalization;
using System.Runtime.InteropServices;

namespace SBBSPrintHub;

public sealed class PrinterCounterService : IPrinterCounterService
{
    private const string Header = "PrinterIP,CounterDateTime,TotalPrintedPages";

    public string HistoryFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "PrinterHistory",
        "PrinterCountHistory.csv");

    public async Task<PrinterHistory> CaptureCurrentCountAsync(string printerIp, CancellationToken cancellationToken = default)
    {
        var pageCount = await Task.Run(() => ReadLivePageCount(printerIp), cancellationToken);
        var record = new PrinterHistory
        {
            PrinterIP = printerIp,
            CounterDateTime = DateTime.Now,
            TotalPrintedPages = pageCount
        };

        await SaveHistoryAsync(record, cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<PrinterHistory>> ReadHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(HistoryFilePath))
        {
            return Array.Empty<PrinterHistory>();
        }

        var lines = await File.ReadAllLinesAsync(HistoryFilePath, cancellationToken);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(ParseHistoryLine)
            .Where(record => record is not null)
            .Cast<PrinterHistory>()
            .OrderBy(record => record.CounterDateTime)
            .ToList();
    }

    public PrinterReportRow? BuildReport(string printerIp, DateTime fromDate, DateTime toDate, IEnumerable<PrinterHistory> history)
    {
        var fromRecord = history
            .Where(x => x.PrinterIP == printerIp && x.CounterDateTime <= fromDate)
            .OrderByDescending(x => x.CounterDateTime)
            .FirstOrDefault();

        var toRecord = history
            .Where(x => x.PrinterIP == printerIp && x.CounterDateTime <= toDate)
            .OrderByDescending(x => x.CounterDateTime)
            .FirstOrDefault();

        return fromRecord is null || toRecord is null
            ? null
            : new PrinterReportRow
            {
                PrinterIP = printerIp,
                FromDate = fromRecord.CounterDateTime,
                FromCount = fromRecord.TotalPrintedPages,
                ToDate = toRecord.CounterDateTime,
                ToCount = toRecord.TotalPrintedPages
            };
    }

    private async Task SaveHistoryAsync(PrinterHistory record, CancellationToken cancellationToken)
    {
        var folder = Path.GetDirectoryName(HistoryFilePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var fileExists = File.Exists(HistoryFilePath);
        await using var writer = new StreamWriter(HistoryFilePath, append: true);

        if (!fileExists)
        {
            await writer.WriteLineAsync(Header.AsMemory(), cancellationToken);
        }

        var line = string.Join(',',
            record.PrinterIP,
            record.CounterDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            record.TotalPrintedPages.ToString(CultureInfo.InvariantCulture));
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    private static PrinterHistory? ParseHistoryLine(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != 3 ||
            !DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var counterDateTime) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var totalPrintedPages))
        {
            return null;
        }

        return new PrinterHistory
        {
            PrinterIP = parts[0],
            CounterDateTime = counterDateTime,
            TotalPrintedPages = totalPrintedPages
        };
    }

    private static int ReadLivePageCount(string printerIp)
    {
#if WINDOWS
        dynamic? snmp = null;
        try
        {
            snmp = Activator.CreateInstance(Type.GetTypeFromProgID("olePrn.OleSNMP")!);
            snmp.Open(printerIp, "public", 2, 2000);
            return Convert.ToInt32(snmp.Get(".1.3.6.1.2.1.43.10.2.1.4.1.1"));
        }
        finally
        {
            if (snmp is not null)
            {
                try
                {
                    snmp.Close();
                    Marshal.ReleaseComObject(snmp);
                }
                catch
                {
                }
            }
        }
#else
        throw new PlatformNotSupportedException("Live SNMP capture through olePrn.OleSNMP is available only on Windows.");
#endif
    }
}
