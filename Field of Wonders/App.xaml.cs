namespace Field_of_Wonders;

public partial class App : Application
{
    // --- Настройки ---
    private static readonly List<LanguageInfo> SupportedLanguages =
    [
        new("Русский", "ru-RU"),
        new("English", "en-US")
        // Добавь сюда другие языки при необходимости
    ];

    private const string SettingsFileName = "settings.msgpack";
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    [MessagePackObject]
    public class AppSettings
    {
        [Key(0)]
        public string? SelectedCulture { get; set; }
    }
    // --- ---

    /// <summary>Получает информацию о культуре, примененной к текущему потоку пользовательского интерфейса.</summary>
    public static CultureInfo? CurrentAppliedCulture { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        string? selectedCultureName = LoadCultureSetting();
        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) || !SupportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        if (needsSelection)
        {
            var selectionWindow = new LanguageSelectionWindow(SupportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                SaveCultureSetting(selectedCultureName);
            }
            else
            {
                selectedCultureName = SupportedLanguages.First().CultureCode;
                // Настройки не сохраняются, окно появится снова при следующем запуске.
            }
        }

        ApplyCulture(selectedCultureName ?? SupportedLanguages.First().CultureCode);

        base.OnStartup(e);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    /// <summary>Загружает код культуры из файла настроек MessagePack.</summary>
    /// <returns>Код культуры или null, если файл не найден, пуст или произошла ошибка.</returns>
    private string? LoadCultureSetting()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return null; // Файла нет - нормальная ситуация при первом запуске
        }

        try
        {
            byte[] fileBytes = File.ReadAllBytes(SettingsFilePath);
            if (fileBytes.Length == 0)
            {
                TryDeleteSettingsFile(); // Удаляем пустой файл
                return null;
            }

            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            var settings = MessagePackSerializer.Deserialize<AppSettings>(fileBytes, options);

            if (string.IsNullOrWhiteSpace(settings?.SelectedCulture))
            {
                TryDeleteSettingsFile(); // Считаем это некорректным состоянием
                return null;
            }

            return settings.SelectedCulture;
        }
        catch (MessagePackSerializationException msgPackEx)
        {
            // Сообщаем пользователю об ошибке чтения настроек
            MessageBox.Show($"Ошибка при чтении файла настроек '{SettingsFileName}':\n{msgPackEx.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Warning);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx)
        {
            // Сообщаем пользователю об ошибке чтения файла
            MessageBox.Show($"Ошибка чтения файла настроек '{SettingsFileName}':\n{ioEx.Message}\n\nПроверьте права доступа к папке приложения.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            return null; // Не удаляем файл, проблема может быть временной
        }
        catch (Exception ex)
        {
            // Сообщаем о непредвиденной ошибке
            MessageBox.Show($"Непредвиденная ошибка при загрузке настроек из '{SettingsFileName}':\n{ex.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            TryDeleteSettingsFile();
            return null;
        }
    }

    /// <summary>Сохраняет выбранный код культуры в файл настроек MessagePack.</summary>
    /// <param name="cultureCode">Код культуры для сохранения.</param>
    private void SaveCultureSetting(string cultureCode)
    {
        try
        {
            var settings = new AppSettings { SelectedCulture = cultureCode };
            byte[] fileBytes = MessagePackSerializer.Serialize(settings);

            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory); // Убедимся, что папка существует
            }

            File.WriteAllBytes(SettingsFilePath, fileBytes);
        }
        catch (UnauthorizedAccessException authEx)
        {
            // Это сообщение уже использует MessageBox
            MessageBox.Show($"Не удалось сохранить настройки языка.\nОтказано в доступе к файлу:\n{SettingsFilePath}\n\nВозможно, требуется запуск от имени администратора.",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException ioEx)
        {
            // Это сообщение уже использует MessageBox
            MessageBox.Show($"Произошла ошибка при записи файла настроек:\n{ioEx.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            // Это сообщение уже использует MessageBox
            MessageBox.Show($"Произошла непредвиденная ошибка при сохранении настроек:\n{ex.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Безопасно пытается удалить файл настроек.</summary>
    private void TryDeleteSettingsFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
                // Информацию об удалении файла пользователю показывать не нужно
            }
        }
        catch (Exception ex)
        {
            // Ошибку удаления файла тоже не показываем, т.к. пользователь мало что может сделать.
            // Можно добавить логирование в файл, если это критично.
            Console.WriteLine($"Failed to delete settings file '{SettingsFilePath}': {ex.Message}"); // Оставлю здесь на случай, если консоль все же используется для логов
        }
    }

    /// <summary>Применяет указанную культуру к текущему потоку UI.</summary>
    /// <param name="cultureCode">Код культуры для применения.</param>
    public static void ApplyCulture(string cultureCode)
    {
        try
        {
            CurrentAppliedCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentUICulture = CurrentAppliedCulture;
            // Thread.CurrentThread.CurrentCulture = CurrentAppliedCulture; // Если нужно для форматирования
        }
        catch (CultureNotFoundException ex)
        {
            // Сообщаем об ошибке поиска культуры *перед* попыткой отката
            MessageBox.Show($"Выбранный язык '{cultureCode}' не найден в системе ({ex.Message}).\nБудет использован язык по умолчанию.",
                            "Ошибка установки языка", MessageBoxButton.OK, MessageBoxImage.Warning);

            string defaultCulture = SupportedLanguages.First().CultureCode;
            if (!cultureCode.Equals(defaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                ApplyCulture(defaultCulture); // Повторный вызов с языком по умолчанию
            }
            else
            {
                // Если даже язык по умолчанию не найден (крайне маловероятно)
                MessageBox.Show($"Критическая ошибка: Язык по умолчанию '{defaultCulture}' не найден в системе.",
                                "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
                // Здесь можно рассмотреть завершение работы приложения
                // Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            // Сообщение об ошибке уже использует MessageBox
            MessageBox.Show($"Произошла непредвиденная ошибка при установке языка:\n{ex.Message}",
                            "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
