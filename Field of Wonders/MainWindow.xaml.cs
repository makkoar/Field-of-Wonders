namespace Field_of_Wonders;

/// <summary>Логика взаимодействия для главного окна приложения MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    /// <summary>Инициализирует новый экземпляр класса <see cref="MainWindow"/> и обрабатывает критические ошибки инициализации.</summary>
    public MainWindow()
    {
        InitializeComponent();

        try
        {
            // TODO: Здесь будет основная инициализация ViewModel и привязка DataContext
            // Например: this.DataContext = new MainViewModel();
            //           ((MainViewModel)this.DataContext).InitializeGame();
        }
        catch (Exception ex) // Перехватываем общие ошибки при инициализации ViewModel или игровой логики
        {
            HandleInitializationError(string.Format(Lang.Error_UnexpectedInitializationFailed_Format, ex.Message));
        }
    }

    /// <summary>Обрабатывает критическую ошибку инициализации: показывает сообщение и завершает приложение.</summary>
    /// <param name="userMessage">Текст сообщения об ошибке для пользователя.</param>
    private void HandleInitializationError(string userMessage)
    {
        _ = MessageBox.Show(userMessage, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error);
        // Безопасное завершение приложения через Dispatcher
        Dispatcher.Invoke(() => Application.Current?.Shutdown(1));
    }
}