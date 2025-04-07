using Field_of_Wonders.Models;

namespace Field_of_Wonders;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Puzzle puzzle = new("Какой цвет у неба?", "Голубой", "Цвета");
    }
}