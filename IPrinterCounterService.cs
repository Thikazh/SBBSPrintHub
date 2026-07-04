namespace SBBSPrintHub;

public interface IPrinterCounterService
{
    string HistoryFilePath { get; }
    Task<PrinterHistory> CaptureCurrentCountAsync(string printerIp, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrinterHistory>> ReadHistoryAsync(CancellationToken cancellationToken = default);
    PrinterReportRow? BuildReport(string printerIp, DateTime fromDate, DateTime toDate, IEnumerable<PrinterHistory> history);
}
