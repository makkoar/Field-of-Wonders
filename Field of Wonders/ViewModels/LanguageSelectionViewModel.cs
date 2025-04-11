namespace Field_of_Wonders.ViewModels;

/// <summary>ViewModel для окна выбора языка.</summary>
public partial class LanguageSelectionViewModel : ObservableObject
{
    #region Свойства (Observable)

    /// <summary>Получает коллекцию доступных языков.</summary>
    [ObservableProperty]
    private ObservableCollection<LanguageInfo> _availableLanguages;

    /// <summary>Получает или задает выбранный язык.</summary>
    [ObservableProperty]
    private LanguageInfo? _selectedLanguage;

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="LanguageSelectionViewModel"/>.</summary>
    /// <param name="supportedLanguages">Коллекция поддерживаемых языков.</param>
    public LanguageSelectionViewModel(IEnumerable<LanguageInfo> supportedLanguages)
    {
        _availableLanguages = [.. supportedLanguages];
        _selectedLanguage = _availableLanguages.FirstOrDefault();
    }

    #endregion
}