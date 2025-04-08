namespace Field_of_Wonders.ViewModels;

/// <summary>ViewModel для главного окна приложения.</summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _mainWindowTitle;

    /// <summary>Инициализирует новый экземпляр класса <see cref="MainViewModel"/>.</summary>
    public MainViewModel()
    {
        LocalizationService.CultureChanged += OnCultureChanged; // Подписываемся на событие смены культуры
        UpdateMainWindowTitle(); // Устанавливаем начальный заголовок
    }

    private void UpdateMainWindowTitle()
    {
        MainWindowTitle = Lang.MainWindow_Title;
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        UpdateMainWindowTitle(); // Обновляем заголовок при изменении культуры
    }

    // TODO: Добавьте остальные свойства и команды для MainViewModel
}