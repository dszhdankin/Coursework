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
using System.IO;

namespace Prototype
{
    /// <summary>
    /// Логика взаимодействия для EnterModelNameWindow.xaml
    /// </summary>
    public partial class EnterColumnNameWindow : Window
    {
        public EnterColumnNameWindow()
        {
            InitializeComponent();
            ColumnNameBox.Focus();
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            bool success = false;
            foreach (var cur in ColumnNameBox.Text)
                if (char.IsLetterOrDigit(cur))
                    success = true;
            if (success)
                DialogResult = true;
            else
                MessageBox.Show("Название параметра должно содержать по крайней мере одну букву или цифру");
        }
    }
}
