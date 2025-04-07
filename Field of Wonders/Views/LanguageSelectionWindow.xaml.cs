namespace Field_of_Wonders.Views;

/// <summary>Окно для выбора языка приложения.</summary>
public partial class LanguageSelectionWindow : Window
{
    /// <summary>Получает информацию о языке, выбранном пользователем.</summary>
    public LanguageInfo? SelectedLanguage { get; private set; }

    /// <summary>Инициализирует новый экземпляр окна <see cref="LanguageSelectionWindow"/>.</summary>
    /// <param name="supportedLanguages">Коллекция поддерживаемых языков для отображения.</param>
    public LanguageSelectionWindow(IEnumerable<LanguageInfo> supportedLanguages)
    {
        InitializeComponent();

        LanguageComboBox.ItemsSource = supportedLanguages;
        LanguageComboBox.SelectedIndex = 0; // По умолчанию выбираем первый язык в списке
    }

    /// <summary>Обрабатывает нажатие кнопки "OK", сохраняя выбранный язык и закрывая окно.</summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события.</param>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = LanguageComboBox.SelectedItem as LanguageInfo;
        DialogResult = true; // Сигнализирует об успешном выборе
    }
}