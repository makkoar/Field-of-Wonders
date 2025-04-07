namespace Field_of_Wonders.Models;

/// <summary>Представляет загадку (слово и вопрос) в игре "Поле Чудес".</summary>
public class Puzzle
{
    #region Поля и Константы

    /// <summary>Символ-заполнитель для неоткрытых букв.</summary>
    private const char Placeholder = '_';
    /// <summary>Массив символов, представляющий текущее видимое состояние слова.</summary>
    private readonly char[] _revealedLetters;

    #endregion

    #region Свойства

    /// <summary>Получает текст вопроса или подсказки.</summary>
    public string Question { get; }
    /// <summary>Получает загаданное слово (в верхнем регистре).</summary>
    public string Answer { get; }
    /// <summary>Получает категорию или тему слова (может быть пустой).</summary>
    public string Category { get; }

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="Puzzle"/>.</summary>
    /// <param name="question">Текст вопроса.</param>
    /// <param name="answer">Загаданное слово.</param>
    /// <param name="category">Категория слова (необязательно).</param>
    /// <exception cref="ArgumentException">Генерируется, если вопрос или ответ пустые.</exception>
    public Puzzle(string question, string answer, string category = "")
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException(Lang.Error_QuestionCannotBeEmpty, nameof(question));
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException(Lang.Error_AnswerCannotBeEmpty, nameof(answer));

        Question = question;
        Answer = answer.ToUpperInvariant(); // Нормализуем ответ к верхнему регистру
        Category = category ?? string.Empty;

        _revealedLetters = new char[Answer.Length];
        for (int i = 0; i < Answer.Length; i++)
        {
            // Небуквенные символы (пробелы, дефисы и т.д.) показываем сразу
            _revealedLetters[i] = !char.IsLetter(Answer[i]) ? Answer[i] : Placeholder;
        }
    }

    #endregion

    #region Публичные Методы

    /// <summary>Возвращает строку, представляющую текущее состояние отгадываемого слова.</summary>
    /// <returns>Строка с текущим состоянием слова (например, "_ О _ Е _ _ _ Е _").</returns>
    public string GetCurrentState() => new(_revealedLetters);

    /// <summary>Проверяет наличие буквы в слове и открывает ее.</summary>
    /// <param name="letter">Предполагаемая буква.</param>
    /// <returns><c>true</c>, если хотя бы одна новая буква была открыта; иначе <c>false</c>.</returns>
    public bool GuessLetter(char letter)
    {
        bool letterFound = false;
        char upperLetter = char.ToUpperInvariant(letter); // Сравниваем без учета регистра

        if (!char.IsLetter(upperLetter)) return false; // Игнорируем не-буквы

        for (int i = 0; i < Answer.Length; i++)
        {
            if (Answer[i] == upperLetter && _revealedLetters[i] == Placeholder)
            {
                _revealedLetters[i] = upperLetter;
                letterFound = true; // Отмечаем, что нашли и открыли хотя бы одну букву
            }
        }
        return letterFound;
    }

    /// <summary>Проверяет, полностью ли отгадано слово.</summary>
    /// <returns><c>true</c>, если все буквы в слове открыты; иначе <c>false</c>.</returns>
    public bool IsSolved()
    {
        for (int i = 0; i < Answer.Length; i++)
        {
            // Если символ в ответе - буква, но она все еще скрыта - слово не решено
            if (char.IsLetter(Answer[i]) && _revealedLetters[i] == Placeholder)
            {
                return false;
            }
        }
        return true; // Все буквы открыты
    }

    /// <summary>Проверяет, совпадает ли предложенное слово с загаданным (без учета регистра).</summary>
    /// <param name="word">Предполагаемое слово.</param>
    /// <returns><c>true</c>, если слова совпадают; иначе <c>false</c>.</returns>
    public bool GuessWord(string word) =>
        !string.IsNullOrWhiteSpace(word) && Answer.Equals(word.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Открывает все буквы в слове (например, если игрок проиграл).</summary>
    public void RevealAll() => Array.Copy(Answer.ToCharArray(), _revealedLetters, Answer.Length);

    #endregion
}