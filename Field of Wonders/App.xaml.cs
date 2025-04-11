namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, инициализирует локализацию и главное окно.</summary>
public partial class App : Application
{
    #region Поля

    private readonly LocalizationService _localizationService = new();

    #endregion

    #region Обработчики событий приложения

    /// <summary>Инициализирует приложение: определяет язык, применяет культуру и отображает главное окно.</summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Exit += OnAppExit;

        LoggingService.Logger.Information("Обнаружение поддерживаемых языков...");
        List<LanguageInfo> supportedLanguages = LocalizationService.DiscoverSupportedLanguages();

        if (supportedLanguages.Count is 0)
        {
            string errorMessage = "Критическая ошибка: Не найдены поддерживаемые языки (отсутствует даже нейтральный язык).";
            LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", errorMessage);
            // Язык еще не выбран, используем нелокализованный заголовок
            ShowAndLogCriticalError(errorMessage, "Критическая ошибка", useLocalization: false);
            // Shutdown(1) вызывается внутри ShowAndLogCriticalError
            return;
        }
        LoggingService.Logger.Information("Найдено {Count} языков: {CultureCodes}", supportedLanguages.Count, string.Join(", ", supportedLanguages.Select(l => l.CultureCode)));

        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        LoggingService.Logger.Information("Инициализация главного окна...");
        MainWindow? mainWindow = new(); // Создаем заранее, но показываем после локализации

        if (needsSelection)
        {
            LoggingService.Logger.Information("Требуется выбор языка.");
            LanguageSelectionWindow selectionWindow = new(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                LoggingService.Logger.Information("Пользователь выбрал язык: {CultureCode}", selectedCultureName);
                AppSettings newSettings = settings ?? new AppSettings();
                newSettings.SelectedCulture = selectedCultureName;
                if (!SettingsService.SaveSettings(newSettings))
                {
                    LoggingService.Logger.Warning("Не удалось сохранить настройку языка.");
                }
            }
            else
            {
                LoggingService.Logger.Warning("Окно выбора языка было закрыто пользователем без подтверждения. Завершение работы приложения.");
                // mainWindow?.Close(); // <-- Удалено: Избыточно, т.к. Shutdown(0) закроет все окна.
                Shutdown(0); // Завершаем приложение с кодом 0 (успех, но без запуска)
                return;
            }
        }

        string cultureToApply = selectedCultureName!;
        LoggingService.Logger.Information("Попытка применить культуру: {CultureCode}", cultureToApply);
        bool cultureApplied = _localizationService.ApplyCulture(cultureToApply);

        if (!cultureApplied)
        {
            string errorMessage = $"Критическая ошибка: Не удалось применить необходимую культуру '{cultureToApply}'.";
            LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", errorMessage);
            // Пытаемся использовать локализацию для заголовка, но с fallback'ом
            ShowAndLogCriticalError(errorMessage, useLocalization: true); // По умолчанию "Критическая ошибка" если Lang недоступен
            // Shutdown(1) вызывается внутри ShowAndLogCriticalError
            return;
        }

        LoggingService.Logger.Information(Lang.Log_ApplyingCulture_Success, _localizationService.CurrentAppliedCulture?.Name ?? "Unknown");
        LoggingService.Logger.Information(Lang.Log_MainWindowOpening);
        mainWindow.Show();
        LoggingService.Logger.Information(Lang.Log_MainWindowInitialized);
    }

    /// <summary>Обработчик события завершения приложения, обеспечивающий сброс логов.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnAppExit(object? sender, ExitEventArgs e)
    {
        LoggingService.Logger.Information(Lang.Log_AppExiting, e.ApplicationExitCode);
        LoggingService.CloseAndFlushLogger();
    }

    /// <summary>Обработчик необработанных исключений в AppDomain.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Локализация может быть недоступна, используем ее с осторожностью
        if (e.ExceptionObject is Exception exception)
        {
            LoggingService.Logger.Fatal(exception, Lang.Log_UnhandledExceptionAppDomain ?? "Unhandled exception in AppDomain (terminating: {IsTerminating})", e.IsTerminating);
            string errorMessage = string.Format(Lang.Error_UnhandledException_Format ?? "Unhandled exception occurred: {0}", exception.Message);
            ShowAndLogCriticalError(errorMessage, useLocalization: true); // Пытаемся использовать локализацию для заголовка
        }
        else
        {
            LoggingService.Logger.Fatal(Lang.Log_UnhandledExceptionAppDomain_NoException ?? "Unhandled exception in AppDomain (no Exception object, terminating: {IsTerminating})", e.IsTerminating);
            ShowAndLogCriticalError(Lang.Error_UnhandledException_NoExceptionObject ?? "An unknown unhandled exception occurred.", useLocalization: true);
        }
        // Неявное завершение приложения здесь, так как ShowAndLogCriticalError вызывает Shutdown/FailFast
    }

    /// <summary>Обработчик необработанных исключений в Dispatcher (UI thread).</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Локализация должна быть доступна, если ошибка произошла после инициализации
        LoggingService.Logger.Fatal(e.Exception, Lang.Log_UnhandledExceptionDispatcher);
        string errorMessage = string.Format(Lang.Error_DispatcherUnhandledException_Format, e.Exception.Message);
        ShowAndLogCriticalError(errorMessage, useLocalization: true);
        e.Handled = true; // Предотвращаем стандартное завершение приложения, т.к. ShowAndLogCriticalError его выполнит.
    }

    #endregion

    #region Внутренние Статические Методы

    /// <summary>Отображает критическое сообщение об ошибке, логирует его и завершает приложение.</summary>
    /// <param name="message">Текст сообщения (может быть локализованным или нет).</param>
    /// <param name="defaultTitle">Заголовок окна сообщения по умолчанию, если локализация не используется или недоступна.</param>
    /// <param name="useLocalization">Указывает, следует ли пытаться использовать локализованный заголовок из ресурсов.</param>
    internal static void ShowAndLogCriticalError(string message, string defaultTitle = "Критическая ошибка", bool useLocalization = true)
    {
        LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", message);

        string title = defaultTitle;
        if (useLocalization)
        {
            // Пытаемся получить локализованный заголовок, если не удается - используем defaultTitle
            title = Lang.ResourceManager.GetString("Error_Critical_Title") ?? defaultTitle;
        }

        try
        {
            // Проверяем, доступен ли Dispatcher текущего приложения
            if (Current?.Dispatcher != null && Current.Dispatcher.CheckAccess())
            {
                _ = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (Current?.Dispatcher != null) // Если нужен Invoke
            {
                _ = Current.Dispatcher.Invoke(() =>
                    _ = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            else
            {
                // Если Dispatcher недоступен (очень ранняя ошибка), логгируем и завершаем
                LoggingService.Logger.Error("Не удалось получить доступ к Current.Dispatcher для отображения критической ошибки.");
                Environment.FailFast($"Критическая ошибка (UI недоступен): {message}");
            }
        }
        catch (Exception exInner)
        {
            LoggingService.Logger.Fatal(exInner, "Критическая ошибка при попытке показать MessageBox: {ErrorMessage}", exInner.Message);
            Environment.FailFast($"{message} (MessageBox.Show failed: {exInner.Message})"); // Аварийное завершение
        }

        // Завершаем приложение после отображения сообщения
        if (Current != null)
        {
            Current.Shutdown(1); // Код 1 - ошибка
        }
        else
        {
            Environment.Exit(1); // Запасной вариант завершения
        }
    }

    #endregion
}