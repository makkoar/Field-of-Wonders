namespace Field_of_Wonders.Views;

/// <summary>Окно для выбора языка приложения с использованием MVVM.</summary>
public partial class LanguageSelectionWindow : Window
{
    #region Свойства

    /// <summary>Получает информацию о языке, выбранном пользователем.</summary>
    /// <remarks>Значение берется из ViewModel после подтверждения.</remarks>
    public LanguageInfo? SelectedLanguage { get; private set; }

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр окна <see cref="LanguageSelectionWindow"/>.</summary>
    /// <param name="supportedLanguages">Коллекция поддерживаемых языков для отображения.</param>
    public LanguageSelectionWindow(IEnumerable<LanguageInfo> supportedLanguages)
    {
        InitializeComponent();
        DataContext = new LanguageSelectionViewModel(supportedLanguages);
    }

    #endregion

    #region Обработчики событий

    /// <summary>Обрабатывает нажатие кнопки "OK", считывая выбранный язык из ViewModel и закрывая окно.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is LanguageSelectionViewModel vm)
        {
            SelectedLanguage = vm.SelectedLanguage;
        }

        if (SelectedLanguage != null)
        {
            DialogResult = true;
        }
        else
        {
            // Это не должно произойти, так как ViewModel устанавливает выбор по умолчанию,
            // но на всякий случай добавим лог и предотвратим закрытие с DialogResult=true
            LoggingService.Logger.Warning("Окно выбора языка: кнопка ОК нажата, но SelectedLanguage is null.");
            DialogResult = false;
        }
    }

    #endregion
}