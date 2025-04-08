using System.Windows.Threading;

namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, инициализирует локализацию и главное окно.</summary>
public partial class App : Application
{
    private readonly LocalizationService _localizationService = new();

    /// <summary>Инициализирует приложение: определяет язык, применяет культуру и отображает главное окно.</summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        LoggingService.Logger.Information(Lang.Log_AppStarting);

        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Exit += OnAppExit;

        LoggingService.Logger.Information(Lang.Log_DiscoveringLanguages);
        List<LanguageInfo> supportedLanguages = LocalizationService.DiscoverSupportedLanguages();

        if (supportedLanguages.Count is 0)
        {
            LoggingService.Logger.Warning(Lang.Log_NoLanguagesFound_AddingFallbacks);
            EnsureFallbackLanguages(supportedLanguages);
            if (supportedLanguages.Count is 0)
            {
                LoggingService.Logger.Fatal(Lang.Log_CriticalError, Lang.Error_Critical_NoLanguagesFound);
                ShowAndLogCriticalError(Lang.Error_Critical_NoLanguagesFound);
                Shutdown(1);
                return;
            }
        }
        LoggingService.Logger.Information(Lang.Log_FoundLanguagesResult, supportedLanguages.Count, string.Join(", ", supportedLanguages.Select(l => l.CultureCode)));

        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        MainWindow mainWindow = new();
        LoggingService.Logger.Information(Lang.Log_MainWindowInitializing);

        if (needsSelection)
        {
            LoggingService.Logger.Information(Lang.Log_LanguageSelectionNeeded);
            LanguageSelectionWindow selectionWindow = new(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                LoggingService.Logger.Information(Lang.Log_UserSelectedLanguage, selectedCultureName);
                AppSettings newSettings = settings ?? new AppSettings();
                newSettings.SelectedCulture = selectedCultureName;
                if (!SettingsService.SaveSettings(newSettings))
                {
                    LoggingService.Logger.Warning(Lang.Log_SaveLanguageSettingFailed);
                }
            }
            else
            {
                LoggingService.Logger.Warning(Lang.Log_UserClosedLanguageSelection, supportedLanguages.First().CultureCode);
                selectedCultureName = supportedLanguages.First().CultureCode;
            }
        }

        string cultureToApply = selectedCultureName ?? supportedLanguages.First().CultureCode;
        LoggingService.Logger.Information(Lang.Log_ApplyingCulture_Attempt, cultureToApply);
        bool cultureApplied = _localizationService.ApplyCulture(cultureToApply);

        if (!cultureApplied)
        {
            LoggingService.Logger.Warning(Lang.Log_ApplyingCulture_Fallback, cultureToApply);
            cultureApplied = _localizationService.ApplyCulture(supportedLanguages.First().CultureCode);
            if (cultureApplied)
            {
                LoggingService.Logger.Information(Lang.Log_ApplyingCulture_FallbackSuccess, supportedLanguages.First().CultureCode);
            }
            else
            {
                LoggingService.Logger.Fatal(Lang.Log_CriticalError, Lang.Error_Critical_CannotSetAnyLanguage);
                ShowAndLogCriticalError(Lang.Error_Critical_CannotSetAnyLanguage);
                Shutdown(1);
                return;
            }
        }
        LoggingService.Logger.Information(Lang.Log_ApplyingCulture_Success, cultureToApply);

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

    /// <summary>Отображает критическое сообщение об ошибке и логирует его как Fatal.</summary>
    /// <param name="message">Текст сообщения.</param>
    private static void ShowAndLogCriticalError(string message)
    {
        LoggingService.Logger.Fatal(Lang.Log_CriticalError, message);
        try
        {
            if (Current?.Dispatcher != null)
            {
                _ = Current.Dispatcher.Invoke(() =>
                    _ = MessageBox.Show(message, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error));
            }
            else
            {
                LoggingService.Logger.Error(Lang.Log_AppCurrentNull);
                Environment.FailFast(message);
            }
        }
        catch (Exception exInner)
        {
            LoggingService.Logger.Fatal(exInner, Lang.Log_CriticalError + " (MessageBox.Show failed)");
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
            LoggingService.Logger.Error(Lang.Log_AddedHardcodedFallback);
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