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
            Console.WriteLine($"=== AddEditViewModel constructor for editing ===");

            if (entryToEdit == null)
            {
                Console.WriteLine("ERROR: entryToEdit is null!");
                throw new ArgumentNullException(nameof(entryToEdit));
            }

            Console.WriteLine($"Entry ID: {entryToEdit.Id}, Title: {entryToEdit.Title}");

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

            Title = entryToEdit.Title ?? string.Empty;
            Username = entryToEdit.Username ?? string.Empty;
            SelectedCategory = entryToEdit.Category ?? string.Empty;

            Console.WriteLine($"Setting - Title: '{Title}', Username: '{Username}', Category: '{SelectedCategory}'");

            try
            {
                var encryptionService = new EncryptionService(masterPassword);
                Password = encryptionService.Decrypt(entryToEdit.EncryptedPassword ?? string.Empty);
                Console.WriteLine($"Password decrypted, length: {Password.Length}");
            }
            catch (Exception ex)
            {
                Password = string.Empty;
                Console.WriteLine($"Decryption error: {ex.Message}");
            }

            LoadCategoriesFromDatabase();
            Console.WriteLine($"=== AddEditViewModel initialized ===");
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
                Console.WriteLine($"=== Save method called ===");
                Console.WriteLine($"IsEditMode: {_isEditMode}");

                var encryptionService = new EncryptionService(_masterPassword);
                string encryptedPassword = encryptionService.Encrypt(Password);
                Console.WriteLine($"Password encrypted, length: {encryptedPassword.Length}");

                if (_isEditMode && _editingEntry != null)
                {
                    Console.WriteLine($"Editing entry ID: {_editingEntry.Id}");

                    _editingEntry.Title = Title.Trim();
                    _editingEntry.Username = Username.Trim();
                    _editingEntry.EncryptedPassword = encryptedPassword;
                    _editingEntry.Category = SelectedCategory;

                    Console.WriteLine($"Sending to database - Title: {_editingEntry.Title}, Username: {_editingEntry.Username}, Category: {_editingEntry.Category}");

                    await _dbService.UpdateEntryAsync(_editingEntry);
                }
                else
                {
                    Console.WriteLine($"Creating new entry");

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

                Console.WriteLine($"=== Save completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Save ERROR: {ex.Message} ===");
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