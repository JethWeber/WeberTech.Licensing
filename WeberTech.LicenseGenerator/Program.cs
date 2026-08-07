using Avalonia;

namespace WeberTech.LicenseGenerator;

internal static class Program
{
    // Ponto de entrada. NÃO inicializar Avalonia dentro de AppMain (evita problemas de reflexão/AOT).
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
