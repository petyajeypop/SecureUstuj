using SecureUstuj.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SecureUstuj.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(string masterPassword)
        {
            InitializeComponent();
            DataContext = new MainViewModel(masterPassword);
        }

        private void EntryItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ShowEntryDetailsCommand.Execute(null);
            }
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}