namespace Field_of_Wonders.Views;

/// <summary>Окно для выбора языка при первом запуске.</summary>
public partial class LanguageSelectionWindow : Window
{
    /// <summary>Получает выбранный пользователем язык.</summary>
    public LanguageInfo? SelectedLanguage { get; private set; }

    /// <summary>Инициализирует новый экземпляр окна <see cref="LanguageSelectionWindow"/>.</summary>
    /// <param name="supportedLanguages">Список поддерживаемых языков для отображения в выпадающем списке.</param>
    public LanguageSelectionWindow(IEnumerable<LanguageInfo> supportedLanguages)
    {
        InitializeComponent();

        LanguageComboBox.ItemsSource = supportedLanguages;
        // LanguageComboBox.DisplayMemberPath больше не нужен, если используется ItemTemplate
        LanguageComboBox.SelectedIndex = 0; // Выбираем первый язык по умолчанию

        // Можно установить Title и Label из ресурсов, если они уже настроены для дефолтного языка
        // Title = Lang.LanguageSelection_Title; // Пример
        // LabelSelect.Content = Lang.LanguageSelection_Prompt; // Пример
    }

    /// <summary>Обработчик нажатия кнопки OK. Сохраняет выбор и закрывает окно.</summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedLanguage = LanguageComboBox.SelectedItem as LanguageInfo;
        DialogResult = true;
    }
}