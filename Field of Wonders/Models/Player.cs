namespace Field_of_Wonders.Models;

/// <summary>Представляет игрока в игре "Поле Чудес".</summary>
public class Player
{
    #region Свойства

    /// <summary>Получает имя игрока.</summary>
    public string Name { get; }

    /// <summary>Получает текущий счет игрока.</summary>
    /// <remarks>Может изменяться через методы <see cref="AddScore"/> и <see cref="ResetScore"/>.</remarks>
    public int Score { get; private set; }

    #endregion

    #region Конструктор

    /// <summary>Инициализирует новый экземпляр класса <see cref="Player"/>.</summary>
    /// <param name="name">Имя игрока. Если имя не указано или пустое, будет использовано имя по умолчанию из ресурсов локализации.</param>
    public Player(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            // Используем локализованное имя по умолчанию
            Name = Lang.Player_DefaultName;
            LoggingService.Logger.Information(Lang.Log_PlayerNameDefault_Format, Name); // Используем Information и локализованный формат
        }
        else
        {
            Name = name.Trim();
        }
        Score = 0;
    }

    #endregion

    #region Методы

    /// <summary>Добавляет указанное количество очков к счету игрока.</summary>
    /// <param name="points">Количество добавляемых очков. Может быть отрицательным, но счет не может стать меньше нуля.</param>
    public void AddScore(int points)
    {
        int oldScore = Score;
        Score += points;
        // Если правила игры не допускают отрицательный счет (кроме случая Банкрот, который обрабатывается отдельно)
        if (Score < 0)
        {
            Score = 0;
        }
        LoggingService.Logger.Information(Lang.Log_PlayerScoreAdded_Format, Name, points, Score, oldScore);
    }

    /// <summary>Сбрасывает счет игрока на 0 (например, при банкротстве или начале нового раунда).</summary>
    public void ResetScore()
    {
        int oldScore = Score;
        Score = 0;
        LoggingService.Logger.Information(Lang.Log_PlayerScoreReset_Format, Name, oldScore);
    }

    #endregion
}