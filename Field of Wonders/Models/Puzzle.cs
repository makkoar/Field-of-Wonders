namespace Field_of_Wonders.Models;

/// <summary>Представляет загадку (слово и вопрос) в игре "Поле Чудес". Хранит состояние отгадываемого слова и управляет процессом его открытия.</summary>
public class Puzzle
{
    #region Константы

    /// <summary>Символ-заполнитель для неоткрытых букв.</summary>
    private const char Placeholder = '_';

    #endregion

    #region Свойства

    /// <summary>Получает текст вопроса или подсказки для загаданного слова.</summary>
    public string Question { get; }

    /// <summary>Получает загаданное слово (в верхнем регистре для согласованности).</summary>
    public string Answer { get; }

    /// <summary>Получает категорию или тему загаданного слова (опционально).</summary>
    public string Category { get; }

    #endregion

    #region Приватные поля

    /// <summary>Массив символов, представляющий текущее видимое состояние слова.</summary>
    private readonly char[] _revealedLetters;

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="Puzzle"/>.</summary>
    /// <param name="question">Текст вопроса или подсказки.</param>
    /// <param name="answer">Загаданное слово.</param>
    /// <param name="category">Категория слова (необязательно).</param>
    /// <exception cref="ArgumentException">Генерируется, если <paramref name="question"/> или <paramref name="answer"/> являются пустой строкой или строкой из пробельных символов.</exception>
    public Puzzle(string question, string answer, string category = "")
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException(Lang.Error_QuestionCannotBeEmpty, nameof(question));
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException(Lang.Error_AnswerCannotBeEmpty, nameof(answer));

        Question = question;
        Answer = answer.ToUpperInvariant();
        Category = category ?? string.Empty;

        _revealedLetters = new char[Answer.Length];
        for (int i = 0; i < Answer.Length; i++)
        {
            _revealedLetters[i] = !char.IsLetter(Answer[i]) ? Answer[i] : Placeholder;
        }
    }

    #endregion

    #region Публичные методы

    /// <summary>Возвращает строку, представляющую текущее состояние отгадываемого слова.</summary>
    /// <returns>Строка с текущим состоянием слова (например, "_ О _ Е _ _ _ Е _").</returns>
    public string GetCurrentState() => new(_revealedLetters);

    /// <summary>Проверяет наличие указанной буквы в загаданном слове и открывает ее.</summary>
    /// <param name="letter">Предполагаемая буква.</param>
    /// <returns><c>true</c>, если хотя бы одна новая буква была открыта; иначе <c>false</c>.</returns>
    public bool GuessLetter(char letter)
    {
        bool letterFound = false;
        char upperLetter = char.ToUpperInvariant(letter);

        if (!char.IsLetter(upperLetter))
        {
            return false;
        }

        for (int i = 0; i < Answer.Length; i++)
        {
            if (Answer[i] == upperLetter && _revealedLetters[i] == Placeholder)
            {
                _revealedLetters[i] = upperLetter;
                letterFound = true;
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
            if (char.IsLetter(Answer[i]) && _revealedLetters[i] == Placeholder)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Проверяет, совпадает ли предложенное слово с загаданным словом (без учета регистра).</summary>
    /// <param name="word">Предполагаемое слово целиком.</param>
    /// <returns><c>true</c>, если слова совпадают; иначе <c>false</c>.</returns>
    public bool GuessWord(string word) => !string.IsNullOrWhiteSpace(word) && Answer.Equals(word.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Открывает все буквы в слове.</summary>
    public void RevealAll() => Array.Copy(Answer.ToCharArray(), _revealedLetters, Answer.Length);

    #endregion
}
