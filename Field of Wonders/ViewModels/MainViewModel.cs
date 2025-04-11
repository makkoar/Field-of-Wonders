namespace Field_of_Wonders.ViewModels;

/// <summary>ViewModel для главного окна приложения.</summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _mainWindowTitle = string.Empty;

    /// <summary>Инициализирует новый экземпляр класса <see cref="MainViewModel"/>.</summary>
    public MainViewModel()
    {
        LocalizationService.CultureChanged += OnCultureChanged;
        UpdateMainWindowTitle();
    }

    private void UpdateMainWindowTitle() => MainWindowTitle = Lang.MainWindow_Title;

    private void OnCultureChanged(object? sender, EventArgs e) => UpdateMainWindowTitle();

    // TODO: Добавить остальные свойства и команды для MainViewModel
}