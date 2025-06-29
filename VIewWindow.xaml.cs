using System.Windows;
using OxyPlot;
using OxyPlot.Wpf;

namespace WpfApp1
{
    public partial class ViewWindow : Window
    {
        public ViewWindow(PlotModel plotModel, string comments)
        {
            InitializeComponent();
            DataPlot.Model = plotModel;
            CommentsText.Text = comments;
        }
    }
}