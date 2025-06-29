using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {

        private Dictionary<string, LineSeries> dataSeries = new Dictionary<string, LineSeries>(); /* Dictionary for storing data */
        private PlotModel plotModel; /* Graph to plot */
        public MainWindow()
        {
            InitializeComponent();
            plotModel = new PlotModel { Title = "Stock Data" };
            plotView.Model = plotModel;
            plotView.Visibility = Visibility.Collapsed;
            ValueSlider.ValueChanged += ValueSlider_ValueChanged;

        }
        private void ImportData_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select data file"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);
                ReadCsvFileAndPlot(filePath, fileName);
            }
        }

        private void ShowPrediction_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxContainer.Children.OfType<CheckBox>().Any(cb => cb.IsChecked == true))
            {
                MessageBox.Show("Select a file first!");
                return;
            }

            int dias = (int)ValueSlider.Value;

            string archivoCSV = CheckBoxContainer.Children.OfType<CheckBox>()
                                .FirstOrDefault(cb => cb.IsChecked == true)?.Content.ToString();

            if (string.IsNullOrEmpty(archivoCSV))
            {
                MessageBox.Show("Select a file first!");
                return;
            }

            OutputWindow outputWindow = new OutputWindow(dias, archivoCSV);
            outputWindow.Show();
        }
        private void AddCheckBox(string fileName) /* Add checkbox to the checkbox container for the new data graph */
        {
            CheckBox checkBox = new CheckBox /* New checkbox unmarked */
            {
                Content = fileName,
                IsChecked = false,
                Margin = new Thickness(0, 5, 0, 0)
            };

            checkBox.Checked += CheckBox_Checked; /* Shows the graph when checked and unchecks rest of the checkboxes*/
            checkBox.Unchecked += (s, e) => UpdatePlot(); /* Hides the graph when unchecked*/

            CheckBoxContainer.Children.Add(checkBox);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) /* Checkbox functionality when it is checked */
        {
            if (sender is CheckBox selectedCheckBox)
            {
                foreach (var checkBox in CheckBoxContainer.Children.OfType<CheckBox>()) /* Unchecks all the other checkboxes */
                {
                    if (checkBox != selectedCheckBox)
                    {
                        checkBox.IsChecked = false;
                    }
                }
                UpdatePlot(); /* Updates with the data graph of that checkbox */
                ValueSlider.Visibility = Visibility.Visible;
                SliderValueText.Visibility = Visibility.Visible;
                ShowPredictionButton.Visibility = Visibility.Visible;
            }
        }
        private void UpdatePlot() /* Loads and updates the graph selected*/
        {
            plotModel.Series.Clear();

            bool hasCheckedSeries = false;

            foreach (var checkBox in CheckBoxContainer.Children.OfType<CheckBox>()) /* Searchs which checkbox is checked and if its data is stored*/
            {
                if (checkBox.IsChecked == true && dataSeries.ContainsKey(checkBox.Content.ToString()))
                {
                    plotModel.Series.Add(dataSeries[checkBox.Content.ToString()]);
                    hasCheckedSeries = true;
                }
            }

            plotView.Model = plotModel;
            plotView.InvalidatePlot(true);

            plotView.Visibility = hasCheckedSeries ? Visibility.Visible : Visibility.Collapsed; /* Shows or hides the graph whether which checkbox is checked or not */
        }
        private void ValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int sliderValue = (int)e.NewValue;
            SliderValueText.Text = $"Time prediction: {sliderValue} d";
        }


        private void ReadCsvFileAndPlot(string filePath, string fileName)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                char separator = lines[0].Contains("\t") ? '\t' : (lines[0].Contains(";") ? ';' : ',');

                var data = lines.Skip(1)
                    .Select(line => line.Split(separator))
                    .Where(columns => columns.Length >= 5)
                    .Select(columns => new
                    {
                        Date = DateTime.TryParse(columns[0].Split(' ')[0], out DateTime date) ? date : DateTime.MinValue,
                        Close = double.TryParse(columns[4].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double close) ? close : double.NaN
                    })
                    .Where(entry => entry.Date != DateTime.MinValue && !double.IsNaN(entry.Close))
                    .ToList();

                if (!data.Any())
                {
                    MessageBox.Show("CSV file doesn't contain valid data");
                    return;
                }

                var series = new LineSeries
                {
                    Title = fileName,
                    StrokeThickness = 2,
                    Color = OxyColors.Blue
                };

                foreach (var point in data)
                {
                    double xValue = DateTimeAxis.ToDouble(point.Date);
                    series.Points.Add(new DataPoint(xValue, point.Close));
                }

                plotModel.Axes.Clear();
                plotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "yyyy",
                    Title = "Year",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    IntervalType = DateTimeIntervalType.Years
                });

                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Close",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });

                dataSeries[fileName] = series;
                AddCheckBox(fileName);
                plotModel.Title = fileName;
                UpdatePlot();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing CSV data file " + ex.Message);
            }
        }

        private void ImportView_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Select prediction file"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);

                try
                {
                    var lines = File.ReadAllLines(filePath);
                    var dataLines = lines.TakeWhile(line => !line.StartsWith("Comments:"));
                    var comments = lines.SkipWhile(line => !line.StartsWith("Comments:"))
                                        .Skip(1)
                                        .Aggregate((a, b) => a + "\n" + b);

                    var series = new LineSeries { Title = fileName, StrokeThickness = 2, Color = OxyColors.Blue };

                    foreach (var line in dataLines.Skip(1))
                    {
                        var parts = line.Split(',');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int day) && int.TryParse(parts[1], out int price))
                        {
                            series.Points.Add(new DataPoint(day > 1 ? day - 1 : 1, price)); // Corregir el desfase, pero mantener el día 1 como mínimo
                        }
                    }

                    PlotModel newModel = new PlotModel { Title = fileName };
                    newModel.Series.Add(series);
                    newModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Day" });
                    newModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Price" });

                    ViewWindow viewWindow = new ViewWindow(newModel, comments);
                    viewWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing CSV file: " + ex.Message);
                }
            }
        }
    }
}