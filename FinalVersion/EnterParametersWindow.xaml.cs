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
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace Prototype
{
    /// <summary>
    /// Логика взаимодействия для EnterParameters.xaml
    /// </summary>
    public partial class EnterParametersWindow : Window
    {
        public DataTable Table = new DataTable();

        public EnterParametersWindow(string[] Names)
        {
            InitializeComponent();
            for (int i = 0; i < Names.Length; i++)
                Table.Columns.Add(new DataColumn(i.ToString(), typeof(string)));
            Table.Rows.Add(Table.NewRow());
            Parameters.ItemsSource = null;
            Parameters.ItemsSource = Table.DefaultView;
            for (int i = 0; i < Names.Length; i++)
            {
                var textColumn = new DataGridTextColumn() { Header = Names[i], Binding = new Binding("[" + i.ToString() + "]") };
                Parameters.Columns.Add(textColumn);
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < Table.Columns.Count; i++)
                    double.Parse(Table.Rows[0][i].ToString(), CultureInfo.InvariantCulture);
            }
            catch
            {
                MessageBox.Show("Введённые параметры должны быть вещественными числами с точкой-разделителем");
                return;
            }
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "txt files (.txt)|*.txt";
            dlg.DefaultExt = ".txt";
            dlg.RestoreDirectory = true;
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            if (dlg.ShowDialog() != true)
                return;
            FileStream stream = null;
            StreamReader file = null;
            try
            {
                stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                file = new StreamReader(stream, Encoding.Default);
                int NumOfCell = 0;
                string[] Fields = new string[Table.Columns.Count];
                while (!file.EndOfStream)
                {
                    if (NumOfCell >= Fields.Length)
                        break;
                    string Field = file.ReadLine();
                    Field.Replace(" ", string.Empty);
                    Fields[NumOfCell] = Field;
                    NumOfCell++;
                }
                for (int i = 0; i < Fields.Length; i++)
                    Table.Rows[0][i] = Fields[i];
            }
            catch (Exception)
            {
                MessageBox.Show("Невозможно открыть данный файл");
            }
            finally
            {
                file?.Dispose();
                stream?.Dispose();
            }
        }

        private void SaveFileClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "txt files (.txt)|*.txt";
            dlg.DefaultExt = ".txt";
            FileStream stream = null;
            StreamWriter file = null;
            if (dlg.ShowDialog() != true)
                return;
            try
            {
                stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                file = new StreamWriter(stream, Encoding.Default);
                List<string> lst = new List<string>();
                foreach (string cur in Table.Rows[0].ItemArray)
                    lst.Add(cur);
                foreach (var cur in lst)
                    file.WriteLine(cur);
            }
            catch (Exception)
            {
                MessageBox.Show("Невозможно сохранить в данный файл");
            }
            finally
            {
                file?.Dispose();
                stream?.Dispose();
            }
        }
    }
}
