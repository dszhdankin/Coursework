using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Prototype
{
    /// <summary>
    /// Логика взаимодействия для ResultPresentationWindow.xaml
    /// </summary>
    public partial class ResultPresentationWindow : Window
    {

        public ResultPresentationWindow(string presentationString, double presentationField)
        {
            InitializeComponent();
            PresentationString.Text = presentationString;
            PresentationField.Text = $"{presentationField:F2}";
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
