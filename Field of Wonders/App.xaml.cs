namespace Field_of_Wonders;

/// <summary>Управляет жизненным циклом приложения, включая инициализацию, выбор языка и настройки.</summary>
public partial class App : Application
{
    /// <summary>Имя файла для сохранения настроек приложения.</summary>
    private const string SettingsFileName = "Settings.msgpack";
    /// <summary>Полный путь к файлу настроек приложения.</summary>
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    /// <summary>Получает информацию о культуре, успешно примененной к текущему потоку UI.</summary>
    public static CultureInfo? CurrentAppliedCulture { get; private set; }

    /// <summary>Выполняется при запуске приложения. Инициализирует выбор языка, применяет культуру и отображает главное окно.</summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        List<LanguageInfo> supportedLanguages = DiscoverSupportedLanguages();

        if (supportedLanguages.Count is 0)
        {
            _ = MessageBox.Show("Не удалось обнаружить поддерживаемые языки. Приложение будет закрыто.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        string? selectedCultureName = LoadCultureSetting();

        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        if (needsSelection)
        {
            LanguageSelectionWindow selectionWindow = new(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                SaveCultureSetting(selectedCultureName);
            }
            else
            {
                selectedCultureName = supportedLanguages.First().CultureCode;
                // Настройки не сохраняются, окно появится снова при следующем запуске.
            }
        }

        ApplyCulture(selectedCultureName ?? supportedLanguages.First().CultureCode);

        base.OnStartup(e);

        MainWindow mainWindow = new();
        mainWindow.Show();
    }

    /// <summary>Динамически обнаруживает поддерживаемые языки на основе ресурсных сборок.</summary>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков, отсортированный по имени. Возвращает список с резервным языком, если обнаружение не удалось.</returns>
    private static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        List<LanguageInfo> languages = [];
        string neutralCultureCode = string.Empty;
        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
            NeutralResourcesLanguageAttribute? neutralResourcesAttr = entryAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();

            if (neutralResourcesAttr != null)
            {
                try
                {
                    neutralCultureCode = neutralResourcesAttr.CultureName;
                    CultureInfo neutralCulture = CultureInfo.GetCultureInfo(neutralCultureCode);
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                }
                catch (CultureNotFoundException)
                {
                    Console.WriteLine($"Warning: Neutral culture '{neutralResourcesAttr.CultureName}' specified in assembly attribute not found.");
                    neutralCultureCode = string.Empty;
                }
            }
            else
            {
                Console.WriteLine("Warning: NeutralResourcesLanguage attribute not found in assembly. Consider adding it in project properties (Package -> Assembly neutral language).");
                if (!languages.Any(l => l.CultureCode.Equals("ru-RU", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        CultureInfo ruCulture = CultureInfo.GetCultureInfo("ru-RU");
                        languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(ruCulture.NativeName), ruCulture.Name));
                        neutralCultureCode = "ru-RU";
                    }
                    catch (CultureNotFoundException) { /* Не добавляем */ }
                }
            }

            string baseDirectory = AppContext.BaseDirectory;
            string resourceAssemblyName = entryAssembly.GetName().Name + ".resources.dll";

            if (Directory.Exists(baseDirectory))
            {
                foreach (string dir in Directory.GetDirectories(baseDirectory))
                {
                    string potentialCultureCode = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(potentialCultureCode) ||
                        potentialCultureCode.Equals(neutralCultureCode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    CultureInfo? culture = null;
                    try
                    {
                        culture = CultureInfo.GetCultureInfo(potentialCultureCode);
                    }
                    catch (CultureNotFoundException)
                    {
                        continue; // Имя папки не является кодом культуры
                    }

                    string resourceDllPath = Path.Combine(dir, resourceAssemblyName);
                    if (File.Exists(resourceDllPath) && !languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName);
                        languages.Add(new LanguageInfo(displayName, culture.Name));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show($"Произошла ошибка при автоматическом определении языков:\n{ex.Message}\n\nБудет использован язык по умолчанию.",
                            "Ошибка определения языков", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Гарантируем наличие хотя бы одного языка
        if (languages.Count is 0)
        {
            try
            {
                // Пытаемся добавить русский как резервный
                CultureInfo fallbackCulture = CultureInfo.GetCultureInfo("ru-RU");
                languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(fallbackCulture.NativeName), fallbackCulture.Name));
            }
            catch (CultureNotFoundException)
            {
                try
                {
                    // Если русский не найден, пытаемся добавить язык системы
                    CultureInfo sysCulture = CultureInfo.InstalledUICulture;
                    languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(sysCulture.NativeName), sysCulture.Name));
                }
                catch (Exception)
                {
                    // Самый крайний случай - добавляем русский жестко, игнорируя NativeName
                    languages.Add(new LanguageInfo("Русский", "ru-RU"));
                }
            }
        }

        return [.. languages.OrderBy(l => l.DisplayName)];
    }

    /// <summary>Загружает код сохраненной культуры из файла настроек MessagePack.</summary>
    /// <returns>Код культуры или null, если файл не найден, поврежден или произошла ошибка.</returns>
    private static string? LoadCultureSetting()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return null;
        }

        try
        {
            byte[] fileBytes = File.ReadAllBytes(SettingsFilePath);
            if (fileBytes.Length == 0)
            {
                TryDeleteSettingsFile();
                return null;
            }

            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            AppSettings settings = MessagePackSerializer.Deserialize<AppSettings>(fileBytes, options);

            if (string.IsNullOrWhiteSpace(settings?.SelectedCulture))
            {
                TryDeleteSettingsFile();
                return null;
            }

            return settings.SelectedCulture;
        }
        catch (MessagePackSerializationException msgPackEx)
        {
            _ = MessageBox.Show($"Ошибка при чтении файла настроек '{SettingsFileName}':\n{msgPackEx.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Warning);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx)
        {
            _ = MessageBox.Show($"Ошибка чтения файла настроек '{SettingsFileName}':\n{ioEx.Message}\n\nПроверьте права доступа к папке приложения.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show($"Непредвиденная ошибка при загрузке настроек из '{SettingsFileName}':\n{ex.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            TryDeleteSettingsFile();
            return null;
        }
    }

    /// <summary>Сохраняет выбранный код культуры в файл настроек MessagePack.</summary>
    /// <param name="cultureCode">Код культуры для сохранения.</param>
    private static void SaveCultureSetting(string cultureCode)
    {
        try
        {
            AppSettings settings = new() { SelectedCulture = cultureCode };
            byte[] fileBytes = MessagePackSerializer.Serialize(settings);

            string? directory = Path.GetDirectoryName(SettingsFilePath);
            _ = directory != null
                ? Directory.CreateDirectory(directory)
                : throw new DirectoryNotFoundException($"Не удалось определить директорию для файла настроек: {SettingsFilePath}");

            File.WriteAllBytes(SettingsFilePath, fileBytes);
        }
        catch (UnauthorizedAccessException)
        {
            _ = MessageBox.Show($"Не удалось сохранить настройки языка.\nОтказано в доступе к файлу:\n{SettingsFilePath}\n\nПроверьте права на запись.",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException ioEx)
        {
            _ = MessageBox.Show($"Произошла ошибка при записи файла настроек '{SettingsFileName}':\n{ioEx.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show($"Произошла непредвиденная ошибка при сохранении настроек:\n{ex.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Безопасно пытается удалить файл настроек. Ошибки удаления игнорируются.</summary>
    private static void TryDeleteSettingsFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
        }
        catch (Exception ex)
        {
            // Ошибка удаления файла не критична для работы приложения при следующем запуске.
            // Записываем в консоль для отладки.
            Console.WriteLine($"Warning: Failed to delete settings file '{SettingsFilePath}': {ex.Message}");
        }
    }

    /// <summary>Применяет указанную культуру к текущему потоку UI.</summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU").</param>
    public static void ApplyCulture(string cultureCode)
    {
        try
        {
            CultureInfo cultureToApply = new(cultureCode);
            Thread.CurrentThread.CurrentUICulture = cultureToApply;
            CurrentAppliedCulture = cultureToApply;
        }
        catch (CultureNotFoundException ex)
        {
            _ = MessageBox.Show($"Выбранный язык '{cultureCode}' не найден ({ex.Message}).\nБудет использован язык по умолчанию.",
                            "Ошибка установки языка", MessageBoxButton.OK, MessageBoxImage.Warning);

            List<LanguageInfo> safeLanguages = DiscoverSupportedLanguages();
            string defaultCultureCode = safeLanguages.First().CultureCode;

            if (!cultureCode.Equals(defaultCultureCode, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CultureInfo defaultCultureInfo = new(defaultCultureCode);
                    Thread.CurrentThread.CurrentUICulture = defaultCultureInfo;
                    CurrentAppliedCulture = defaultCultureInfo;
                }
                catch (Exception innerEx)
                {
                    _ = MessageBox.Show($"Критическая ошибка: Не удалось установить язык по умолчанию '{defaultCultureCode}'.\n{innerEx.Message}",
                                    "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                _ = MessageBox.Show($"Критическая ошибка: Язык по умолчанию '{defaultCultureCode}' не найден.",
                                "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _ = MessageBox.Show($"Произошла непредвиденная ошибка при установке языка:\n{ex.Message}",
                            "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
            // Попытка установить жестко заданную резервную культуру
            try
            {
                CultureInfo fallbackCulture = new("ru-RU");
                Thread.CurrentThread.CurrentUICulture = fallbackCulture;
                CurrentAppliedCulture = fallbackCulture;
            }
            catch { /* Если даже резервный язык не найден, приложение скорее всего не сможет работать корректно */ }
        }
    }
}