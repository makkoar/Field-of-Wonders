namespace Field_of_Wonders.ViewModels;

/// <summary>ViewModel для главного окна приложения.</summary>
public partial class MainViewModel : ObservableObject
{
    #region Свойства (Observable)

    [ObservableProperty]
    private string _mainWindowTitle = string.Empty;

    // TODO: Добавить остальные свойства ViewModel

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="MainViewModel"/>.</summary>
    public MainViewModel()
    {
        LocalizationService.CultureChanged += OnCultureChanged;
        UpdateMainWindowTitle();
        // TODO: Добавить инициализацию игры
    }

    #endregion

    #region Методы

    private void UpdateMainWindowTitle() => MainWindowTitle = Lang.MainWindow_Title;

    // TODO: Добавить команды и другие методы ViewModel

    #endregion

    #region Обработчики Событий

    private void OnCultureChanged(object? sender, EventArgs e) => UpdateMainWindowTitle();

    #endregion
}