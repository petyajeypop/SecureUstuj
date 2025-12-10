using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureUstuj.Models;
using SecureUstuj.Services;
using SecureUstuj.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SecureUstuj.ViewModels
{
    public partial class AddEditViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;
        private readonly PasswordEntry? _editingEntry;
        private readonly bool _isEditMode;
        private readonly string _masterPassword;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        public ObservableCollection<string> Categories { get; }

        public AddEditViewModel(string masterPassword)
        {
            _masterPassword = masterPassword;
            _dbService = new DatabaseService(masterPassword);
            _isEditMode = false;

            Categories = new ObservableCollection<string>
            {
                "Email",
                "Social",
                "Games",
                "Work",
                "Bank",
                "Other"
            };

            LoadCategoriesFromDatabase();
        }

        public AddEditViewModel(PasswordEntry entryToEdit, string masterPassword)
        {


            if (entryToEdit == null)
                throw new ArgumentNullException(nameof(entryToEdit));

            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
        

            _masterPassword = masterPassword;
            _dbService = new DatabaseService(masterPassword);
            _editingEntry = entryToEdit;
            _isEditMode = true;

            Categories = new ObservableCollection<string>
            {
                "Email",
                "Social",
                "Games",
                "Work",
                "Bank",
                "Other"
            };

            LoadCategoriesFromDatabase();

            Title = entryToEdit.Title;
            Username = entryToEdit.Username;

            try
            {
                var encryptionService = new EncryptionService(masterPassword);
                Password = encryptionService.Decrypt(entryToEdit.EncryptedPassword ?? string.Empty);
            }
            catch (Exception ex)
            {
                Password = string.Empty;
                Console.WriteLine($"Decryption error: {ex.Message}");
            }

            SelectedCategory = entryToEdit.Category ?? string.Empty;
            Console.WriteLine($"Setting SelectedCategory to: '{SelectedCategory}'");
        }

        private async void LoadCategoriesFromDatabase()
        {
            try
            {
                var dbCategories = await _dbService.GetCategoriesAsync();
                Console.WriteLine($"Loaded {dbCategories.Count} categories from database");

                foreach (var category in dbCategories)
                {
                    if (!Categories.Contains(category) && !string.IsNullOrEmpty(category))
                    {
                        Categories.Add(category);
                        Console.WriteLine($"Added category: {category}");
                    }
                }

                Console.WriteLine($"Total categories: {Categories.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        [RelayCommand]
        private void GeneratePassword()
        {
            var generator = new PasswordGenerator();
            Password = generator.GeneratePassword(12, true, true, true);
        }



        [RelayCommand]
        private void OpenAdvancedGenerator()
        {
            var generatorWindow = new PasswordGeneratorWindow(Password);
            generatorWindow.Owner = Application.Current.MainWindow;

            if (generatorWindow.ShowDialog() == true)
            {
                Password = generatorWindow.GeneratedPassword;
            }
        }

        [RelayCommand]
        private void CopyPassword()
        {
            if (string.IsNullOrEmpty(Password))
            {
                ShowError("No password to copy");
                return;
            }

            try
            {
                Clipboard.SetText(Password);
                MessageBox.Show("Password copied to clipboard", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError($"Error copying: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!ValidateInput())
                return;

            try
            {
                var encryptionService = new EncryptionService(_masterPassword);
                string encryptedPassword = encryptionService.Encrypt(Password);

                if (_isEditMode && _editingEntry != null)
                {
                    var updatedEntry = new PasswordEntry
                    {
                        Id = _editingEntry.Id,
                        Title = Title.Trim(),
                        Username = Username.Trim(),
                        EncryptedPassword = encryptedPassword,
                        Category = SelectedCategory,
                        CreatedDate = _editingEntry.CreatedDate
                    };

                    await _dbService.UpdateEntryAsync(updatedEntry);
                }
                else
                {
                    var newEntry = new PasswordEntry
                    {
                        Title = Title.Trim(),
                        Username = Username.Trim(),
                        EncryptedPassword = encryptedPassword,
                        Category = SelectedCategory,
                        CreatedDate = DateTime.Now
                    };

                    await _dbService.AddPasswordEntryAsync(newEntry);
                }

                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Save error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        private bool ValidateInput()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(Title))
            {
                ShowError("Title is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowError("Username is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Password is required");
                return false;
            }

            if (Password.Length < 4)
            {
                ShowError("Password must be at least 4 characters");
                return false;
            }

            return true;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}