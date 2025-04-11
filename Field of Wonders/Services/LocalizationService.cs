namespace Field_of_Wonders.Services;

/// <summary>Сервис для управления локализацией приложения.</summary>
public class LocalizationService
{
    #region Свойства

    /// <summary>Получает информацию о культуре, успешно примененной к текущему потоку UI.</summary>
    public CultureInfo? CurrentAppliedCulture { get; private set; }

    #endregion

    #region События

    /// <summary>Статическое событие, вызываемое при изменении культуры приложения.</summary>
    public static event EventHandler? CultureChanged;

    #endregion

    #region Статические Методы

    /// <summary>Динамически обнаруживает поддерживаемые языки на основе доступных ресурсных наборов.</summary>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков, отсортированный по имени. Возвращает пустой список при критической ошибке.</returns>
    public static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        List<LanguageInfo> languages = [];
        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture;
        Assembly mainAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
        string baseName = typeof(Lang).FullName ?? $"{mainAssembly.GetName().Name}.Localization.Lang";
        ResourceManager resourceManager = new(baseName, mainAssembly);
        CultureInfo? neutralCulture = null;
        bool neutralCultureFoundAndAdded = false;

        try
        {
            // 1. Определяем и пытаемся добавить нейтральную культуру (основной Lang.resx)
            NeutralResourcesLanguageAttribute? neutralAttribute = mainAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            string neutralCultureName = neutralAttribute?.CultureName ?? "ru-RU";
            neutralCulture = GetCultureInfoSafe(neutralCultureName);

            if (neutralCulture != null)
            {
                ResourceSet? neutralSet = null;
                try
                {
                    neutralSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
                }
                catch (Exception ex)
                {
                    LoggingService.Logger.Error(ex, "Ошибка при доступе к нейтральному набору ресурсов для культуры '{NeutralCultureName}'.", neutralCulture.Name);
                }

                if (neutralSet != null)
                {
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    if (!languages.Any(l => l.CultureCode.Equals(neutralCulture.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                        LoggingService.Logger.Information(Lang.Log_NeutralCultureAdded_Format, neutralCulture.Name);
                        neutralCultureFoundAndAdded = true;
                    }
                }
                else
                {
                    LoggingService.Logger.Fatal("Критическая ошибка: Не удалось найти основной набор ресурсов для нейтральной культуры '{NeutralCultureName}'. Приложение не может функционировать.", neutralCulture.Name);
                    return [];
                }
            }
            else
            {
                LoggingService.Logger.Fatal("Критическая ошибка: Не удалось получить информацию о нейтральной культуре '{NeutralCultureName}'.", neutralCultureName);
                return [];
            }

            // 2. Ищем специфичные культуры (сборки-сателлиты)
            CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);

            foreach (CultureInfo culture in allCultures)
            {
                if (culture.Name.Equals(neutralCulture.Name, StringComparison.OrdinalIgnoreCase)) continue;

                ResourceSet? rs = null;
                try
                {
                    rs = resourceManager.GetResourceSet(culture, true, false);
                }
                catch (CultureNotFoundException) { continue; }
                catch (MissingManifestResourceException) { continue; }
                catch (Exception ex)
                {
                    LoggingService.Logger.Warning(ex, Lang.Log_GetResourceSetFailed_Format, culture.Name);
                    continue;
                }

                if (rs != null && !languages.Any(l => l.CultureCode.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(culture.NativeName);
                    languages.Add(new LanguageInfo(displayName, culture.Name));
                    LoggingService.Logger.Information(Lang.Log_SpecificCultureAdded_Format, culture.Name);
                }
            }

            resourceManager.ReleaseAllResources();
        }
        catch (Exception ex)
        {
            LoggingService.Logger.Error(ex, "Непредвиденная ошибка при обнаружении языков: {ErrorMessage}", ex.Message);
            return [];
        }

        if (languages.Count == 0 && neutralCultureFoundAndAdded)
        {
            LoggingService.Logger.Warning("После обнаружения нейтрального языка список поддерживаемых языков остался пустым.");
        }
        else if (languages.Count == 0)
        {
            LoggingService.Logger.Fatal("Критическая ошибка: Поддерживаемые языки не найдены.");
        }

        return [.. languages.OrderBy(l => l.DisplayName)];
    }

    #endregion

    #region Публичные Методы Экземпляра

    /// <summary>Применяет указанную культуру к текущему потоку UI и потоку по умолчанию.</summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU").</param>
    /// <returns><c>true</c> если культура успешно применена, иначе <c>false</c>.</returns>
    public bool ApplyCulture(string cultureCode)
    {
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
            LoggingService.Logger.Warning(ex, "Не удалось применить культуру '{CultureCode}' (не найдена): {ErrorMessage}", cultureCode, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
        catch (Exception ex)
        {
            LoggingService.Logger.Error(ex, "Непредвиденная ошибка при применении культуры '{CultureCode}': {ErrorMessage}", cultureCode, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
    }

    #endregion

    #region Приватные Статические Методы

    /// <summary>Безопасно пытается получить объект CultureInfo.</summary>
    /// <param name="cultureCode">Код культуры для поиска.</param>
    /// <returns>Объект CultureInfo или null, если культура не найдена.</returns>
    private static CultureInfo? GetCultureInfoSafe(string cultureCode)
    {
        try { return CultureInfo.GetCultureInfo(cultureCode); }
        catch (CultureNotFoundException)
        {
            LoggingService.Logger.Warning("Не удалось найти культуру '{CultureCode}'.", cultureCode);
            return null;
        }
    }

    #endregion
}