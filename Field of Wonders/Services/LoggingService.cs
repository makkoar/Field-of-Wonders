namespace Field_of_Wonders.Services;

/// <summary>Предоставляет статический доступ к настроенному экземпляру логгера Serilog.</summary>
public static class LoggingService
{
    /// <summary>Экземпляр логгера, инициализируемый при первом обращении.</summary>
    private static readonly Lazy<ILogger> _lazyLogger = new(InitializeLogger, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Получает основной экземпляр логгера приложения.</summary>
    public static ILogger Logger => _lazyLogger.Value;

    /// <summary>Инициализирует глобальный логгер Serilog с настроенными приемниками.</summary>
    /// <returns>Настроенный экземпляр логгера.</returns>
    private static ILogger InitializeLogger()
    {
        string logsDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        string logFilePath = Path.Combine(logsDirectory, "log-.txt");
        string fatalLogFilePath = Path.Combine(logsDirectory, "log-fatal-.txt");
        string errorsOnlyFilePath = Path.Combine(logsDirectory, "log-errors-only-.txt");
        string fullLogsFilePath = Path.Combine(logsDirectory, "log-full-.txt");

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information() // Глобальный минимальный уровень, фильтрация происходит на уровне приемников (Sinks).
            .Enrich.FromLogContext(); // Добавляет контекстную информацию (например, из LogContext.PushProperty).

        // Лог 1: Инфо и выше (log-.txt)
        _ = loggerConfiguration.WriteTo.File(
            logFilePath,
            restrictedToMinimumLevel: LogEventLevel.Information,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8 // Явное указание кодировки UTF-8
        );

        // Лог 2: Полные логи (Инфо, Варнинг, Еррор, Фатал) (log-full-.txt)
        _ = loggerConfiguration.WriteTo.File(
            fullLogsFilePath,
            restrictedToMinimumLevel: LogEventLevel.Debug,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 3,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}" // SourceContext убран
        );

        // Лог 3: Только ошибки (log-errors-only-.txt)
        _ = loggerConfiguration.WriteTo.File(
            errorsOnlyFilePath,
            restrictedToMinimumLevel: LogEventLevel.Error,
            rollingInterval: RollingInterval.Month, // Реже ротация для ошибок
            retainedFileCountLimit: 12,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8
        );


        // Лог критических ошибок (Fatal)
        _ = loggerConfiguration.WriteTo.File(
            fatalLogFilePath,
            restrictedToMinimumLevel: LogEventLevel.Fatal,
            rollingInterval: RollingInterval.Month,
            retainedFileCountLimit: 12,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8
        );

        Log.Logger = loggerConfiguration.CreateLogger();

        // Логгируем сообщение о старте.
        // На данном этапе Lang будет использовать ресурсы по умолчанию (вероятно, русский), т.к. культура приложения еще не установлена.
        Log.Information(new string('=', 80));
        Log.Information(Lang.Log_AppStarting);

        return Log.Logger;
    }

    /// <summary>Закрывает и сбрасывает буферы логгера Serilog, обеспечивая запись всех оставшихся сообщений.</summary>
    public static void CloseAndFlushLogger()
    {
        // Используем локализованную строку, если культура уже была применена,
        // иначе будет использована строка из Lang.resx (ресурс по умолчанию).
        Logger.Information(Lang.Log_FlushingLogs);
        Log.CloseAndFlush();
    }
}