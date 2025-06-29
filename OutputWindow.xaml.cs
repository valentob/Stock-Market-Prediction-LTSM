using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class OutputWindow : Window
    {
        private int predictionDays;
        private string csvFilePath;
        private string savedComment = string.Empty;

        public OutputWindow(int days, string csvFile)
        {
            InitializeComponent();
            predictionDays = days;
            csvFilePath = csvFile;

            LoadPredictionChart();
        }

        private void LoadPredictionChart()
        {
            try
            {
                var model = new PlotModel { Title = "Stock Prediction" };
                var lineSeries = new LineSeries { Title = "Predicted Data", Color = OxyColors.Green };

                Random rand = new Random();
                for (int i = 0; i < predictionDays; i++)
                {
                    lineSeries.Points.Add(new DataPoint(i, rand.Next(100, 200)));
                }

                model.Series.Add(lineSeries);

                model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Days" });
                model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Price", Minimum = 0 });

                plotView.Model = model;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading prediction: " + ex.Message);
            }
        }

        private void AddComment_Click(object sender, RoutedEventArgs e)
        {
            string comment = CommentsBox.Text.Trim();
            if (!string.IsNullOrEmpty(comment))
            {
                savedComment = comment;
                MessageBox.Show($"Comment saved: {comment}", "Comment", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Write a comment first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV File|*.csv",
                    Title = "Save Prediction Data"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string exportPath = saveFileDialog.FileName;
                    using (StreamWriter writer = new StreamWriter(exportPath))
                    {
                        writer.WriteLine("Day,Predicted Price");

                        var lineSeries = plotView.Model.Series[0] as LineSeries;
                        if (lineSeries != null)
                        {
                            for (int i = 0; i < predictionDays; i++)
                            {
                                writer.WriteLine($"{i + 1},{lineSeries.Points[i].Y}");
                            }
                        }

                        writer.WriteLine();
                        writer.WriteLine("Comments:");
                        writer.WriteLine(string.IsNullOrWhiteSpace(savedComment) ? "No comments added." : savedComment);
                    }

                    MessageBox.Show($"Results exported to: {exportPath}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to export data: " + ex.Message);
            }
        }

        private void CommentsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            savedComment = CommentsBox.Text.Trim();
        }
    }
}