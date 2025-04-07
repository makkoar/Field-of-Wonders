using System.Runtime.InteropServices;
using Field_of_Dreams.Properties;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Field_of_Dreams
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        public MainWindow()
        {
            InitializeComponent();
            CheckSendboxie();
            LoadSettings();
            Number_of_Players.ValueChanged += (s, e) => 
            {
                SaveSettings();
                LoadSettings();
            };
            Start_Game.Click += (s, e) => 
            {
                new Game().Show();
                Close();
            };
        }

        /// <summary>
        /// Процедура проверки на наличие сторонних ПО
        /// </summary>
        private void CheckSendboxie()
        {
            bool IsSandboxed() { return GetModuleHandle("SbieDll.dll") != IntPtr.Zero; }
            if (IsSandboxed())
            {
                MessageBox.Show($"Обнаружен запуск из под SendBoxie. \nПриложение \"{Settings.Default.ProgeamName}\" будет приостановлено.", "Внимание!");
                Application.Current.Shutdown();
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
        /// Процедура добавления игроков
        /// </summary>
        /// <param name="Count">Количество игроков</param>
        private void AddPlayers(int Count)
        {
            int Column = 1;
            int Row = 1;
            var textBoxes = grid.Children.OfType<TextBox>().ToList();
            foreach (var textBox in textBoxes) { grid.Children.Remove(textBox); }
            for (int i = 0; i <= Count - 1; i++)
            {
                TextBox textBox = new TextBox
                {
                    Text = $"Игрок №{ i + 1 }",
                    Height = 20,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                textBox.TextChanged += (s, e) =>
                {
                    SaveSettings();
                };
                switch (Column)
                {
                    case 1:
                        AddElement(textBox, Column - 1 , Row);
                        Column = 2;
                        break;
                    case 2:
                        AddElement(textBox, Column - 1, Row);
                        Column = 1;
                        Row++;
                        break;
                }
                SaveSettings();
            }
            
        }

        /// <summary>
        /// Процедура сохранения настроек
        /// </summary>
        private void SaveSettings()
        {
            int i = 1;
            Settings.Default.NumberOfPlayers = Number_of_Players.Value;
            List<TextBox> textBoxes = grid.Children.OfType<TextBox>().ToList();
            foreach (TextBox textBox in textBoxes)
            {
                Settings.Default[$"Player_{i}"] = textBox.Text;
                i++;
            }
            Settings.Default.Save();
        }

        /// <summary>
        /// Процедура загрузки настроек
        /// </summary>
        private void LoadSettings()
        {
            window.Title = Settings.Default.ProgeamName;
            Number_of_Players.Value = Settings.Default.NumberOfPlayers;
            Number_of_Players_Text.Text = $"Кол-во игроков: {Number_of_Players.Value}";
            AddPlayers((int)Number_of_Players.Value);
        }
    }
}