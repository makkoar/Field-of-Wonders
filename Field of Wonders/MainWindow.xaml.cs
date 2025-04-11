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
            DataContext = new MainViewModel();
            // TODO: Здесь будет основная инициализация ViewModel
            //           ((MainViewModel)this.DataContext).InitializeGame();
        }
        catch (Exception ex) // Перехватываем общие ошибки при инициализации ViewModel или игровой логики
        {
            HandleInitializationError(string.Format(Lang.Error_UnexpectedInitializationFailed_Format, ex.Message));
        }
    }

    /// <summary>Обрабатывает критическую ошибку инициализации: показывает сообщение и завершает приложение.</summary>
    /// <param name="userMessage">Текст сообщения об ошибке для пользователя.</param>
    internal static void HandleInitializationError(string userMessage) =>
        // Используем статический метод App для показа MessageBox и логгирования
        App.ShowAndLogCriticalError(userMessage);// Приложение завершится в App.ShowAndLogCriticalError
}