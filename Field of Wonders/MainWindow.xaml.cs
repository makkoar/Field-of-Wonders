namespace Field_of_Wonders;

/// <summary>Логика взаимодействия для главного окна приложения MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    /// <summary>Инициализирует новый экземпляр класса <see cref="MainWindow"/>. Пытается создать базовую игровую логику и обрабатывает критические ошибки инициализации.</summary>
    public MainWindow()
    {
        InitializeComponent();

        try
        {
            // Временная инициализация для проверки.
            // В будущем здесь будет инициализация ViewModel или GameManager
            // Важно: Передан непустой вопрос, чтобы избежать ArgumentException.
            Puzzle puzzle = new("Какой цвет у неба?", "Голубой", "Цвета");

            // TODO: Здесь будет основная инициализация ViewModel и привязка DataContext
            // this.DataContext = new MainViewModel(puzzle);

        }
        catch (ArgumentException argEx) // Ошибка валидации начальных данных (например, пустой вопрос/ответ)
        {
            HandleInitializationError($"{Lang.Error_DataInitializationFailed_Prefix}\n{argEx.Message}");
        }
        catch (Exception ex) // Другие непредвиденные ошибки при инициализации
        {
            HandleInitializationError($"{Lang.Error_UnexpectedInitializationFailed_Prefix}\n{ex.Message}");
        }
    }

    /// <summary>Обрабатывает критическую ошибку инициализации: показывает сообщение и завершает приложение.</summary>
    /// <param name="userMessage">Текст сообщения об ошибке для пользователя.</param>
    private void HandleInitializationError(string userMessage)
    {
        _ = MessageBox.Show(userMessage, Lang.Error_Critical_Title, MessageBoxButton.OK, MessageBoxImage.Error);

        // Безопасное завершение приложения через Dispatcher, если окно еще не полностью готово.
        Dispatcher.Invoke(() => Application.Current?.Shutdown(1));
    }
}