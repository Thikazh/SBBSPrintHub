using Microsoft.Extensions.Logging;

namespace SBBSPrintHub;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IPrinterCounterService, PrinterCounterService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
