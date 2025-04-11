namespace Field_of_Wonders.Services;

/// <summary>Сервис для сохранения и загрузки настроек приложения.</summary>
public class SettingsService
{
    #region Константы

    /// <summary>Имя файла для сохранения настроек.</summary>
    private const string SettingsFileName = "Settings.msgpack";

    #endregion

    #region Поля (статические)

    /// <summary>Полный путь к файлу настроек.</summary>
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    #endregion

    #region Статические Методы

    /// <summary>Загружает настройки приложения из файла.</summary>
    /// <returns>Объект <see cref="AppSettings"/> или null при ошибке или отсутствии файла.</returns>
    public static AppSettings? LoadSettings()
    {
        if (!File.Exists(SettingsFilePath)) return null;

        try
        {
            byte[] fileBytes = File.ReadAllBytes(SettingsFilePath);
            if (fileBytes.Length == 0)
            {
                TryDeleteSettingsFile(); // Удаляем пустой файл
                return null;
            }

            // Настройки безопасности для десериализации из недоверенного источника (файл)
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
            AppSettings? settings = MessagePackSerializer.Deserialize<AppSettings>(fileBytes, options);

            if (settings == null) // Если десериализация вернула null
            {
                TryDeleteSettingsFile();
                return null;
            }
            LoggingService.Logger.Information(Lang.Log_SettingsLoaded, SettingsFileName);
            return settings;
        }
        catch (MessagePackSerializationException msgPackEx) // Файл поврежден или не соответствует модели
        {
            LoggingService.Logger.Warning(msgPackEx, Lang.Log_LoadSettings_ReadFailed_Format, SettingsFileName, msgPackEx.Message);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx) // Ошибка чтения файла
        {
            LoggingService.Logger.Error(ioEx, Lang.Log_LoadSettings_IOException_Format, SettingsFileName, ioEx.Message);
            return null;
        }
        catch (Exception ex) // Непредвиденная ошибка
        {
            LoggingService.Logger.Error(ex, Lang.Log_LoadSettings_Unexpected_Format, SettingsFileName, ex.Message);
            TryDeleteSettingsFile();
            return null;
        }
    }

    /// <summary>Сохраняет настройки приложения в файл.</summary>
    /// <param name="settings">Объект настроек для сохранения.</param>
    /// <returns><c>true</c> при успешном сохранении, иначе <c>false</c>.</returns>
    public static bool SaveSettings(AppSettings settings)
    {
        try
        {
            byte[] fileBytes = MessagePackSerializer.Serialize(settings);

            string? directory = Path.GetDirectoryName(SettingsFilePath);
            if (directory == null)
            {
                LoggingService.Logger.Error(Lang.Log_SaveSettings_DirectoryNotFound_Format, SettingsFilePath);
                return false;
            }
            _ = Directory.CreateDirectory(directory); // Гарантируем наличие директории

            File.WriteAllBytes(SettingsFilePath, fileBytes);
            LoggingService.Logger.Information(Lang.Log_SettingsSaved, SettingsFileName);
            return true;
        }
        catch (UnauthorizedAccessException) // Нет прав на запись
        {
            LoggingService.Logger.Error(Lang.Log_SaveSettings_Unauthorized_Format, SettingsFilePath);
            return false;
        }
        catch (IOException ioEx) // Ошибка записи файла
        {
            LoggingService.Logger.Error(ioEx, Lang.Log_SaveSettings_IOException_Format, SettingsFileName, ioEx.Message);
            return false;
        }
        catch (Exception ex) // Непредвиденная ошибка
        {
            LoggingService.Logger.Error(ex, Lang.Log_SaveSettings_Unexpected_Format, ex.Message);
            return false;
        }
    }

    /// <summary>Безопасно пытается удалить файл настроек. Ошибки логгируются как Warning.</summary>
    private static void TryDeleteSettingsFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
                LoggingService.Logger.Information(Lang.Log_SettingsFileDeleted, SettingsFileName);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Logger.Warning(ex, Lang.Log_SettingsFileDeleteFailed, SettingsFileName);
        }
    }

    #endregion
}