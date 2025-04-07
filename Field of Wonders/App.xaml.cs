namespace Field_of_Wonders;

/// <summary>
/// Предоставляет логику взаимодействия для App.xaml, управляя жизненным циклом приложения,
/// включая инициализацию, выбор языка и загрузку/сохранение настроек.
/// </summary>
public partial class App : Application
{
    // --- Настройки ---
    /// <summary>Имя файла для сохранения настроек приложения.</summary>
    private const string SettingsFileName = "settings.msgpack";
    /// <summary>Полный путь к файлу настроек приложения.</summary>
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    /// <summary>Получает информацию о культуре, которая была успешно применена к текущему потоку пользовательского интерфейса.</summary>
    public static CultureInfo? CurrentAppliedCulture { get; private set; }

    /// <summary>
    /// Выполняется при запуске приложения. Инициализирует выбор языка, применяет культуру и отображает главное окно.
    /// </summary>
    /// <param name="e">Аргументы события запуска.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Получаем список поддерживаемых языков динамически
        List<LanguageInfo> supportedLanguages = DiscoverSupportedLanguages();

        if (!supportedLanguages.Any())
        {
            MessageBox.Show("Не удалось обнаружить поддерживаемые языки. Приложение будет закрыто.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(); // Завершаем работу, если языков нет
            return;
        }

        string? selectedCultureName = LoadCultureSetting();

        // Проверяем, есть ли сохраненный язык в списке обнаруженных
        bool needsSelection = string.IsNullOrEmpty(selectedCultureName) ||
                              !supportedLanguages.Any(l => l.CultureCode.Equals(selectedCultureName, StringComparison.OrdinalIgnoreCase));

        if (needsSelection)
        {
            // Используем обнаруженный список языков для окна выбора
            var selectionWindow = new LanguageSelectionWindow(supportedLanguages);
            bool? dialogResult = selectionWindow.ShowDialog();

            if (dialogResult == true && selectionWindow.SelectedLanguage != null)
            {
                selectedCultureName = selectionWindow.SelectedLanguage.CultureCode;
                SaveCultureSetting(selectedCultureName);
            }
            else
            {
                // Если пользователь закрыл окно или произошла ошибка, выбираем первый язык из обнаруженного списка
                selectedCultureName = supportedLanguages.First().CultureCode;
                // Настройки не сохраняются, окно появится снова при следующем запуске.
            }
        }

        // Применяем выбранную или дефолтную культуру
        ApplyCulture(selectedCultureName ?? supportedLanguages.First().CultureCode);

        base.OnStartup(e);

        // Создаем и показываем главное окно после установки культуры
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    /// <summary>
    /// Динамически обнаруживает поддерживаемые языки на основе доступных ресурсных сборок.
    /// Включает нейтральный язык (указанный в атрибуте сборки или предполагаемый) и языки из подпапок.
    /// </summary>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков, отсортированный по отображаемому имени. Возвращает список с хотя бы одним языком (резервным), даже если обнаружение не удалось.</returns>
    private static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        var languages = new List<LanguageInfo>();
        var neutralCultureCode = string.Empty;
        var currentUiCulture = CultureInfo.CurrentUICulture; // Кэшируем для производительности

        try
        {
            // 1. Определяем нейтральный язык (из атрибута сборки)
            Assembly entryAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
            var neutralResourcesAttr = entryAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();

            if (neutralResourcesAttr != null)
            {
                try
                {
                    neutralCultureCode = neutralResourcesAttr.CultureName;
                    var neutralCulture = CultureInfo.GetCultureInfo(neutralCultureCode);
                    // Делаем первую букву заглавной для единообразия, используя кэшированную культуру UI
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                }
                catch (CultureNotFoundException)
                {
                    Console.WriteLine($"Warning: Neutral culture '{neutralResourcesAttr.CultureName}' specified in assembly attribute not found.");
                    neutralCultureCode = string.Empty; // Сбрасываем
                }
            }
            else
            {
                Console.WriteLine("Warning: NeutralResourcesLanguage attribute not found in assembly. Consider adding it in project properties (Package -> Assembly neutral language).");
                // Попытка добавить русский как базовый, если он не был добавлен выше
                if (!languages.Any(l => l.CultureCode.Equals("ru-RU", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        var ruCulture = CultureInfo.GetCultureInfo("ru-RU");
                        languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(ruCulture.NativeName), ruCulture.Name));
                        neutralCultureCode = "ru-RU"; // Условно считаем его нейтральным
                    }
                    catch (CultureNotFoundException) {/*Не добавляем*/}
                }
            }

            // 2. Ищем сателлитные сборки в подпапках
            string baseDirectory = AppContext.BaseDirectory;
            string resourceAssemblyName = entryAssembly.GetName().Name + ".resources.dll";

            if (Directory.Exists(baseDirectory)) // Проверяем существование базовой директории
            {
                foreach (string dir in Directory.GetDirectories(baseDirectory))
                {
                    string potentialCultureCode = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(potentialCultureCode)) continue;

                    // Игнорируем нейтральный язык, если он уже добавлен
                    if (potentialCultureCode.Equals(neutralCultureCode, StringComparison.OrdinalIgnoreCase)) continue;

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
                    if (File.Exists(resourceDllPath))
                    {
                        // Убедимся, что не добавляем дубликат
                        if (!languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName);
                            languages.Add(new LanguageInfo(displayName, culture.Name));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Произошла ошибка при автоматическом определении языков:\n{ex.Message}\n\nБудет использован язык по умолчанию.",
                            "Ошибка определения языков", MessageBoxButton.OK, MessageBoxImage.Warning);
            // Обеспечиваем возврат хотя бы одного языка
            if (!languages.Any())
            {
                try
                {
                    var fallbackCulture = CultureInfo.GetCultureInfo("ru-RU"); // Резервный язык
                    languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(fallbackCulture.NativeName), fallbackCulture.Name));
                }
                catch (CultureNotFoundException) { /* Совсем крайний случай */ }
            }
        }

        // Гарантируем, что хотя бы один язык есть
        if (!languages.Any())
        {
            try
            {
                var sysCulture = CultureInfo.InstalledUICulture;
                languages.Add(new LanguageInfo(currentUiCulture.TextInfo.ToTitleCase(sysCulture.NativeName), sysCulture.Name));
            }
            catch (Exception)
            {
                // Самый крайний случай - добавляем русский жестко
                languages.Add(new LanguageInfo("Русский", "ru-RU"));
            }
        }

        // Сортируем по отображаемому имени для удобства пользователя
        return languages.OrderBy(l => l.DisplayName).ToList();
    }


    /// <summary>Загружает код сохраненной культуры из файла настроек MessagePack.</summary>
    /// <returns>Код культуры (например, "ru-RU") или null, если файл не найден, поврежден, пуст или произошла ошибка чтения.</returns>
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
                TryDeleteSettingsFile(); // Удаляем пустой/поврежденный файл
                return null;
            }

            // Используем опции безопасности для десериализации данных из ненадежного источника (файл)
            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            var settings = MessagePackSerializer.Deserialize<AppSettings>(fileBytes, options);

            // Проверяем, что данные корректны
            if (string.IsNullOrWhiteSpace(settings?.SelectedCulture))
            {
                TryDeleteSettingsFile(); // Некорректные данные
                return null;
            }

            return settings.SelectedCulture;
        }
        catch (MessagePackSerializationException msgPackEx)
        {
            MessageBox.Show($"Ошибка при чтении файла настроек '{SettingsFileName}':\n{msgPackEx.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Warning);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx) // Ошибка доступа к файлу
        {
            MessageBox.Show($"Ошибка чтения файла настроек '{SettingsFileName}':\n{ioEx.Message}\n\nПроверьте права доступа к папке приложения.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            return null; // Не удаляем файл, проблема может быть временной (например, антивирус)
        }
        catch (Exception ex) // Другие непредвиденные ошибки
        {
            MessageBox.Show($"Непредвиденная ошибка при загрузке настроек из '{SettingsFileName}':\n{ex.Message}\n\nФайл будет удален. При следующем запуске потребуется выбрать язык.",
                            "Ошибка загрузки настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            TryDeleteSettingsFile();
            return null;
        }
    }

    /// <summary>Сохраняет выбранный код культуры в файл настроек MessagePack.</summary>
    /// <param name="cultureCode">Код культуры для сохранения (например, "en-US").</param>
    private void SaveCultureSetting(string cultureCode)
    {
        try
        {
            var settings = new AppSettings { SelectedCulture = cultureCode };
            byte[] fileBytes = MessagePackSerializer.Serialize(settings);

            // Убедимся, что директория существует перед записью
            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            else // Крайне маловероятный сценарий, но обработаем
            {
                throw new DirectoryNotFoundException($"Не удалось определить директорию для файла настроек: {SettingsFilePath}");
            }


            File.WriteAllBytes(SettingsFilePath, fileBytes);
        }
        catch (UnauthorizedAccessException) // Нет прав на запись
        {
            MessageBox.Show($"Не удалось сохранить настройки языка.\nОтказано в доступе к файлу:\n{SettingsFilePath}\n\nПроверьте права на запись в папку приложения или запустите от имени администратора.",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException ioEx) // Ошибка ввода-вывода при записи
        {
            MessageBox.Show($"Произошла ошибка при записи файла настроек '{SettingsFileName}':\n{ioEx.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex) // Другие непредвиденные ошибки
        {
            MessageBox.Show($"Произошла непредвиденная ошибка при сохранении настроек:\n{ex.Message}",
                            "Ошибка сохранения настроек", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Безопасно пытается удалить файл настроек. Ошибки удаления игнорируются и не показываются пользователю.
    /// </summary>
    private void TryDeleteSettingsFile()
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
            // Ошибку удаления файла не показываем пользователю, т.к. он мало что может сделать.
            // Записываем в консоль/лог для отладки.
            Console.WriteLine($"Warning: Failed to delete settings file '{SettingsFilePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Применяет указанную культуру к текущему потоку пользовательского интерфейса (<see cref="Thread.CurrentUICulture"/>).
    /// Обрабатывает возможные ошибки и пытается применить язык по умолчанию в случае неудачи.
    /// </summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU", "en-US").</param>
    public static void ApplyCulture(string cultureCode)
    {
        try
        {
            var cultureToApply = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentUICulture = cultureToApply;
            // Раскомментируйте следующую строку, если вам нужно также изменить форматирование дат, чисел, валют
            // Thread.CurrentThread.CurrentCulture = cultureToApply;
            CurrentAppliedCulture = cultureToApply; // Сохраняем успешно примененную культуру
        }
        catch (CultureNotFoundException ex)
        {
            // Сообщаем об ошибке *перед* попыткой отката
            MessageBox.Show($"Выбранный язык '{cultureCode}' не найден в системе ({ex.Message}).\nБудет использован язык по умолчанию.",
                            "Ошибка установки языка", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Пытаемся откатиться на первый язык из списка поддерживаемых (который мы уже получили ранее)
            // Вместо рекурсии, лучше явно получить дефолтный язык.
            // Если DiscoverSupportedLanguages гарантирует хотя бы один язык, это безопасно.
            List<LanguageInfo> safeLanguages = DiscoverSupportedLanguages(); // Повторный вызов, чтобы получить гарантированный список
            string defaultCulture = safeLanguages.First().CultureCode;

            if (!cultureCode.Equals(defaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var defaultCultureInfo = new CultureInfo(defaultCulture);
                    Thread.CurrentThread.CurrentUICulture = defaultCultureInfo;
                    // Thread.CurrentThread.CurrentCulture = defaultCultureInfo;
                    CurrentAppliedCulture = defaultCultureInfo;
                }
                catch (Exception innerEx) // Ошибка даже при установке языка по умолчанию
                {
                    MessageBox.Show($"Критическая ошибка: Не удалось установить даже язык по умолчанию '{defaultCulture}'.\n{innerEx.Message}",
                                    "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Здесь можно рассмотреть Application.Current.Shutdown();
                }
            }
            else
            {
                // Если даже язык по умолчанию не найден (крайне маловероятно)
                MessageBox.Show($"Критическая ошибка: Язык по умолчанию '{defaultCulture}' не найден в системе.",
                                "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
                // Application.Current.Shutdown();
            }
        }
        catch (Exception ex) // Другие непредвиденные ошибки
        {
            MessageBox.Show($"Произошла непредвиденная ошибка при установке языка:\n{ex.Message}",
                            "Ошибка языка", MessageBoxButton.OK, MessageBoxImage.Error);
            // Попытка установить резервную культуру
            try
            {
                var fallbackCulture = new CultureInfo("ru-RU"); // Или "en-US"
                Thread.CurrentThread.CurrentUICulture = fallbackCulture;
                CurrentAppliedCulture = fallbackCulture;
            }
            catch { }
        }
    }
}
