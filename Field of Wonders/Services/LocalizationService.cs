namespace Field_of_Wonders.Services;

/// <summary>Сервис для управления локализацией приложения.</summary>
public class LocalizationService
{
    /// <summary>Получает информацию о культуре, успешно примененной к текущему потоку UI.</summary>
    public CultureInfo? CurrentAppliedCulture { get; private set; }

    /// <summary>Статическое событие, вызываемое при изменении культуры приложения.</summary>
    public static event EventHandler? CultureChanged;

    /// <summary>Динамически обнаруживает поддерживаемые языки на основе доступных ресурсных наборов.</summary>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков, отсортированный по имени. Может вернуть пустой список при ошибке.</returns>
    public static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        List<LanguageInfo> languages = [];
        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture; // Для форматирования NativeName
        Assembly mainAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
        string baseName = typeof(Lang).FullName ?? $"{mainAssembly.GetName().Name}.Localization.Lang"; // Базовое имя менеджера ресурсов
        ResourceManager resourceManager = new(baseName, mainAssembly);
        CultureInfo? neutralCulture = null; // Храним нейтральную культуру
        bool neutralCultureFoundAndAdded = false;

        try
        {
            // 1. Определяем и пытаемся добавить нейтральную культуру (язык по умолчанию в Lang.resx)
            NeutralResourcesLanguageAttribute? neutralAttribute = mainAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            string neutralCultureName = neutralAttribute?.CultureName ?? "ru-RU"; // Предполагаем ru-RU если атрибут не найден
            neutralCulture = GetCultureInfoSafe(neutralCultureName); // Использует хардкод для лога ошибки, если надо

            if (neutralCulture != null)
            {
                // Проверяем, действительно ли есть набор для нейтральной культуры
                ResourceSet? neutralSet = null;
                try
                {
                    neutralSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false); // Ищем инвариантный (нейтральный) набор
                }
                catch (Exception ex) // Ловим ошибки доступа к ресурсам
                {
                    LoggingService.Logger.Error(ex, "Ошибка при доступе к нейтральному набору ресурсов для культуры '{NeutralCultureName}'.", neutralCulture.Name); // Хардкод
                }


                if (neutralSet != null)
                {
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    if (!languages.Any(l => l.CultureCode.Equals(neutralCulture.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                        // Используем Lang, т.к. нейтральный язык ТОЧНО есть, если мы здесь
                        LoggingService.Logger.Information(Lang.Log_NeutralCultureAdded_Format, neutralCulture.Name);
                        neutralCultureFoundAndAdded = true;
                    }
                }
                else
                {
                    // Это критично - основной файл ресурсов не найден или недоступен
                    LoggingService.Logger.Error("Не удалось найти набор ресурсов для предполагаемой нейтральной культуры '{NeutralCultureName}'. Локализация может работать некорректно.", neutralCulture.Name); // Хардкод
                }
            }

            // 2. Ищем специфичные культуры, проверяя наличие ResourceSet
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);

            foreach (CultureInfo culture in allCultures)
            {
                if (neutralCulture != null && culture.Name.Equals(neutralCulture.Name, StringComparison.OrdinalIgnoreCase)) continue;

                ResourceSet? rs = null;
                try
                {
                    rs = resourceManager.GetResourceSet(culture, true, false);
                }
                catch (CultureNotFoundException) { continue; }
                catch (MissingManifestResourceException) { continue; }
                catch (Exception ex)
                {
                    // Используем Lang, если нейтральная культура найдена, иначе хардкод
                    string logMsg = neutralCultureFoundAndAdded ? Lang.Log_GetResourceSetFailed_Format : "Не удалось получить набор ресурсов для культуры '{0}'. Пропуск.";
                    LoggingService.Logger.Warning(ex, logMsg, culture.Name);
                    continue;
                }

                if (rs != null && !languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName);
                    languages.Add(new LanguageInfo(displayName, culture.Name));
                    // Используем Lang, т.к. хотя бы нейтральный язык найден (если neutralCultureFoundAndAdded == true)
                    // Если нейтральный не найден, Lang может быть недоступен, но лог сработает (хоть и покажет ключ)
                    LoggingService.Logger.Information(Lang.Log_SpecificCultureAdded_Format, culture.Name);
                }
            }

            resourceManager.ReleaseAllResources();
        }
        catch (Exception ex)
        {
            // Используем хардкод, так как причина ошибки может быть связана с ресурсами
            LoggingService.Logger.Error(ex, "Ошибка при обнаружении языков: {ErrorMessage}", ex.Message);
            return [];
        }

        // Если после всех попыток список пуст (даже нейтральный язык не найден/добавлен)
        if (languages.Count == 0)
        {
            // Используем хардкод, так как Lang гарантированно недоступен
            LoggingService.Logger.Fatal("Критическая ошибка: Поддерживаемые языки не найдены или не удалось загрузить ресурсы.");
        }

        return [.. languages.OrderBy(l => l.DisplayName)];
    }

    /// <summary>Применяет указанную культуру к текущему потоку UI и потоку по умолчанию.</summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU").</param>
    /// <returns><c>true</c> если культура успешно применена, иначе <c>false</c>.</returns>
    public bool ApplyCulture(string cultureCode)
    {
        // На момент вызова этого метода предполагается, что Lang уже должен быть доступен,
        // т.к. язык был выбран или загружен из настроек.
        try
        {
            CultureInfo cultureToApply = new(cultureCode);
            Thread.CurrentThread.CurrentUICulture = cultureToApply;
            Thread.CurrentThread.CurrentCulture = cultureToApply;
            CultureInfo.DefaultThreadCurrentCulture = cultureToApply;
            CultureInfo.DefaultThreadCurrentUICulture = cultureToApply;
            CurrentAppliedCulture = cultureToApply;
            LoggingService.Logger.Information(Lang.Log_CultureApplied, cultureCode);
            CultureChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }
        catch (CultureNotFoundException ex)
        {
            // Lang должен быть доступен.
            LoggingService.Logger.Warning(ex, Lang.Log_ApplyCulture_NotFound_Format, cultureCode, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
        catch (Exception ex)
        {
            LoggingService.Logger.Error(ex, Lang.Log_ApplyCulture_Unexpected_Format, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
    }

    /// <summary>Безопасно пытается получить объект CultureInfo.</summary>
    /// <param name="cultureCode">Код культуры для поиска.</param>
    /// <returns>Объект CultureInfo или null, если культура не найдена.</returns>
    private static CultureInfo? GetCultureInfoSafe(string cultureCode)
    {
        try { return CultureInfo.GetCultureInfo(cultureCode); }
        catch (CultureNotFoundException)
        {
            // Lang может быть недоступен на самом раннем этапе, используем хардкод
            LoggingService.Logger.Warning("Не удалось найти культуру '{CultureCode}'.", cultureCode);
            return null;
        }
    }
}
