namespace Field_of_Wonders.Services;

/// <summary>Сервис для сохранения и загрузки настроек приложения.</summary>
public class SettingsService
{
    /// <summary>Имя файла для сохранения настроек.</summary>
    private const string SettingsFileName = "Settings.msgpack";
    /// <summary>Полный путь к файлу настроек.</summary>
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

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
            return settings;
        }
        catch (MessagePackSerializationException msgPackEx) // Файл поврежден или не соответствует модели
        {
            ShowError(string.Format(Lang.Error_LoadSettings_ReadFailed_Format, SettingsFileName, msgPackEx.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Warning);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx) // Ошибка чтения файла
        {
            ShowError(string.Format(Lang.Error_LoadSettings_IOException_Format, SettingsFileName, ioEx.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Error);
            return null;
        }
        catch (Exception ex) // Непредвиденная ошибка
        {
            ShowError(string.Format(Lang.Error_LoadSettings_Unexpected_Format, SettingsFileName, ex.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Error);
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
                ShowError(string.Format(Lang.Error_SaveSettings_DirectoryNotFound_Format, SettingsFilePath), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
                return false;
            }
            _ = Directory.CreateDirectory(directory); // Гарантируем наличие директории

            File.WriteAllBytes(SettingsFilePath, fileBytes);
            return true;
        }
        catch (UnauthorizedAccessException) // Нет прав на запись
        {
            ShowError(string.Format(Lang.Error_SaveSettings_Unauthorized_Format, SettingsFilePath), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
        }
        catch (IOException ioEx) // Ошибка записи файла
        {
            ShowError(string.Format(Lang.Error_SaveSettings_IOException_Format, SettingsFileName, ioEx.Message), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
        }
        catch (Exception ex) // Непредвиденная ошибка
        {
            ShowError(string.Format(Lang.Error_SaveSettings_Unexpected_Format, ex.Message), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>Безопасно пытается удалить файл настроек. Ошибки игнорируются.</summary>
    private static void TryDeleteSettingsFile()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки удаления - не критично для следующего запуска.
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