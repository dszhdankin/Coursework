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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.Globalization;
using Accord.Statistics.Models.Regression.Linear;

namespace Prototype
{
    public class ParametrName
    {
        public string Name { get; set; } = "";
        public bool Hidden { get; set; } = false;
        public ParametrName(string Name = "", bool Hidden = false)
        {
            this.Name = Name;
            this.Hidden = Hidden;
        }
    }

    public partial class MainWindow : Window
    {
        DataTable TrainData = new DataTable();
        List<RegCoef> TrainModel = new List<RegCoef>(), SortedTrainModel = new List<RegCoef>();
        List<ParametrName> NamesOfParameters = new List<ParametrName>();
        MultipleLinearRegression regression = null;
        public MainWindow()
        {
            InitializeComponent();
            TrainData.Columns.Add("0", typeof(string));
            TrainData.Columns.Add("1", typeof(string));
            NamesOfParameters.Add(new ParametrName("Наименование"));
            NamesOfParameters.Add(new ParametrName("Доход"));
            Bind();
        }

        public bool EqualNames(string Name1, string Name2)
        {
            int i = 0, j = 0;
            while (i < Name1.Length || j < Name2.Length)
            {
                while (i < Name1.Length && char.IsWhiteSpace(Name1[i]))
                    i++;
                while (j < Name2.Length && char.IsWhiteSpace(Name2[j]))
                    j++;
                if (i == Name1.Length && j == Name2.Length)
                    break;
                else if (i == Name1.Length || j == Name2.Length)
                    return false;
                else if (Name1[i] != Name2[j])
                    return false;
                else
                {
                    i++;
                    j++;
                }
            }
            return true;
        }

        private void Bind()
        {
            int i = 0;
            TrainGrid.ItemsSource = null;
            TrainGrid.Items.Clear();
            TrainGrid.Columns.Clear();
            TrainGrid.ItemsSource = TrainData.DefaultView;
            foreach (DataColumn cur in TrainData.Columns)
            {
                if (!NamesOfParameters[i].Hidden)
                {
                    var gridcolumn = new DataGridTextColumn()
                    {
                        Header = NamesOfParameters[i].Name,
                        Binding = new Binding("[" + cur.ColumnName + "]")
                    };
                    TrainGrid.Columns.Add(gridcolumn);
                }
                i++;
            }
        }

        private void OpenTrain_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CSV tables (.csv)|*.csv";
            dlg.DefaultExt = ".csv";
            dlg.RestoreDirectory = true;
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            Nullable<bool> result = dlg.ShowDialog();
            DataTable OldData = TrainData;
            var OldNamesOfParametrs = NamesOfParameters;
            try
            {
                if (result == true)
                {
                    TrainData = new DataTable();
                    NamesOfParameters = new List<ParametrName>();
                    string filename = dlg.FileName;
                    using (TextFieldParser parser = new TextFieldParser(filename, Encoding.Default))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(";");
                        bool FirstTime = true;
                        while (!parser.EndOfData)
                        {
                            string[] fields = parser.ReadFields();
                            if (FirstTime)
                            {
                                if (fields.Length < 2)
                                    throw new IncorrectFileException();
                                if (!EqualNames(fields[0], "Наименование") || !EqualNames(fields[fields.Length - 1], "Доход"))
                                    throw new IncorrectFileException();
                                for (int i = 0; i < fields.Length; i++)
                                {
                                    TrainData.Columns.Add(new DataColumn(i.ToString(), typeof(string)));
                                    NamesOfParameters.Add(new ParametrName(fields[i]));
                                }
                                FirstTime = false;
                            }
                            else
                            {
                                DataRow CurRow = TrainData.NewRow();
                                int IndexCurColumn = 0;
                                if (fields.Length != NamesOfParameters.Count)
                                    throw new IncorrectFileException();
                                foreach (string field in fields)
                                {
                                    CurRow[IndexCurColumn] = field;
                                    IndexCurColumn++;
                                }
                                TrainData.Rows.Add(CurRow);
                            }
                        }
                    }
                    Bind();
                }
            }
            catch (IncorrectFileException)
            {
                MessageBox.Show("Файл содержит некорректные данные");
                TrainData?.Dispose();
                TrainData = OldData;
                NamesOfParameters = OldNamesOfParametrs;
            }
            catch (MalformedLineException)
            {
                MessageBox.Show("Файл содержит некорректные данные");
                TrainData?.Dispose();
                TrainData = OldData;
                NamesOfParameters = OldNamesOfParametrs;
            }
            catch (Exception)
            {
                MessageBox.Show("Невозможно открыть данный файл");
                TrainData?.Dispose();
                TrainData = OldData;
                NamesOfParameters = OldNamesOfParametrs;
            }
            finally
            {
                Bind();
            }
        }

        private double StandardDeviation(double[] a)
        {
            double midoutputs = 0.0;
            for (int i = 0; i < a.Length; i++)
                midoutputs += a[i];
            midoutputs /= a.Length;
            double qy = 0.0;
            for (int i = 0; i < a.Length; i++)
                qy += (a[i] - midoutputs) * (a[i] - midoutputs);
            qy /= a.Length;
            qy = Math.Sqrt(qy);
            return qy;
        }

        private void Train_Click(object sender, RoutedEventArgs e)
        {
            List<RegCoef> OldTrainModel = TrainModel;
            if (TrainData.Rows.Count < 1)
                return;
            try
            {
                double[][] inputs = new double[TrainData.Rows.Count][];
                List<int> IndicesOfActive = new List<int>();
                for (int i = 1; i < NamesOfParameters.Count - 1; i++)
                    if (!NamesOfParameters[i].Hidden)
                        IndicesOfActive.Add(i);
                if (IndicesOfActive.Count < 1)
                    return;
                for (int i = 0; i < inputs.Length; i++)
                {
                    inputs[i] = new double[IndicesOfActive.Count];
                    for (int j = 0; j < inputs[i].Length; j++)
                    {
                        inputs[i][j] = double.Parse(TrainData.Rows[i][IndicesOfActive[j]].ToString(), CultureInfo.InvariantCulture);
                    }
                }
                double[] outputs = new double[inputs.Length];
                for (int i = 0; i < outputs.Length; i++)
                    outputs[i] = double.Parse(TrainData.Rows[i][TrainData.Columns.Count - 1].ToString(), CultureInfo.InvariantCulture);
                var ord = new OrdinaryLeastSquares() { UseIntercept = true };
                regression = ord.Learn(inputs, outputs);
                CoefficentOfDetermination.Text = $"{regression.CoefficientOfDetermination(inputs, outputs, adjust: true):F4}";
                TrainModel = new List<RegCoef>();
                for (int i = 0; i < IndicesOfActive.Count; i++)
                {
                    double[] x = new double[outputs.Length];
                    for (int j = 0; j < inputs.Length; j++)
                    {
                        x[j] = inputs[j][i];
                    }
                    double scoef = regression.Weights[i] * (StandardDeviation(x) / StandardDeviation(outputs));
                    var cur = new RegCoef();
                    cur.Name = NamesOfParameters[IndicesOfActive[i]].Name;
                    cur.StandCoef = $"{scoef:F4}";
                    cur.Coef = $"{regression.Weights[i]:F4}";
                    TrainModel.Add(cur);
                }
                SortedTrainModel = new List<RegCoef>(TrainModel);
                CoefGrid.ItemsSource = null;
                CoefGrid.ItemsSource = SortedTrainModel;
            }
            catch (FormatException)
            {
                MessageBox.Show("Данные должны представляться числами");
                TrainModel = OldTrainModel;
            }
            catch (Exception)
            {
                MessageBox.Show("Неизвестная ошибка");
                TrainModel = OldTrainModel;
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            string NameOfColumn = "";
            var cnw = new EnterColumnNameWindow();
            cnw.ShowDialog();
            if (cnw.DialogResult != true)
                return;
            NameOfColumn = cnw.ColumnNameBox.Text;
            string[] BufferAll = new string[TrainData.Rows.Count];
            NamesOfParameters.Insert(NamesOfParameters.Count - 1, new ParametrName(NameOfColumn));
            for (int i = 0; i < BufferAll.Length; i++)
                BufferAll[i] = TrainData.Rows[i][TrainData.Columns.Count - 1].ToString();
            TrainData.Columns.RemoveAt(TrainData.Columns.Count - 1);
            TrainData.Columns.Add(new DataColumn(TrainData.Columns.Count.ToString(), typeof(string)));
            TrainData.Columns.Add(new DataColumn(TrainData.Columns.Count.ToString(), typeof(string)));
            for (int i = 0; i < BufferAll.Length; i++)
            {
                TrainData.Rows[i][TrainData.Columns.Count - 1] = BufferAll[i];
            }
            Bind();
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            TrainData.Rows.Add(TrainData.NewRow());
            Bind();
        }

        private void RemoveColumn_Click(object sender, RoutedEventArgs e)
        {
            var CurCol = TrainGrid.CurrentColumn;
            int IndexInVisible = -1;
            for (int i = 0; i < TrainGrid.Columns.Count; i++)
            {
                if (ReferenceEquals(CurCol, TrainGrid.Columns[i]))
                {
                    IndexInVisible = i;
                    break;
                }
            }
            if (IndexInVisible == 0 || IndexInVisible == TrainGrid.Columns.Count - 1 || IndexInVisible == -1)
                return;
            int IndexInAll = -1;
            while (IndexInVisible >= 0)
            {
                IndexInAll++;
                if (!NamesOfParameters[IndexInAll].Hidden)
                    IndexInVisible--;
            }
            if (IndexInAll == -1)
                return;
            TrainData.Columns.RemoveAt(IndexInAll);
            NamesOfParameters.RemoveAt(IndexInAll);
            for (int i = 0; i < TrainData.Columns.Count; i++)
                TrainData.Columns[i].ColumnName = i.ToString();
            Bind();
        }

        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (TrainGrid.SelectedItems.Count <= 0)
                return;
            int[] indices = new int[TrainGrid.SelectedItems.Count];
            int i = 0;
            foreach (var cur in TrainGrid.SelectedItems)
            {
                indices[i] = TrainGrid.Items.IndexOf(cur);
                i++;
            }
            Array.Sort(indices);
            for (i = indices.Length - 1; i >= 0; i--)
                TrainData.Rows.RemoveAt(indices[i]);
            Bind();
        }

        private void HideRow_Click(object sender, RoutedEventArgs e)
        {
            var CurCol = TrainGrid.CurrentColumn;
            int IndexInVisible = -1;
            for (int i = 0; i < TrainGrid.Columns.Count; i++)
            {
                if (ReferenceEquals(CurCol, TrainGrid.Columns[i]))
                {
                    IndexInVisible = i;
                    break;
                }
            }
            if (IndexInVisible == 0 || IndexInVisible == TrainGrid.Columns.Count - 1 || IndexInVisible == -1)
                return;
            int IndexInAll = -1;
            while (IndexInVisible >= 0)
            {
                IndexInAll++;
                if (!NamesOfParameters[IndexInAll].Hidden)
                    IndexInVisible--;
            }
            if (IndexInAll == -1)
                return;
            NamesOfParameters[IndexInAll].Hidden = true;
            Bind();
        }

        private void ShowHidden_Click(object sender, RoutedEventArgs e)
        {
            var hcw = new HiddenColumnsWindow(NamesOfParameters);
            hcw.Owner = this;
            hcw.ShowDialog();
            if (hcw.DialogResult != true)
                return;
            int[] IndicesOfChosen = new int[hcw.HiddenColumns.SelectedItems.Count];
            int i = 0;
            foreach (var cur in hcw.HiddenColumns.SelectedItems)
            {
                IndicesOfChosen[i] = hcw.HiddenColumns.Items.IndexOf(cur);
                i++;
            }
            Array.Sort(IndicesOfChosen);
            int IndexInAll = 0, IndexOfCurHidden = -1;
            for (i = 0; i < IndicesOfChosen.Length;)
            {
                while (!NamesOfParameters[IndexInAll].Hidden)
                    IndexInAll++;
                IndexOfCurHidden++;
                if (IndexOfCurHidden == IndicesOfChosen[i])
                {
                    IndicesOfChosen[i] = IndexInAll;
                    i++;
                }
                IndexInAll++;
            }
            foreach (var cur in IndicesOfChosen)
                NamesOfParameters[cur].Hidden = false;
            Bind();
        }

        private void CalculateResult_Click(object sender, RoutedEventArgs e)
        {
            if (TrainModel.Count < 1)
                return;
            string[] Names = new string[TrainModel.Count];
            for (int i = 0; i < Names.Length; i++)
            {
                Names[i] = TrainModel[i].Name;
            }
            var epw = new EnterParametersWindow(Names);
            epw.ShowDialog();
            if (epw.DialogResult == true)
            {
                var inputs = new List<double>();
                foreach (string cur in epw.Table.Rows[0].ItemArray)
                    inputs.Add(double.Parse(cur, CultureInfo.InvariantCulture));
                //MessageBox.Show($"Предполагаемый доход: {regression.Transform(inputs.ToArray()):F2}");
                var PresentationWindow = new ResultPresentationWindow("Предполагаемый доход:", regression.Transform(inputs.ToArray()));
                PresentationWindow.ShowDialog();
                epw.Table?.Dispose();
            }
        }

        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            if (SortedTrainModel.Count < 1)
                return;
            if ((sender as MenuItem).Name == "StandInc")
            {
                SortedTrainModel.Sort((a, b) =>
                {
                    if (a.standCoef > b.standCoef)
                        return 1;
                    else if (a.standCoef < b.standCoef)
                        return -1;
                    else
                        return 0;
                });
            }
            else if ((sender as MenuItem).Name == "StandDec")
            {
                SortedTrainModel.Sort((a, b) =>
                {
                    if (a.standCoef < b.standCoef)
                        return 1;
                    else if (a.standCoef > b.standCoef)
                        return -1;
                    else
                        return 0;
                });
            }
            else if ((sender as MenuItem).Name == "UsualInc")
            {
                SortedTrainModel.Sort((a, b) =>
                {
                    if (a.coef > b.coef)
                        return 1;
                    else if (a.coef < b.coef)
                        return -1;
                    else
                        return 0;
                });
            }
            else
            {
                SortedTrainModel.Sort((a, b) =>
                {
                    if (a.coef < b.coef)
                        return 1;
                    else if (a.coef > b.coef)
                        return -1;
                    else
                        return 0;
                });
            }
            CoefGrid.ItemsSource = null;
            CoefGrid.ItemsSource = SortedTrainModel;
        }

        private void CalculateParameter_Click(object sender, RoutedEventArgs e)
        {
            var ListOfActive = new List<string>();
            for (int i = 0; i < TrainModel.Count; i++)
            {
                ListOfActive.Add(TrainModel[i].Name);
            }
            if (ListOfActive.Count < 1)
                return;
            var chpw = new ChooseParameterWindow(ListOfActive.ToArray());
            chpw.ShowDialog();
            if (chpw.DialogResult != true)
                return;
            int SelectedIndex = chpw.Names.SelectedIndex;
            ListOfActive.RemoveAt(SelectedIndex);
            ListOfActive.Add("Доход");
            var epw = new EnterParametersWindow(ListOfActive.ToArray());
            epw.ShowDialog();
            if (epw.DialogResult != true)
                return;
            var parameters = new List<double>();
            foreach (string cur in epw.Table.Rows[0].ItemArray)
                parameters.Add(double.Parse(cur, CultureInfo.InvariantCulture));
            double ValueOnLeftSide = parameters[parameters.Count - 1];
            parameters[parameters.Count - 1] = 1;
            var coefficents = regression.Weights.ToList();
            coefficents.Add(regression.Intercept);
            coefficents.RemoveAt(SelectedIndex);
            for (int i = 0; i < parameters.Count; i++)
                ValueOnLeftSide -= parameters[i] * coefficents[i];
            double res = ValueOnLeftSide / regression.Weights[SelectedIndex];
            //MessageBox.Show($"Предполагаемое необходимое значение параметра \"{TrainModel[SelectedIndex].Name}\": {res}");
            var PresentationWindow =
                new ResultPresentationWindow("Предполагаемое необходимое значение параметра \"{TrainModel[SelectedIndex].Name}\":", res);
            PresentationWindow.ShowDialog();
            epw.Table?.Dispose();
        }

        private string StringToCSVCell(string s)
        {
            string res = "";
            bool Quotes = false;
            foreach (var cur in s)
            {
                if (cur == '\n' || cur == ';' || cur == '\"')
                    Quotes = true;
                res += cur;
                if (cur == '\"')
                    res += cur;
            }
            if (Quotes)
                res = "\"" + res + "\"";
            return res;
        }

        private void SaveTrain_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "CSV tables (.csv)|*.csv";
            dlg.DefaultExt = ".csv";
            dlg.RestoreDirectory = true;
            dlg.CheckPathExists = true;
            if (dlg.ShowDialog() != true)
                return;
            FileStream stream = null;
            StreamWriter file = null;
            try
            {
                StringBuilder DataToSave = new StringBuilder();
                foreach (DataGridTextColumn cur in TrainGrid.Columns)
                {
                    DataToSave.Append(StringToCSVCell(cur.Header.ToString()) + ";");
                }
                DataToSave[DataToSave.Length - 1] = '\n';
                for (int i = 0; i < TrainData.Rows.Count; i++)
                {
                    for (int j = 0; j < TrainData.Columns.Count; j++)
                    {
                        DataToSave.Append(StringToCSVCell(TrainData.Rows[i][j].ToString()) + ";");
                    }
                    DataToSave[DataToSave.Length - 1] = '\n';
                }
                stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                file = new StreamWriter(stream, Encoding.Default);
                file.Write(DataToSave);
            }
            catch (Exception)
            {
                MessageBox.Show("Сохранение не удалось");
            }
            finally
            {
                file?.Dispose();
                stream?.Dispose();
            }
        }

        private void SaveLearn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "txt files (.txt)|*.txt";
            dlg.DefaultExt = ".txt";
            dlg.RestoreDirectory = true;
            dlg.CheckPathExists = true;
            if (dlg.ShowDialog() != true)
                return;
            if (regression == null)
                return;
            FileStream stream = null;
            StreamWriter file = null;
            try
            {
                stream = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                file = new StreamWriter(stream, Encoding.Default);
                foreach (var cur in TrainModel)
                {
                    file.WriteLine(cur.Name);
                    file.WriteLine(cur.coef);
                    file.WriteLine(cur.standCoef);
                }
                file.WriteLine("Intercept");
                file.WriteLine(regression.Intercept);
                file.WriteLine(regression.Intercept);
                file.WriteLine("CoefficentOfDetermination");
                file.WriteLine(CoefficentOfDetermination.Text);
                file.WriteLine(CoefficentOfDetermination.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Сохранение не удалось");
            }
            finally
            {
                file?.Dispose();
                stream?.Dispose();
            }
        }

        private void OpenLearn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "txt files (.txt)|*.txt";
            dlg.DefaultExt = ".csv";
            dlg.RestoreDirectory = true;
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            if (dlg.ShowDialog() != true)
                return;
            FileStream stream = null;
            StreamReader file = null;
            try
            {
                stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.None);
                file = new StreamReader(stream, Encoding.Default);
                List<RegCoef> Buffer = new List<RegCoef>();
                while (!file.EndOfStream)
                {
                    RegCoef cur = new RegCoef() { Name = file.ReadLine() };
                    cur.coef = double.Parse(file.ReadLine());
                    cur.standCoef = double.Parse(file.ReadLine());
                    Buffer.Add(cur);
                }
                if (Buffer.Count < 3)
                    throw new FormatException();
                if (Buffer[Buffer.Count - 2].Name != "Intercept")
                    throw new FormatException();
                regression = new MultipleLinearRegression(Buffer.Count - 2, Buffer[Buffer.Count - 2].coef);
                List<double> Weights = new List<double>();
                for (int i = 0; i < Buffer.Count - 2; i++)
                    Weights.Add(Buffer[i].coef);
                regression.Weights = Weights.ToArray();
                CoefficentOfDetermination.Text = $"{Buffer[Buffer.Count - 1].coef:F4}";
                Buffer.RemoveAt(Buffer.Count - 1);
                Buffer.RemoveAt(Buffer.Count - 1);
                TrainModel = Buffer;
                SortedTrainModel = new List<RegCoef>(TrainModel);
                CoefGrid.ItemsSource = null;
                CoefGrid.ItemsSource = SortedTrainModel;
            }
            catch (FormatException)
            {
                MessageBox.Show("В файле некорректные данные");
            }
            catch (Exception)
            {
                MessageBox.Show("Открыть файл не удалось");
            }
            finally
            {
                file?.Dispose();
                stream?.Dispose();
            }
        }

    }

}
