namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, инициализирует локализацию и главное окно.</summary>
public partial class App : Application
{
    private readonly LocalizationService _localizationService = new();

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
            LoggingService.Logger.Warning("Поддерживаемые языки не найдены. Добавляются резервные варианты.");
            EnsureFallbackLanguages(supportedLanguages);
            if (supportedLanguages.Count is 0)
            {
                string errorMessage = Lang.Error_Critical_NoLanguagesFound;
                LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", errorMessage);
                ShowAndLogCriticalError(errorMessage);
                Shutdown(1);
                return;
            }
        }
        LoggingService.Logger.Information("Найдено {Count} языков: {CultureCodes}", supportedLanguages.Count, string.Join(", ", supportedLanguages.Select(l => l.CultureCode)));

        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        MainWindow mainWindow = new();
        LoggingService.Logger.Information("Инициализация главного окна...");

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
                string defaultCulture = supportedLanguages.First().CultureCode;
                LoggingService.Logger.Warning("Пользователь закрыл окно выбора языка. Используется язык по умолчанию: {DefaultCultureCode}", defaultCulture);
                selectedCultureName = defaultCulture;
            }
        }

        string cultureToApply = selectedCultureName ?? supportedLanguages.First().CultureCode;
        LoggingService.Logger.Information("Попытка применить культуру: {CultureCode}", cultureToApply);
        bool cultureApplied = _localizationService.ApplyCulture(cultureToApply);

        if (!cultureApplied)
        {
            string fallbackCulture = supportedLanguages.First().CultureCode;
            LoggingService.Logger.Warning("Не удалось применить культуру {FailedCultureCode}. Попытка применить резервную культуру: {FallbackCultureCode}...", cultureToApply, fallbackCulture);
            cultureApplied = _localizationService.ApplyCulture(fallbackCulture);
            if (cultureApplied)
            {
                LoggingService.Logger.Information("Резервная культура {FallbackCultureCode} успешно применена.", fallbackCulture);
            }
            else
            {
                string errorMessage = Lang.Error_Critical_CannotSetAnyLanguage;
                LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", errorMessage);
                ShowAndLogCriticalError(errorMessage);
                Shutdown(1);
                return;
            }
        }

        LoggingService.Logger.Information(Lang.Log_ApplyingCulture_Success, _localizationService.CurrentAppliedCulture?.Name ?? "Неизвестно");

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
        if (e.ExceptionObject is Exception exception)
        {
            LoggingService.Logger.Fatal(exception, Lang.Log_UnhandledExceptionAppDomain, e.IsTerminating);
            string errorMessage = string.Format(Lang.Error_UnhandledException_Format, exception.Message);
            ShowAndLogCriticalError(errorMessage);
        }
        else
        {
            LoggingService.Logger.Fatal(Lang.Log_UnhandledExceptionAppDomain_NoException, e.IsTerminating);
            ShowAndLogCriticalError(Lang.Error_UnhandledException_NoExceptionObject);
        }
    }

    /// <summary>Обработчик необработанных исключений в Dispatcher (UI thread).</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggingService.Logger.Fatal(e.Exception, Lang.Log_UnhandledExceptionDispatcher);
        string errorMessage = string.Format(Lang.Error_DispatcherUnhandledException_Format, e.Exception.Message);
        ShowAndLogCriticalError(errorMessage);
        e.Handled = true;
    }

    /// <summary>Отображает критическое сообщение об ошибке и логирует его как Fatal. Использует локализованные строки.</summary>
    /// <param name="message">Текст сообщения (уже локализованный).</param>
    internal static void ShowAndLogCriticalError(string message)
    {
        LoggingService.Logger.Fatal("Критическая ошибка: {ErrorMessage}", message);
        try
        {
            if (Current?.Dispatcher != null)
            {
                _ = Current.Dispatcher.Invoke(() =>
                    _ = MessageBox.Show(message, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            else
            {
                LoggingService.Logger.Error("Не удалось получить доступ к Current.Dispatcher для отображения критической ошибки.");
                Environment.FailFast(message);
            }
        }
        catch (Exception exInner)
        {
            LoggingService.Logger.Fatal(exInner, "Критическая ошибка (не удалось показать MessageBox.Show)");
            Environment.FailFast($"{message} (MessageBox.Show failed: {exInner.Message})");
        }
    }

    /// <summary>Обеспечивает добавление резервных языков в список, если автоматическое обнаружение не дало результатов.</summary>
    /// <param name="languages">Список языков для проверки и пополнения.</param>
    private static void EnsureFallbackLanguages(List<LanguageInfo> languages)
    {
        if (languages.Count > 0) return;

        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture;

        TryAddFallbackCulture(languages, currentUiCulture, "ru-RU");
        if (languages.Count > 0) return;

        TryAddFallbackCulture(languages, currentUiCulture, "en-US");
        if (languages.Count > 0) return;

        try
        {
            CultureInfo sysCulture = CultureInfo.InstalledUICulture;
            TryAddFallbackCulture(languages, currentUiCulture, sysCulture.Name);
            if (languages.Count > 0) return;
        }
        catch (Exception sysCultureEx)
        {
            LoggingService.Logger.Warning(sysCultureEx, Lang.Log_SystemCultureAddFailed);
        }

        if (languages.Count == 0)
        {
            languages.Add(new LanguageInfo("Русский (Резерв)", "ru-RU"));
            LoggingService.Logger.Error("Ни один язык не был обнаружен или добавлен. Добавлен жестко закодированный русский язык как резервный.");
        }
    }

    /// <summary>Вспомогательный метод для попытки добавления резервной культуры в список.</summary>
    /// <param name="languages">Список языков для пополнения.</param>
    /// <param name="currentUiCulture">Текущая UI культура для форматирования имени языка.</param>
    /// <param name="cultureCodeToAdd">Код культуры для добавления.</param>
    private static void TryAddFallbackCulture(List<LanguageInfo> languages, CultureInfo currentUiCulture, string cultureCodeToAdd)
    {
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureCodeToAdd);
            if (!languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
            {
                string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName) + " (Резерв)";
                languages.Add(new LanguageInfo(displayName, culture.Name));
                LoggingService.Logger.Debug(Lang.Log_AddedCultureToList, cultureCodeToAdd);
            }
        }
        catch (CultureNotFoundException)
        {
            LoggingService.Logger.Warning(Lang.Log_CultureAddNotFound, cultureCodeToAdd);
        }
    }
}