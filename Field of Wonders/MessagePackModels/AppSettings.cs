namespace Field_of_Wonders.MessagePackModels;

/// <summary>Класс для сериализации/десериализации настроек приложения с использованием MessagePack.</summary>
[MessagePackObject]
public class AppSettings
{
    /// <summary>Получает или задает код выбранной пользователем культуры (например, "ru-RU").</summary>
    [Key(0)]
    public string? SelectedCulture { get; set; }
}