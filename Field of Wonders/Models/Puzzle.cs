using Field_of_Wonders.Localization;

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

    /// <summary>Получает загаданное слово (в верхнем регистре для согласованности). Используется для проверки, но не отображается игроку напрямую.</summary>
    public string Answer { get; }

    /// <summary>Получает категорию или тему загаданного слова (опционально).</summary>
    public string Category { get; }

    #endregion

    #region Приватные поля

    /// <summary>Массив символов, представляющий текущее видимое состояние слова. Неотгаданные буквы представлены символом-заполнителем <see cref="Placeholder"/>.</summary>
    private readonly char[] _revealedLetters;

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="Puzzle"/>.</summary>
    /// <param name="question">Текст вопроса или подсказки.</param>
    /// <param name="answer">Загаданное слово.</param>
    /// <param name="category">Категория слова (необязательно).</param>
    /// <exception cref="ArgumentNullException">Генерируется, если <paramref name="question"/> или <paramref name="answer"/> равны null.</exception>
    /// <exception cref="ArgumentException">Генерируется, если <paramref name="answer"/> или <paramref name="question"/> являются пустой строкой или строкой из пробельных символов.</exception>
    public Puzzle(string question, string answer, string category = "")
    {
        // Проверка входных данных
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException(Lang.Error_QuestionCannotBeEmpty, nameof(question));
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException(Lang.Error_AnswerCannotBeEmpty, nameof(answer));
            
        Question = question;
        Answer = answer.ToUpperInvariant(); // Сразу приводим ответ к верхнему регистру
        Category = category ?? string.Empty; // Убедимся, что Category не null

        // Инициализация массива для отображения букв
        _revealedLetters = new char[Answer.Length];
        for (int i = 0; i < Answer.Length; i++)
        {
            // Сразу открываем не-буквы
            if (!char.IsLetter(Answer[i]))
            {
                _revealedLetters[i] = Answer[i];
            }
            else
            {
                _revealedLetters[i] = Placeholder;
            }
        }
    }

    #endregion

    #region Публичные методы

    /// <summary>Возвращает строку, представляющую текущее состояние отгадываемого слова, где неоткрытые буквы заменены на символ-заполнитель (<see cref="Placeholder"/>).</summary>
    /// <returns>Строка с текущим состоянием слова (например, "_ О _ Е _ _ _ Е _").</returns>
    public string GetCurrentState()
    {
        return new string(_revealedLetters);
    }

    /// <summary>Проверяет наличие указанной буквы в загаданном слове и открывает ее, если она найдена. Сравнение происходит без учета регистра. Буквы, не являющиеся стандартными буквами алфавита, игнорируются.</summary>
    /// <param name="letter">Предполагаемая буква.</param>
    /// <returns><c>true</c>, если хотя бы одна новая буква была открыта; иначе <c>false</c>.</returns>
    public bool GuessLetter(char letter)
    {
        bool letterFound = false;
        char upperLetter = char.ToUpperInvariant(letter);

        if (!char.IsLetter(upperLetter))
        {
            return false; // Игнорируем не-буквы
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

    /// <summary>Проверяет, полностью ли отгадано слово (все ли буквы открыты).</summary>
    /// <returns><c>true</c>, если все буквы в слове открыты; иначе <c>false</c>.</returns>
    public bool IsSolved()
    {
        for (int i = 0; i < Answer.Length; i++)
        {
            if (char.IsLetter(Answer[i]) && _revealedLetters[i] == Placeholder)
            {
                return false; // Нашли неоткрытую букву
            }
        }
        return true; // Неоткрытых букв не найдено
    }

    /// <summary>Проверяет, совпадает ли предложенное слово с загаданным словом. Сравнение происходит без учета регистра.</summary>
    /// <param name="word">Предполагаемое слово целиком.</param>
    /// <returns><c>true</c>, если слова совпадают; иначе <c>false</c>.</returns>
    public bool GuessWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }
        return Answer.Equals(word.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Открывает все буквы в слове. Может использоваться, например, при неправильном угадывании слова целиком или для отображения ответа в конце раунда.</summary>
    public void RevealAll()
    {
        // Копируем символы из строки Answer в массив _revealedLetters
        Array.Copy(Answer.ToCharArray(), _revealedLetters, Answer.Length);
    }

    #endregion
}