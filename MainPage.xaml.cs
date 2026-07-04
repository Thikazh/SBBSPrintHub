using System.Collections.ObjectModel;

namespace SBBSPrintHub;

public partial class MainPage : ContentPage
{
    private readonly IPrinterCounterService printerCounterService;
    private readonly ObservableCollection<PrinterReportRow> reportRows = new();
    private IReadOnlyList<PrinterHistory> history = Array.Empty<PrinterHistory>();

    public MainPage(IPrinterCounterService printerCounterService)
    {
        InitializeComponent();
        this.printerCounterService = printerCounterService;
        ReportGrid.ItemsSource = reportRows;

        FromDatePicker.Date = DateTime.Today;
        ToDatePicker.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAndFilterAsync();
    }

    private async void OnCaptureClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Reading live printer count...";
            var record = await printerCounterService.CaptureCurrentCountAsync(PrinterIpEntry.Text?.Trim() ?? string.Empty);
            StatusLabel.Text = $"Saved count {record.TotalPrintedPages} at {record.CounterDateTime:yyyy-MM-dd HH:mm:ss}.";
            await LoadHistoryAndFilterAsync();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnFilterClicked(object sender, EventArgs e) => await LoadHistoryAndFilterAsync();

    private async void OnReloadClicked(object sender, EventArgs e) => await LoadHistoryAndFilterAsync();

    private async Task LoadHistoryAndFilterAsync()
    {
        history = await printerCounterService.ReadHistoryAsync();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        reportRows.Clear();
        var printerIp = PrinterIpEntry.Text?.Trim() ?? string.Empty;
        var fromDate = FromDatePicker.Date.Date;
        var toDate = ToDatePicker.Date.Date.AddDays(1).AddTicks(-1);

        if (toDate < fromDate)
        {
            StatusLabel.Text = "To Date must be greater than or equal to From Date.";
            return;
        }

        var row = printerCounterService.BuildReport(printerIp, fromDate, toDate, history);
        if (row is not null)
        {
            reportRows.Add(row);
            StatusLabel.Text = $"Showing printed page count from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}.";
        }
        else
        {
            StatusLabel.Text = "No counter history found for the selected printer/date range. Capture counts regularly to build history.";
        }
    }
}
