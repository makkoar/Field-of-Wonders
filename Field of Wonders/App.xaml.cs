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
            ShowAndLogCriticalError(errorMessage, "Критическая ошибка");
            Shutdown(1);
            return;
        }
        LoggingService.Logger.Information("Найдено {Count} языков: {CultureCodes}", supportedLanguages.Count, string.Join(", ", supportedLanguages.Select(l => l.CultureCode)));

        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        LoggingService.Logger.Information("Инициализация главного окна...");
        MainWindow? mainWindow = new();

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
                Shutdown(0);
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
            ShowAndLogCriticalError(errorMessage, Lang.ResourceManager.GetString("Error_Critical_Title") ?? "Критическая ошибка");
            Shutdown(1);
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
        // Исключение может произойти до установки культуры, используем Lang с осторожностью
        if (e.ExceptionObject is Exception exception)
        {
            LoggingService.Logger.Fatal(exception, Lang.Log_UnhandledExceptionAppDomain, e.IsTerminating);
            string errorMessage = string.Format(Lang.Error_UnhandledException_Format, exception.Message);
            ShowAndLogCriticalError(errorMessage, Lang.ResourceManager.GetString("Error_Critical_Title") ?? "Критическая ошибка");
        }
        else
        {
            LoggingService.Logger.Fatal(Lang.Log_UnhandledExceptionAppDomain_NoException, e.IsTerminating);
            ShowAndLogCriticalError(Lang.Error_UnhandledException_NoExceptionObject, Lang.ResourceManager.GetString("Error_Critical_Title") ?? "Критическая ошибка");
        }
    }

    /// <summary>Обработчик необработанных исключений в Dispatcher (UI thread).</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Исключение может произойти до установки культуры, используем Lang с осторожностью
        LoggingService.Logger.Fatal(e.Exception, Lang.Log_UnhandledExceptionDispatcher);
        string errorMessage = string.Format(Lang.Error_DispatcherUnhandledException_Format, e.Exception.Message);
        ShowAndLogCriticalError(errorMessage, Lang.ResourceManager.GetString("Error_Critical_Title") ?? "Критическая ошибка");
        e.Handled = true; // Предотвращаем стандартное завершение приложения
    }

    #endregion

    #region Внутренние Методы

    /// <summary>Отображает критическое сообщение об ошибке и логирует его как Fatal.</summary>
    /// <param name="message">Текст сообщения (предпочтительно уже локализованный).</param>
    /// <param name="title">Заголовок окна сообщения (предпочтительно локализованный или строка по умолчанию).</param>
    internal static void ShowAndLogCriticalError(string message, string title = "Критическая ошибка")
    {
        LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", message);
        try
        {
            if (Current?.Dispatcher != null)
            {
                _ = Current.Dispatcher.Invoke(() =>
                    _ = MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            else
            {
                LoggingService.Logger.Error("Не удалось получить доступ к Current.Dispatcher для отображения критической ошибки.");
                Environment.FailFast(message); // Аварийное завершение
            }
        }
        catch (Exception exInner)
        {
            LoggingService.Logger.Fatal(exInner, "Критическая ошибка (не удалось показать MessageBox.Show)");
            Environment.FailFast($"{message} (MessageBox.Show failed: {exInner.Message})"); // Аварийное завершение
        }
    }

    #endregion
}