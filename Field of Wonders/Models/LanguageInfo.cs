namespace Field_of_Wonders.Models;

/// <summary>Представляет информацию о поддерживаемом языке в приложении.</summary>
/// <param name="DisplayName">Имя языка, отображаемое пользователю (например, "Русский", "English").</param>
/// <param name="CultureCode">Код культуры IETF (например, "ru-RU", "en-US"), используемый для установки локализации.</param>
public record LanguageInfo(string DisplayName, string CultureCode);