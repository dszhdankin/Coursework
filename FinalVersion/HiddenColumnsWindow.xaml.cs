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
    public partial class HiddenColumnsWindow : Window
    {
        public HiddenColumnsWindow(List<ParametrName> parametrNames)
        {
            InitializeComponent();
            foreach (var cur in parametrNames)
                if (cur.Hidden)
                    HiddenColumns.Items.Add(cur.Name);
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            HiddenColumns.SelectAll();
        }
    }
}
