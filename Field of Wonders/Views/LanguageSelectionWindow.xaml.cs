namespace Field_of_Wonders.Views;

/// <summary>Окно для выбора языка приложения. Использует статичные строки, не зависящие от текущей локализации.</summary>
public partial class LanguageSelectionWindow : Window
{
    /// <summary>Получает информацию о языке, выбранном пользователем.</summary>
    public LanguageInfo? SelectedLanguage { get; private set; }

    /// <summary>Инициализирует новый экземпляр окна <see cref="LanguageSelectionWindow"/>.</summary>
    /// <param name="supportedLanguages">Коллекция поддерживаемых языков для отображения в выпадающем списке.</param>
    public LanguageSelectionWindow(IEnumerable<LanguageInfo> supportedLanguages)
    {
        InitializeComponent();

        LanguageComboBox.ItemsSource = supportedLanguages;
        LanguageComboBox.SelectedIndex = 0;
    }

    /// <summary>Обрабатывает нажатие кнопки "OK". Сохраняет выбранный язык в свойство <see cref="SelectedLanguage"/> и закрывает окно с результатом <c>true</c>.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = LanguageComboBox.SelectedItem as LanguageInfo;
        DialogResult = true;
    }
}