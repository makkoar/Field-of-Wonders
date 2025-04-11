namespace Field_of_Wonders;

/// <summary>Логика взаимодействия для главного окна приложения MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    #region Конструктор

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
            // Используем локализованный заголовок, так как культура должна была быть установлена ранее
            HandleInitializationError(string.Format(Lang.Error_UnexpectedInitializationFailed_Format, ex.Message));
        }
    }

    #endregion

    #region Статические Методы

    /// <summary>Обрабатывает критическую ошибку инициализации: показывает сообщение и завершает приложение.</summary>
    /// <param name="userMessage">Текст сообщения об ошибке для пользователя.</param>
    internal static void HandleInitializationError(string userMessage) =>
        App.ShowAndLogCriticalError(userMessage, useLocalization: true);

    #endregion
}