namespace Field_of_Wonders.Services;

/// <summary>Предоставляет статический доступ к настроенному экземпляру логгера Serilog.</summary>
public static class LoggingService
{
    #region Поля (статические)

    /// <summary>Экземпляр логгера, инициализируемый при первом обращении.</summary>
    private static readonly Lazy<ILogger> _lazyLogger = new(InitializeLogger, LazyThreadSafetyMode.ExecutionAndPublication);

    #endregion

    #region Свойства (статические)

    /// <summary>Получает основной экземпляр логгера приложения.</summary>
    public static ILogger Logger => _lazyLogger.Value;

    #endregion

    #region Статические Методы

    /// <summary>Инициализирует глобальный логгер Serilog с настроенными приемниками.</summary>
    /// <returns>Настроенный экземпляр логгера.</returns>
    private static ILogger InitializeLogger()
    {
        string logsDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        string logFilePath = Path.Combine(logsDirectory, "log-.txt"); // Info+
        string errorsOnlyFilePath = Path.Combine(logsDirectory, "log-errors-only-.txt"); // Error+
        string fullLogsFilePath = Path.Combine(logsDirectory, "log-full-.txt"); // Verbose+

        const int retainedFileCount = 15; // Хранить логи 15 дней

        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose() // Глобальный минимальный уровень Verbose
            .Enrich.FromLogContext();

        // Лог 1: Основной лог (Information и выше)
        _ = loggerConfiguration.WriteTo.File(
            logFilePath,
            restrictedToMinimumLevel: LogEventLevel.Information,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8
        );

        // Лог 2: Полные логи (Verbose и выше)
        _ = loggerConfiguration.WriteTo.File(
            fullLogsFilePath,
            restrictedToMinimumLevel: LogEventLevel.Verbose,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8
        );

        // Лог 3: Только ошибки (Error и Fatal)
        _ = loggerConfiguration.WriteTo.File(
            errorsOnlyFilePath,
            restrictedToMinimumLevel: LogEventLevel.Error,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: retainedFileCount,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            encoding: System.Text.Encoding.UTF8
        );

        Log.Logger = loggerConfiguration.CreateLogger();

        // Логгируем сообщение о старте НА РУССКОМ ЯЗЫКЕ, так как локаль еще не установлена.
        Log.Information(new string('=', 80));
        Log.Information("Запуск приложения \"Поле Чудес\"...");

        return Log.Logger;
    }

    /// <summary>Закрывает и сбрасывает буферы логгера Serilog, обеспечивая запись всех оставшихся сообщений.</summary>
    public static void CloseAndFlushLogger()
    {
        Logger.Information(Lang.Log_FlushingLogs);
        Log.CloseAndFlush();
    }

    #endregion
}