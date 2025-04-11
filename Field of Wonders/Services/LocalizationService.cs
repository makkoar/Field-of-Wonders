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

    /// <summary>Динамически обнаруживает поддерживаемые языки.</summary>
    /// <remarks>
    /// В режиме DEBUG используется ручной поиск сателлитных сборок (*.resources.dll) в подкаталогах.
    /// В режиме Release используется поиск по встроенным манифестным ресурсам (для single-file).
    /// </remarks>
    /// <returns>Список <see cref="LanguageInfo"/> поддерживаемых языков (нейтральных и специфичных), отсортированный по имени.</returns>
    public static List<LanguageInfo> DiscoverSupportedLanguages()
    {
        List<LanguageInfo> languages = [];
        CultureInfo currentUiCulture = CultureInfo.CurrentUICulture; // Для получения NativeName
        Assembly mainAssembly = Assembly.GetEntryAssembly() ?? typeof(App).Assembly;
        string baseName = typeof(Lang).FullName ?? $"{mainAssembly.GetName().Name}.Localization.Lang"; // Используется только в RELEASE
        CultureInfo? neutralCulture = null;
        string neutralCultureCode = string.Empty;
        bool neutralCultureFoundAndAdded = false;

        try
        {
            // 1. Определяем и добавляем нейтральную культуру (общий шаг)
            NeutralResourcesLanguageAttribute? neutralAttribute = mainAssembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            neutralCultureCode = neutralAttribute?.CultureName ?? "ru-RU"; // Fallback

            neutralCulture = GetCultureInfoSafe(neutralCultureCode);

            if (neutralCulture != null)
            {
                // Проверяем наличие нейтрального ресурса
                bool neutralResourceExists = false;
                try
                {
                    ResourceManager tempManager = new(typeof(Lang));
                    using ResourceSet? rs = tempManager.GetResourceSet(CultureInfo.InvariantCulture, true, false);
                    neutralResourceExists = rs != null;
                }
                catch (Exception ex)
                {
                    LoggingService.Logger.Warning(ex, "Ошибка при проверке наличия нейтрального ресурса для '{NeutralCultureCode}'.", neutralCultureCode);
                }

                if (neutralResourceExists)
                {
                    string displayName = currentUiCulture.TextInfo.ToTitleCase(neutralCulture.NativeName);
                    languages.Add(new LanguageInfo(displayName, neutralCulture.Name));
                    LoggingService.Logger.Information("Обнаружена и добавлена нейтральная культура '{NeutralCultureName}'.", neutralCulture.Name);
                    neutralCultureFoundAndAdded = true;
                }
                else
                {
                    LoggingService.Logger.Fatal("Критическая ошибка: Не найден нейтральный ресурс для культуры '{NeutralCultureCode}'.", neutralCultureCode);
                    return [];
                }
            }
            else
            {
                LoggingService.Logger.Fatal("Критическая ошибка: Не удалось получить информацию о нейтральной культуре '{NeutralCultureName}'.", neutralCultureCode);
                return [];
            }

#if DEBUG
            // ==================================
            // DEBUG: Ручной поиск по каталогам
            // ==================================
            LoggingService.Logger.Information("Обнаружение языков в режиме DEBUG (поиск сателлитных сборок в каталогах)...");

            string baseDirectory = AppContext.BaseDirectory;
            string resourceAssemblyName = $"{mainAssembly.GetName().Name}.resources.dll";

            if (Directory.Exists(baseDirectory))
            {
                string[] subDirectories = Directory.GetDirectories(baseDirectory);
                foreach (string dirPath in subDirectories)
                {
                    string potentialCultureCode = Path.GetFileName(dirPath);

                    if (string.IsNullOrEmpty(potentialCultureCode) || potentialCultureCode.Equals(neutralCultureCode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Пропуск нейтральной культуры или некорректных имен
                    }

                    CultureInfo? specificCulture = GetCultureInfoSafe(potentialCultureCode);
                    if (specificCulture == null)
                    {
                        continue; // Не валидный код культуры (лог был в GetCultureInfoSafe)
                    }

                    string resourceDllPath = Path.Combine(dirPath, resourceAssemblyName);
                    if (File.Exists(resourceDllPath))
                    {
                        if (!languages.Any(l => l.CultureCode.Equals(specificCulture.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            string displayName = currentUiCulture.TextInfo.ToTitleCase(specificCulture.NativeName);
                            languages.Add(new LanguageInfo(displayName, specificCulture.Name));
                            // Логгируем добавление найденного языка
                            LoggingService.Logger.Information("DEBUG: Обнаружена и добавлена культура '{CultureName}' из каталога.", specificCulture.Name);
                        }
                    }
                    // Нет нужды логировать отсутствие файла для каждого каталога
                }
            }
            else
            {
                LoggingService.Logger.Warning("DEBUG: Базовый каталог '{BaseDirectory}' не существует.", baseDirectory);
            }

#else
            // ==================================
            // RELEASE: Поиск по манифестным ресурсам
            // ==================================
            LoggingService.Logger.Information("Обнаружение языков в режиме RELEASE (поиск встроенных ресурсов)...");
            string resourceFileSuffix = ".resources";
            string neutralResourceName = baseName + resourceFileSuffix;

            var allResourceNames = mainAssembly.GetManifestResourceNames();
            LoggingService.Logger.Information("RELEASE: Найдено всего {Count} манифестн(ый/ых) ресурс(а/ов):", allResourceNames.Length);
            // Оставляем диагностический вывод всех ресурсов, т.к. он важен при проблемах с публикацией
            foreach(var name in allResourceNames)
            {
                LoggingService.Logger.Information("RELEASE: > {ResourceName}", name);
            }

            string baseNameWithDot = baseName + ".";
            int minLength = baseName.Length + 1 + 1 + resourceFileSuffix.Length; // Ожидаемая минимальная длина для Lang.<culture>.resources

            foreach (string resourceName in allResourceNames)
            {
                if (resourceName.Equals(neutralResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Пропускаем нейтральный
                }

                if (resourceName.Length >= minLength &&
                    resourceName.StartsWith(baseNameWithDot, StringComparison.OrdinalIgnoreCase) &&
                    resourceName.EndsWith(resourceFileSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string potentialCultureName = resourceName[baseNameWithDot.Length..^resourceFileSuffix.Length];
                    CultureInfo? specificCulture = GetCultureInfoSafe(potentialCultureName);

                    // Добавляем только НЕ-нейтральные культуры, найденные по этому шаблону
                    if (specificCulture != null && !specificCulture.IsNeutralCulture && !specificCulture.Equals(neutralCulture))
                    {
                        if (!languages.Any(l => l.CultureCode.Equals(specificCulture.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            string displayName = currentUiCulture.TextInfo.ToTitleCase(specificCulture.NativeName);
                            languages.Add(new LanguageInfo(displayName, specificCulture.Name));
                            LoggingService.Logger.Information("RELEASE: Обнаружена и добавлена специфичная культура '{CultureName}' через манифест.", specificCulture.Name);
                        }
                    }
                    // Логи для отладки можно оставить на уровне Verbose/Warning, если возникнут проблемы
                    else if (specificCulture != null) // Если культура нашлась, но нейтральная/совпадает
                    { LoggingService.Logger.Verbose("RELEASE: Извлеченная культура '{PotentialCultureName}' является нейтральной или совпадает с основной. Игнорируется.", potentialCultureName); }
                    else // Если GetCultureInfoSafe вернул null
                    { LoggingService.Logger.Warning("RELEASE: Не удалось распознать код культуры '{PotentialCultureName}' из имени ресурса '{ResourceName}'.", potentialCultureName, resourceName); }
                }
                else if (resourceName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    // Лог для ресурсов, которые начинаются с Lang, но не подходят под шаблон Lang.<culture>.resources
                    LoggingService.Logger.Verbose("RELEASE: Ресурс '{ResourceName}' не соответствует шаблону специфичной локализации.", resourceName);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            LoggingService.Logger.Error(ex, "Непредвиденная ошибка при обнаружении языков: {ErrorMessage}", ex.Message);
            return neutralCultureFoundAndAdded ? languages : [];
        }

        // Финальная проверка и сортировка
        if (languages.Count <= 1 && neutralCultureFoundAndAdded) // Используем <= 1 на случай если нейтральный не добавился по ошибке выше
        {
            LoggingService.Logger.Warning("Обнаружен только нейтральный язык. Специфичные ресурсы локализации не найдены или не обработаны корректно.");
        }
        else if (languages.Count == 0)
        {
            LoggingService.Logger.Fatal("Критическая ошибка: Языки не найдены.");
        }

        return [.. languages.OrderBy(l => l.DisplayName)];
    }

    #endregion

    #region Публичные Методы Экземпляра

    /// <summary>Применяет указанную культуру к текущему потоку UI и потоку по умолчанию.</summary>
    /// <param name="cultureCode">Код культуры для применения (например, "ru-RU", "de", "en-US").</param>
    /// <returns><c>true</c> если культура успешно применена, иначе <c>false</c>.</returns>
    public bool ApplyCulture(string cultureCode)
    {
        try
        {
            CultureInfo cultureToApply = new(cultureCode);
            Thread.CurrentThread.CurrentUICulture = cultureToApply;
            Thread.CurrentThread.CurrentCulture = cultureToApply; // Применяем и к форматированию чисел/дат
            CultureInfo.DefaultThreadCurrentCulture = cultureToApply;
            CultureInfo.DefaultThreadCurrentUICulture = cultureToApply;
            CurrentAppliedCulture = cultureToApply;
            LoggingService.Logger.Information(Lang.Log_CultureApplied, cultureCode); // Логгируем успех (локализованно)
            CultureChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (CultureNotFoundException ex)
        {
            // Логгируем ошибку (не локализованно)
            LoggingService.Logger.Warning(ex, "Не удалось применить культуру '{CultureCode}' (не найдена): {ErrorMessage}", cultureCode, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
        catch (Exception ex)
        {
            // Логгируем ошибку (не локализованно)
            LoggingService.Logger.Error(ex, "Непредвиденная ошибка при применении культуры '{CultureCode}': {ErrorMessage}", cultureCode, ex.Message);
            CurrentAppliedCulture = null;
            return false;
        }
    }

    #endregion

    #region Приватные Статические Методы

    /// <summary>Безопасно получает объект CultureInfo по коду культуры.</summary>
    /// <param name="cultureCode">Код культуры (например, "ru-RU", "de").</param>
    /// <returns>Объект CultureInfo или null, если культура не найдена или код некорректен.</returns>
    private static CultureInfo? GetCultureInfoSafe(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode)) return null;
        try
        {
            // Используем код как есть, без замены символов
            return CultureInfo.GetCultureInfo(cultureCode);
        }
        catch (CultureNotFoundException)
        {
            // Это ожидаемо при проверке имен папок/ресурсов, логгируем на низком уровне
            LoggingService.Logger.Verbose("Не удалось найти культуру '{CultureCode}'.", cultureCode);
            return null;
        }
        catch (ArgumentException argEx) // Некорректное имя
        {
            LoggingService.Logger.Warning(argEx, "Некорректное имя культуры при попытке получить CultureInfo: '{CultureCode}'", cultureCode);
            return null;
        }
    }

    #endregion
}