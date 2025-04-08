using Field_of_Wonders.ViewModels;

namespace Field_of_Wonders;

/// <summary>Логика взаимодействия для главного окна приложения MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    /// <summary>Инициализирует новый экземпляр класса <see cref="MainWindow"/> и обрабатывает критические ошибки инициализации.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
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
    internal static void HandleInitializationError(string userMessage)
    {
        LoggingService.Logger.Fatal(userMessage); // Логгируем критическую ошибку
        App.ShowAndLogCriticalError(userMessage); // Используем статический метод App для показа MessageBox и логгирования
        // Приложение завершится в App.ShowAndLogCriticalError
    }
}