namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, инициализирует локализацию и главное окно.</summary>
public partial class App : Application
{
    // Экземпляры сервисов можно хранить здесь или использовать DI контейнер.
    // Статические методы сервисов используются напрямую в OnStartup для простоты.
    // private readonly SettingsService _settingsService = new(); // Пока не используется напрямую
    private readonly LocalizationService _localizationService = new();

    /// <summary>Инициализирует приложение: определяет язык, применяет культуру и отображает главное окно.</summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        List<LanguageInfo> supportedLanguages = LocalizationService.DiscoverSupportedLanguages();
        if (supportedLanguages.Count is 0)
        {
            ShowCriticalError(Lang.Error_Critical_NoLanguagesFound);
            Shutdown(1);
            return;
        }

        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        MainWindow mainWindow = new();

        if (needsSelection)
        {
            // Показываем окно выбора языка
            LanguageSelectionWindow selectionWindow = new(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                // Пользователь выбрал язык и нажал OK
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                AppSettings newSettings = settings ?? new AppSettings();
                newSettings.SelectedCulture = selectedCultureName;
                _ = SettingsService.SaveSettings(newSettings); // Сохраняем выбор
            }
            else
            {
                // Пользователь закрыл окно или не выбрал язык - используем первый доступный
                selectedCultureName = supportedLanguages.First().CultureCode;
                // Настройки не сохраняем, окно выбора появится снова при следующем запуске.
            }
        }

        // Применяем выбранную или первую доступную культуру
        string cultureToApply = selectedCultureName ?? supportedLanguages.First().CultureCode;
        bool cultureApplied = _localizationService.ApplyCulture(cultureToApply);

        if (!cultureApplied)
        {
            // Резервный вариант: пытаемся применить первую культуру из списка, если выбранная не сработала
            cultureApplied = _localizationService.ApplyCulture(supportedLanguages.First().CultureCode);
            if (!cultureApplied)
            {
                // Крайний случай: не удалось применить ни одну культуру
                ShowCriticalError(Lang.Error_Critical_CannotSetAnyLanguage);
                Shutdown(1);
                return;
            }
        }

        // Запускаем главное окно
        mainWindow.Show();
    }

    /// <summary>Отображает критическое сообщение об ошибке.</summary>
    /// <param name="message">Текст сообщения.</param>
    private static void ShowCriticalError(string message) =>
        _ = MessageBox.Show(message, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error);
}