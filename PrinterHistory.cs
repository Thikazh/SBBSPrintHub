namespace SBBSPrintHub;

public sealed class PrinterHistory
{
    public string PrinterIP { get; set; } = string.Empty;
    public DateTime CounterDateTime { get; set; }
    public int TotalPrintedPages { get; set; }
}

public sealed class PrinterReportRow
{
    public string PrinterIP { get; init; } = string.Empty;
    public DateTime FromDate { get; init; }
    public int FromCount { get; init; }
    public DateTime ToDate { get; init; }
    public int ToCount { get; init; }
    public int PrintedPages => ToCount - FromCount;
}
