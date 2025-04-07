using System.Windows;
using System;


namespace Field_of_Dreams
{
    public partial class InputBox : Window
    {
        public InputBox(string question, string defaultAnswer = "")
        {
            InitializeComponent();
            Question.Text = question;
            TAnswer.Text = defaultAnswer;
            Reply.Click += (s, e) => { DialogResult = true; };
        }

        private void ContentRender(object sender, EventArgs e)
        {
            TAnswer.SelectAll();
            TAnswer.Focus();
        }

        public string Answer
        {
            get { return TAnswer.Text.ToUpper(); }
        }
    }

}
