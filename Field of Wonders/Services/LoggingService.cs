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
        string debugLogFilePath = Path.Combine(logsDirectory, "log-debug-.txt");
        string fatalLogFilePath = Path.Combine(logsDirectory, "log-fatal-.txt");

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug() // Глобальный минимальный уровень, фильтрация происходит на уровне приемников (Sinks).
            .Enrich.FromLogContext(); // Добавляет контекстную информацию (например, из LogContext.PushProperty).

        // Основной лог (Information и выше)
        loggerConfiguration.WriteTo.File(
            logFilePath,
            restrictedToMinimumLevel: LogEventLevel.Information,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        );

#if DEBUG
        // Дебаг-лог (Debug и выше) - только для DEBUG сборок
        loggerConfiguration.WriteTo.File(
            debugLogFilePath,
            restrictedToMinimumLevel: LogEventLevel.Debug,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 3,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}" // Включаем SourceContext для отладки.
        );
#endif

        // Лог критических ошибок (Fatal)
        loggerConfiguration.WriteTo.File(
            fatalLogFilePath,
            restrictedToMinimumLevel: LogEventLevel.Fatal,
            rollingInterval: RollingInterval.Month,
            retainedFileCountLimit: 12,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        );

        Log.Logger = loggerConfiguration.CreateLogger();

        // Логгируем сообщение о старте.
        // На данном этапе Lang будет использовать ресурсы по умолчанию (вероятно, русский), т.к. культура приложения еще не установлена.
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