namespace PaymentProcessorService.StartupExtensions.Logging;

public sealed class LoggingSettings
{
    public const string SectionName = "Logging";
    public const string AppName = "PaymentProcessorService";

    public string Environment { get; init; } = "Development";
    public string MinimumLevel { get; init; } = "Information";
    public string SeqUrl { get; init; } = "http://localhost:5341";
    public bool EnableConsole { get; init; } = true;
    public string LogFilePath { get; init; } = "logs/payment-processor-service-.log";
    public int RetainedFileCountLimit { get; init; } = 31;
}