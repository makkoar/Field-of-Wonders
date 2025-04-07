// --- App.xaml.cs ---
using Field_of_Wonders.Services; // Добавляем using для сервисов

namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, координируя инициализацию сервисов и UI.</summary>
public partial class App : Application
{
    // Экземпляры сервисов (для простоты создаем здесь, в идеале - использовать DI)
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localizationService = new();

    /// <summary>Выполняется при запуске приложения. Инициализирует выбор языка, применяет культуру и отображает главное окно.</summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e); // Вызываем базовый метод в начале

        // 1. Получаем список языков
        List<LanguageInfo> supportedLanguages = LocalizationService.DiscoverSupportedLanguages();
        if (supportedLanguages.Count is 0)
        {
            ShowCriticalError(Lang.Error_Critical_NoLanguagesFound);
            Shutdown(1); // Завершаем приложение с кодом ошибки
            return;
        }

        // 2. Загружаем настройки
        AppSettings? settings = SettingsService.LoadSettings();
        string? selectedCultureName = settings?.SelectedCulture;

        // 3. Определяем, нужен ли выбор языка
        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        if (needsSelection)
        {
            LanguageSelectionWindow selectionWindow = new(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                // Сохраняем только если пользователь выбрал язык и нажал ОК
                AppSettings newSettings = settings ?? new AppSettings(); // Используем старые настройки или создаем новые
                newSettings.SelectedCulture = selectedCultureName;
                _ = SettingsService.SaveSettings(newSettings); // Сохраняем через сервис
            }
            else
            {
                // Если пользователь закрыл окно или не выбрал язык, используем первый в списке
                selectedCultureName = supportedLanguages.First().CultureCode;
                // Настройки не сохраняются, окно появится снова при следующем запуске.
            }
        }

        // 4. Применяем культуру
        string cultureToApply = selectedCultureName ?? supportedLanguages.First().CultureCode;
        bool cultureApplied = _localizationService.ApplyCulture(cultureToApply);

        if (!cultureApplied)
        {
            // Если не удалось применить выбранную/сохраненную культуру, пытаемся применить первую из списка
            cultureApplied = _localizationService.ApplyCulture(supportedLanguages.First().CultureCode);
            if (!cultureApplied)
            {
                // Если и это не удалось, показываем критическую ошибку и выходим
                ShowCriticalError(Lang.Error_Critical_CannotSetAnyLanguage);
                Shutdown(1);
                return;
            }
        }

        // 5. Отображаем главное окно
        MainWindow mainWindow = new();
        mainWindow.Show();
    }

    /// <summary>Отображает критическое сообщение об ошибке.</summary>
    /// <param name="message">Текст сообщения.</param>
    private static void ShowCriticalError(string message) => _ = MessageBox.Show(message, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error);
}