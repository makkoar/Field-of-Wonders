namespace Field_of_Wonders.Services;

/// <summary>Сервис для управления настройками приложения (сохранение и загрузка).</summary>
public class SettingsService
{
    /// <summary>Имя файла для сохранения настроек приложения.</summary>
    private const string SettingsFileName = "Settings.msgpack";
    /// <summary>Полный путь к файлу настроек приложения.</summary>
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    /// <summary>Загружает настройки приложения из файла MessagePack.</summary>
    /// <returns>Объект <see cref="AppSettings"/> или null, если файл не найден, поврежден или произошла ошибка.</returns>
    public static AppSettings? LoadSettings()
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

            // Дополнительная проверка на null после десериализации
            if (settings == null)
            {
                TryDeleteSettingsFile();
                return null;
            }

            return settings;
        }
        catch (MessagePackSerializationException msgPackEx)
        {
            ShowError(string.Format(Lang.Error_LoadSettings_ReadFailed_Format, SettingsFileName, msgPackEx.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Warning);
            TryDeleteSettingsFile();
            return null;
        }
        catch (IOException ioEx)
        {
            ShowError(string.Format(Lang.Error_LoadSettings_IOException_Format, SettingsFileName, ioEx.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Error);
            return null;
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Lang.Error_LoadSettings_Unexpected_Format, SettingsFileName, ex.Message), Lang.Error_LoadSettings_Title, MessageBoxImage.Error);
            TryDeleteSettingsFile();
            return null;
        }
    }

    /// <summary>Сохраняет настройки приложения в файл MessagePack.</summary>
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
            _ = Directory.CreateDirectory(directory);

            File.WriteAllBytes(SettingsFilePath, fileBytes);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(string.Format(Lang.Error_SaveSettings_Unauthorized_Format, SettingsFilePath), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
        }
        catch (IOException ioEx)
        {
            ShowError(string.Format(Lang.Error_SaveSettings_IOException_Format, SettingsFileName, ioEx.Message), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
        }
        catch (Exception ex)
        {
            ShowError(string.Format(Lang.Error_SaveSettings_Unexpected_Format, ex.Message), Lang.Error_SaveSettings_Title, MessageBoxImage.Error);
            return false;
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
            // Можно добавить логирование вместо вывода в консоль.
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to delete settings file '{SettingsFilePath}': {ex.Message}");
        }
    }

    /// <summary>Отображает сообщение об ошибке.</summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="caption">Заголовок окна сообщения.</param>
    /// <param name="icon">Иконка сообщения.</param>
    private static void ShowError(string message, string caption, MessageBoxImage icon) =>
        // Используем Application.Current.Dispatcher для потокобезопасного вызова MessageBox
        // из потенциально любого потока, где может быть вызван сервис.
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _ = MessageBox.Show(message, caption, MessageBoxButton.OK, icon);
        });
}