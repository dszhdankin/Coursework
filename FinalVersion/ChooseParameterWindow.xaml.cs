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
    /// Логика взаимодействия для ChooseWindow.xaml
    /// </summary>
    public partial class ChooseParameterWindow : Window
    {

        public ChooseParameterWindow(string[] NamesOfParameters)
        {
            InitializeComponent();
            foreach (var cur in NamesOfParameters)
                Names.Items.Add(cur);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (Names.SelectedIndex == -1)
                return;
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
