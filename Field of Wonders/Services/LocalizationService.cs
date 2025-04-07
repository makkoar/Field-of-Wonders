namespace Field_of_Wonders.Services;

/// <summary>Сервис для управления локализацией приложения.</summary>
public class LocalizationService
{
    /// <summary>Получает информацию о культуре, успешно примененной к текущему потоку UI.</summary>
    public CultureInfo? CurrentAppliedCulture { get; private set; }

    /// <summary>Динамически обнаруживает поддерживаемые языки на основе ресурсных сборок.</summary>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков, отсортированный по имени.</returns>
    public static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        List<LanguageInfo> languages = [];
        string neutralCultureCode = string.Empty;
        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture; // Для форматирования NativeName

        try
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
            NeutralResourcesLanguageAttribute? neutralResourcesAttr = entryAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();

            // Определяем нейтральный язык из атрибута сборки
            if (neutralResourcesAttr != null)
            {
                try
                {
                    neutralCultureCode = neutralResourcesAttr.CultureName;
                    CultureInfo neutralCulture = CultureInfo.GetCultureInfo(neutralCultureCode);
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                }
                catch (CultureNotFoundException) { neutralCultureCode = string.Empty; } // Сбрасываем, если не найден
            }
            else
            {
                // Пытаемся добавить русский как предполагаемый нейтральный/основной, если атрибута нет
                if (!languages.Any(l => l.CultureCode.Equals("ru-RU", StringComparison.OrdinalIgnoreCase)))
                {
                    TryAddCulture(languages, currentUiCulture, "ru-RU");
                    neutralCultureCode = "ru-RU"; // Предполагаем, что он нейтральный
                }
            }

            // Ищем сателлитные сборки в подпапках
            string baseDirectory = AppContext.BaseDirectory;
            string resourceAssemblyName = $"{entryAssembly.GetName().Name}.resources.dll";

            if (Directory.Exists(baseDirectory))
            {
                foreach (string dir in Directory.GetDirectories(baseDirectory))
                {
                    string potentialCultureCode = Path.GetFileName(dir);
                    if (string.IsNullOrEmpty(potentialCultureCode) || potentialCultureCode.Equals(neutralCultureCode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Пропускаем нейтральную культуру
                    }

                    CultureInfo? culture = GetCultureInfoSafe(potentialCultureCode);
                    if (culture == null) continue; // Не валидный код культуры

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
            ShowError(string.Format(Lang.Error_DiscoverLanguages_Failed_Format, ex.Message), Lang.Error_DiscoverLanguages_Title, MessageBoxImage.Warning);
        }

        // Гарантируем наличие хотя бы одного языка (резервные варианты)
        EnsureAtLeastOneLanguage(languages, currentUiCulture);

        return [.. languages.OrderBy(l => l.DisplayName)];
    }

    /// <summary>Применяет указанную культуру к текущему потоку UI и потоку по умолчанию.</summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU").</param>
    /// <returns><c>true</c> если культура успешно применена, иначе <c>false</c>.</returns>
    public bool ApplyCulture(string cultureCode)
    {
        try
        {
            CultureInfo cultureToApply = new(cultureCode);
            Thread.CurrentThread.CurrentUICulture = cultureToApply;
            Thread.CurrentThread.CurrentCulture = cultureToApply; // Применяем и к форматированию
            CultureInfo.DefaultThreadCurrentCulture = cultureToApply;
            CultureInfo.DefaultThreadCurrentUICulture = cultureToApply;
            CurrentAppliedCulture = cultureToApply;
            return true;
        }
        catch (CultureNotFoundException ex)
        {
            ShowError(string.Format(Lang.Error_ApplyCulture_NotFound_Format, cultureCode, ex.Message), Lang.Error_ApplyCulture_Title, MessageBoxImage.Warning);
            CurrentAppliedCulture = null;
            return false;
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Lang.Error_ApplyCulture_Unexpected_Format, ex.Message), Lang.Error_ApplyCulture_Title, MessageBoxImage.Error);
            CurrentAppliedCulture = null;
            return false;
        }
    }

    /// <summary>Безопасно пытается получить объект CultureInfo.</summary>
    /// <param name="cultureCode">Код культуры для поиска.</param>
    private static CultureInfo? GetCultureInfoSafe(string cultureCode)
    {
        try { return CultureInfo.GetCultureInfo(cultureCode); }
        catch (CultureNotFoundException) { return null; }
    }

    /// <summary>Пытается добавить информацию о культуре в список, если она существует и еще не добавлена.</summary>
    /// <param name="languages">Список языков для пополнения.</param>
    /// <param name="currentUiCulture">Текущая культура UI для форматирования имени.</param>
    /// <param name="cultureCodeToAdd">Код культуры для добавления.</param>
    private static void TryAddCulture(List<LanguageInfo> languages, CultureInfo currentUiCulture, string cultureCodeToAdd)
    {
        try
        {
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureCodeToAdd);
            if (!languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
            {
                string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName);
                languages.Add(new LanguageInfo(displayName, culture.Name));
            }
        }
        catch (CultureNotFoundException) { /* Игнорируем */ }
    }

    /// <summary>Убеждается, что в списке есть хотя бы один язык, добавляя резервные варианты.</summary>
    /// <param name="languages">Список языков для проверки.</param>
    /// <param name="currentUiCulture">Текущая культура UI для форматирования имени резервных языков.</param>
    private static void EnsureAtLeastOneLanguage(List<LanguageInfo> languages, CultureInfo currentUiCulture)
    {
        if (languages.Count is not 0) return;

        // Резервные варианты: ru -> en -> язык системы -> ru (жестко)
        TryAddCulture(languages, currentUiCulture, "ru-RU");
        if (languages.Count is not 0) return;

        TryAddCulture(languages, currentUiCulture, "en-US");
        if (languages.Count is not 0) return;

        try
        {
            CultureInfo sysCulture = CultureInfo.InstalledUICulture;
            if (!languages.Any(l => l.CultureCode.Equals(sysCulture.Name, StringComparison.OrdinalIgnoreCase)))
            {
                string displayName = currentUiCulture.TextInfo.ToTitleCase(sysCulture.NativeName);
                languages.Add(new LanguageInfo(displayName, sysCulture.Name));
            }
            if (languages.Count is not 0) return;
        }
        catch { /* Игнорируем ошибку получения системной культуры */ }

        if (languages.Count is 0)
        {
            languages.Add(new LanguageInfo("Русский", "ru-RU")); // Крайний случай
        }
    }

    /// <summary>Отображает сообщение об ошибке в потоке UI.</summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="caption">Заголовок окна сообщения.</param>
    /// <param name="icon">Иконка сообщения.</param>
    private static void ShowError(string message, string caption, MessageBoxImage icon) => Application.Current?.Dispatcher.Invoke(() =>
    {
        _ = MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
    });
}