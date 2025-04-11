namespace Field_of_Wonders.MessagePackModels;

/// <summary>Модель данных для сохранения и загрузки настроек приложения.</summary>
[MessagePackObject]
public class AppSettings
{
    /// <summary>Получает или задает код выбранной пользователем культуры (например, "ru-RU", "en-US").</summary>
    [Key(0)]
    public string? SelectedCulture { get; set; }
}