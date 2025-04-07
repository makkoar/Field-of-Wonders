using System.Windows.Media.Animation;
using Field_of_Dreams.Properties;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace Field_of_Dreams
{
    public partial class Game : Window
    {
        static int NumberOfSegments = 40;
        static string QuestionsFilePuth = $@"{Environment.CurrentDirectory}\Questions.dll";
        static SolidColorBrush LetterColorBrush = new SolidColorBrush(Color.FromArgb(255, 10, 10, 10));

        Dictionary<int, string> DrumValue = new Dictionary<int, string>();
        int PlayerTurn = 1;
        int NumberOfCorrectAnswers = 2;
        int[] PlayersPoints = new int[6] { 0, 0, 0, 0, 0, 0 };
        string Question, Answer, ProgrammLetter;
        List<string> Letters = new List<string>();
        List<string> BlackLetters = new List<string>();
        List<int> BlackPlayers = new List<int>();
        InputBox inputBox;

        public Game()
        {
            InitializeComponent();
            DispatcherTimer Timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 10)
            };
            SpinTheDrum.Click += (s, e) =>
            {
                Rotate();
                DrumValueText.Text = null;
            };
            TypeTheWholeWord.Click += (s, e) =>
            {
                inputBox = new InputBox(Question);
                if (inputBox.ShowDialog() == true)
                {
                    if (Answer == inputBox.Answer)
                    {
                        AddPoints();
                        PlayerWon();
                    }
                    else
                    {
                        BlackPlayers.Add(PlayerTurn);
                        PlayersPoints[PlayerTurn] = 0;
                        UpdatePlayersPoints();
                        MessageBox.Show($"{ Settings.Default[$"Player_{ PlayerTurn }"].ToString() } выбывает из игры");
                        PlayerTurnNext();
                        if (BlackPlayers.Count() == Settings.Default.NumberOfPlayers - 1)
                        {
                            PlayerWon();
                        }
                    }

                }
            };
            window.KeyDown += (s, e) =>
            {
                if (TypeTheWholeWord.IsVisible)
                {
                    if (CheckCorrectAnswer(e))
                    {
                        if (GetDrumValue == "ПЛЮС" && ProgrammLetter != null)
                        {
                            if (Convert.ToInt32(ProgrammLetter) > Answer.Length) DrumValueText.Text = $"В слове всего { Answer.Length} букв! Выберите другую букву.";
                            else
                            {
                                int NumberOpenLetter = Convert.ToInt32(ProgrammLetter);
                                foreach (TextBlock textBlock in grid.Children.OfType<TextBlock>().ToList())
                                {
                                    if (textBlock.Name == $"Letter_{ NumberOpenLetter }") { textBlock.Foreground = LetterColorBrush; }
                                }
                                OpenLetter(ProgrammLetter);
                                PlayerTurnNext();
                            }
                        }
                        else
                        {
                            if (!CheckInBlackList(ProgrammLetter))
                            {
                                BlackLetters.Add(ProgrammLetter);
                                OpenLetter(ProgrammLetter);
                                AddPoints();
                            }
                            else DrumValueText.Text = $"Вы уже называли букву \"{ ProgrammLetter }\"!";
                        }
                    }
                    else if (ProgrammLetter != null)
                    {
                        NumberOfCorrectAnswers = 2;
                        SpinTheDrum.Visibility = Visibility.Visible;
                        TypeTheWholeWord.Visibility = Visibility.Hidden;
                        DrumValueText.Text = $"В слове нет буквы \"{ ProgrammLetter }\"";
                        PlayerTurnNext();
                    }
                }
            };
            Timer.Tick += (s, e) => {
                SpinTheDrum.Visibility = Visibility.Visible;//test
                window.Title = $"Ход игрока: { Settings.Default[$"Player_{ PlayerTurn }"].ToString() } / { ProgrammLetter }";
            };
            for (int i = 1; i <= NumberOfSegments; i++)
            {
                DrumValue.Add((int)((double)(((360 / NumberOfSegments * i) + (360 / NumberOfSegments / 2)) % 360) / 9), (new string[]
            {
                "НУЛЬ", "50", "25", "БАНКРОТ", "100", "30", "50", "10", "НУЛЬ", "25",
                "250", "5", "БАНКРОТ", "30", "10", "25", "150", "X2", "НУЛЬ", "50",
                "5", "25", "НУЛЬ", "30", "100", "15", "БАНКРОТ", "150", "5", "25",
                "10", "НУЛЬ", "50", "ПЛЮС", "БАНКРОТ", "25", "50", "30", "100", "10"}
            )[i - 1]);
            }
            UpdatePlayersPoints();
            Generate();
            Timer.Start();
        }

        /// <summary>
        /// Процедура отвечающая за победу текущего игрока.
        /// </summary>
        private void PlayerWon()
        {
            if (inputBox?.Content?.ToString() == Answer || BlackPlayers.Count() == Settings.Default.NumberOfPlayers - 1) MessageBox.Show($"Побеждает { Settings.Default[$"Player_{ PlayerTurn }"].ToString() } с { PlayersPoints[PlayerTurn - 1] }очк.");
            else
            {
                int max = int.MinValue;
                int Won = 0;
                foreach (int Points in PlayersPoints)
                {
                    if (PlayersPoints[Points] > max)
                    {
                        max = PlayersPoints[Points];
                        Won = Points;
                    }
                }
                MessageBox.Show($"Побеждает { Settings.Default[$"Player_{ Won + 1 }"].ToString() } с { PlayersPoints[Won] }очк.");
            }
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Преедача хода следующему игроку.
        /// </summary>
        private void PlayerTurnNext()
        {
            if (SpinTheDrum.Visibility != Visibility.Visible || GetDrumValue == "НУЛЬ" || GetDrumValue == "БАНКРОТ")
            {
                SpinTheDrum.Visibility = Visibility.Visible;
                TypeTheWholeWord.Visibility = Visibility.Hidden;
                PlayerTurn = (PlayerTurn < Settings.Default.NumberOfPlayers) ? PlayerTurn + 1 : 1;
                foreach (int playerTurn in BlackPlayers)
                {
                    if (playerTurn == PlayerTurn)
                    {
                        PlayerTurnNext();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Открытие угаданной буквы
        /// </summary>
        /// <param name="Letter">Буква которую угадали</param>
        private void OpenLetter(string Letter)
        {
            DrumValueText.Text = $"Откройте букву \"{ Letter }\"!";
            foreach (TextBlock textBlock in grid.Children.OfType<TextBlock>().ToList())
            {
                if (textBlock.Text == Letter) textBlock.Foreground = LetterColorBrush;  
            }
            int i = 0;
            foreach (TextBlock textBlock in grid.Children.OfType<TextBlock>().ToList())
                if (textBlock.Foreground == LetterColorBrush) i++;
            if (i == Answer.Length) PlayerWon();
        }

        /// <summary>
        /// Процедура добаавляет текущему игроку колличество очков указанное на барабане.
        /// </summary>
        private void AddPoints()
        {
            switch (GetDrumValue)
            {
                case "ПЛЮС": break;
                case "X2":
                    PlayersPoints[PlayerTurn - 1] *= NumberOfCorrectAnswers;
                    NumberOfCorrectAnswers++;
                    break;
                default: PlayersPoints[PlayerTurn - 1] += int.Parse(GetDrumValue); break;
            }
            UpdatePlayersPoints();
        }

        /// <summary>
        /// Поиск буквы в BlackList
        /// </summary>
        /// <param name="Letter">Буква, которую ищем</param>
        private bool CheckInBlackList(string Letter)
        {
            foreach (string blackLetter in BlackLetters)
            {
                if (Letter == blackLetter)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Метод проверяет правильность ответа
        /// </summary>
        /// <param name="e">Нажатая кнопка</param>
        private bool CheckCorrectAnswer(KeyEventArgs e)
        {
            ProgrammLetter = e.Key.ToString().ToUpper();
            if (GetDrumValue == "ПЛЮС")
            {
                switch (e.Key.ToString().ToUpper())
                {
                    case "D1": ProgrammLetter = "1"; break;
                    case "D2": ProgrammLetter = "2"; break;
                    case "D3": ProgrammLetter = "3"; break;
                    case "D4": ProgrammLetter = "4"; break;
                    case "D5": ProgrammLetter = "5"; break;
                    case "D6": ProgrammLetter = "6"; break;
                    case "D7": ProgrammLetter = "7"; break;
                    case "D8": ProgrammLetter = "8"; break;
                    case "D9": ProgrammLetter = "9"; break;
                    default: ProgrammLetter = null; break;
                }
                return true;
            }
            switch (e.Key.ToString().ToUpper())
            {
                case "F": ProgrammLetter = "А"; break;
                case "OEMCOMMA": ProgrammLetter = "Б"; break;
                case "D": ProgrammLetter = "В"; break;
                case "U": ProgrammLetter = "Г"; break;
                case "L": ProgrammLetter = "Д"; break;
                case "T": ProgrammLetter = "Е"; break;
                case "OEM3": ProgrammLetter = "Ё"; break;
                case "OEM1": ProgrammLetter = "Ж"; break;
                case "P": ProgrammLetter = "З"; break;
                case "B": ProgrammLetter = "И"; break;
                case "Q": ProgrammLetter = "Й"; break;
                case "R": ProgrammLetter = "К"; break;
                case "K": ProgrammLetter = "Л"; break;
                case "V": ProgrammLetter = "М"; break;
                case "Y": ProgrammLetter = "Н"; break;
                case "J": ProgrammLetter = "О"; break;
                case "G": ProgrammLetter = "П"; break;
                case "H": ProgrammLetter = "Р"; break;
                case "C": ProgrammLetter = "С"; break;
                case "N": ProgrammLetter = "Т"; break;
                case "E": ProgrammLetter = "У"; break;
                case "A": ProgrammLetter = "Ф"; break;
                case "OEMOPENBRACKETS": ProgrammLetter = "Х"; break;
                case "W": ProgrammLetter = "Ц"; break;
                case "X": ProgrammLetter = "Ч"; break;
                case "I": ProgrammLetter = "Ш"; break;
                case "O": ProgrammLetter = "Щ"; break;
                case "OEM6": ProgrammLetter = "Ъ"; break;
                case "S": ProgrammLetter = "Ы"; break;
                case "M": ProgrammLetter = "Ь"; break;
                case "OEMQUOTES": ProgrammLetter = "Э"; break;
                case "OEMPERIOD": ProgrammLetter = "Ю"; break;
                case "Z": ProgrammLetter = "Я"; break;
                default: ProgrammLetter = null; break;
            }
            foreach (char Letter in Answer)
                if (Convert.ToChar(Letter).ToString() == ProgrammLetter) return true;
            return false;
        }

        /// <summary>
        /// Метод обновляет отображаемые данные об очках игроков
        /// </summary>
        private void UpdatePlayersPoints()
        {
            int ii = 0;
            foreach (TextBlock textBlock in stackPanel.Children.OfType<TextBlock>().ToList())
            {
                if (ii + 1 <= Settings.Default.NumberOfPlayers) textBlock.Text = $"{ Settings.Default[$"Player_{ii + 1}"].ToString() } - {PlayersPoints[ii]} очков";
                else textBlock.Text = null;
                ii++;
            }
        }

        /// <summary>
        /// Метод генерирует слово для игры
        /// </summary>
        private void Generate()
        {
            string[] QuestionsFile = File.ReadAllLines(QuestionsFilePuth, Encoding.GetEncoding(1251));
            string QuestionAndAnswer = QuestionsFile[new Random().Next(0, QuestionsFile.Length)];
            Question = QuestionAndAnswer.Split(':')[0];
            Answer = QuestionAndAnswer.Split(':')[1].ToUpper();
            question.Text = $"Вопрос:\n{ Question }";
            NewLetter();
        }

        /// <summary>
        /// Процедура создания букв для окна
        /// </summary>
        private void NewLetter()
        {
            for (int i = 0; i < Answer.Length; i++)
            {
                char ch = Answer[i];
                Letters.Add(Convert.ToChar(ch).ToString());
                TextBlock Letter = new TextBlock
                {
                    Name = $"Letter_{ i + 1 }",
                    Height = 20,
                    Margin = new Thickness(2, 15, 2, 15),
                    TextWrapping = TextWrapping.Wrap,
                    Background = Brushes.White,
                    OpacityMask = Brushes.Black,
                    Padding = new Thickness(5, 0, 0, 0),
                    FontSize = 16,
                    Foreground = Brushes.White,
                    Text = Letters[i]
                };
                AddElement(Letter, i + 1, 0);
            }
        }

        /// <summary>
        /// Процедура добавляет указаный элемент на Grid
        /// </summary>
        /// <param name="Object">Добавляемый объект</param>
        /// <param name="Column">Столбец в который добовляется объект</param>
        /// <param name="Row">Строка в который добовляется объект</param>
        private void AddElement(UIElement Object, int Column, int Row)
        {
            Grid.SetColumn(Object, Column);
            Grid.SetRow(Object, Row);
            grid.Children.Add(Object);
        }

        /// <summary>
        /// Функция возвращает значение на барабане
        /// </summary>
        private string GetDrumValue => DrumValue[(int)((DrumRotate.Angle + 4.5 + 270) % 360 / 9)];

        /// <summary>
        /// Метод отвечает за вращение барабана.
        /// </summary>
        async private void Rotate()
        {
            SpinTheDrum.Visibility = Visibility.Hidden;
            int r = new Random().Next(360, 720);
            DoubleAnimation Animation = new DoubleAnimation
            {
                From = DrumRotate.Angle % 360,
                By = r,
                Duration = new Duration(TimeSpan.FromSeconds(5)),
                AccelerationRatio = 0.25,
                DecelerationRatio = 0.25
            };
            DrumRotate.BeginAnimation(RotateTransform.AngleProperty, Animation);
            await Task.Run(() => Thread.Sleep(5050) );
            Animation = new DoubleAnimation
            {
                From = DrumRotate.Angle % 360,
                By = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0)),
            };
            DrumRotate.BeginAnimation(RotateTransform.AngleProperty, Animation);
            switch (GetDrumValue)
            {
                case "БАНКРОТ":
                    DrumValueText.Text = $"Сектор { GetDrumValue } на барабане. В следующий раз повезёт!";
                    PlayersPoints[PlayerTurn - 1] = 0;
                    UpdatePlayersPoints();
                    PlayerTurnNext();
                    break;
                case "НУЛЬ":
                    DrumValueText.Text = $"Сектор { GetDrumValue } на барабане. В следующий раз повезёт!";
                    PlayerTurnNext();
                    break;
                case "ПЛЮС":
                    TypeTheWholeWord.Visibility = Visibility.Visible;
                    DrumValueText.Text = $"Сектор { GetDrumValue } на барабане. Номер буквы __!";
                    break;
                case "X2":
                    TypeTheWholeWord.Visibility = Visibility.Visible;
                    DrumValueText.Text = $"Сектор { GetDrumValue } на барабане. Буква __!";
                    break;
                default:
                    TypeTheWholeWord.Visibility = Visibility.Visible;
                    DrumValueText.Text = $"{ GetDrumValue } очков на барабане. Буква __!";
                    break;
            }
        }
    }
}