using SecureUstuj.Models;
using SecureUstuj.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SecureUstuj.Views
{
    public partial class AddEditWindow : Window
    {
        public AddEditWindow(string masterPassword)
        {
            InitializeComponent();
            DataContext = new AddEditViewModel(masterPassword);
        }

        public AddEditWindow(PasswordEntry entry, string masterPassword)
        {
            InitializeComponent();
            DataContext = new AddEditViewModel(entry, masterPassword);
        }
    }

}