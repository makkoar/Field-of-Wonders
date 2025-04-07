namespace Field_of_Wonders.MessagePackModels;

/// <summary>Модель данных для сохранения и загрузки настроек приложения с использованием MessagePack.</summary>
[MessagePackObject]
public class AppSettings
{
    /// <summary>Получает или задает код выбранной пользователем культуры (например, "ru-RU", "en-US").</summary>
    [Key(0)]
    public string? SelectedCulture { get; set; }

    // Примечание: В будущем сюда можно добавить другие настройки,
    // например, громкость звука, имя пользователя и т.д.,
    // присвоив им следующие индексы [Key(1)], [Key(2)]...
}