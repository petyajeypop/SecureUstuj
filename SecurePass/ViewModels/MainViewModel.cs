using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SecureUstuj.Models;
using SecureUstuj.Services;
using SecureUstuj.Views;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System;

namespace SecureUstuj.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly string _masterPassword;

        [ObservableProperty]
        private ObservableCollection<PasswordEntry> _entries;

        [ObservableProperty]
        private PasswordEntry? _selectedEntry;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "All Categories";

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _showPasswordGenerator = false;

        [ObservableProperty]
        private string _generatedPassword = string.Empty;

        [ObservableProperty]
        private int _passwordLength = 12;

        [ObservableProperty]
        private bool _useUppercase = true;

        [ObservableProperty]
        private bool _useDigits = true;

        [ObservableProperty]
        private bool _useSpecial = true;

        public ObservableCollection<string> Categories { get; }

        public MainViewModel(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
            {
                throw new ArgumentException("Master password cannot be null or empty", nameof(masterPassword));
            }

            _masterPassword = masterPassword;
            _entries = new ObservableCollection<PasswordEntry>();

            Categories = new ObservableCollection<string>
            {
                "All Categories",
                "Email",
                "Social",
                "Games",
                "Work",
                "Bank",
                "Other"
            };

            // Инициализируем данные в правильном потоке
            InitializeData();

            GeneratePasswordCommand.Execute(null);
        }

        private async void InitializeData()
        {
            try
            {
                // Исправляем двойное шифрование
                await DatabaseService.FixDoubleEncryptedPasswords(_masterPassword);

                // Инициализируем тестовые данные если нужно
                await DatabaseService.InitializeTestDataAsync(_masterPassword);

                // Загружаем записи
                await LoadEntries();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during initialization: {ex.Message}");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        [RelayCommand]
        private async Task LoadEntries()
        {
            try
            {
                Console.WriteLine($"=== LoadEntries called ===");

                var allEntries = await DatabaseService.GetAllEntriesAsync(_masterPassword);
                Console.WriteLine($"Total entries in DB: {allEntries.Count}");

                var filteredEntries = allEntries.AsEnumerable();

                if (SelectedCategory != "All Categories" && !string.IsNullOrEmpty(SelectedCategory))
                {
                    filteredEntries = filteredEntries.Where(e => e.Category == SelectedCategory);
                    Console.WriteLine($"Filtered by category '{SelectedCategory}', count: {filteredEntries.Count()}");
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filteredEntries = filteredEntries.Where(e =>
                        e.Title.ToLower().Contains(searchLower) ||
                        e.Username.ToLower().Contains(searchLower) ||
                        (e.Category != null && e.Category.ToLower().Contains(searchLower)));
                    Console.WriteLine($"Filtered by search '{SearchText}', count: {filteredEntries.Count()}");
                }

                // Обновляем коллекцию через Dispatcher
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Entries.Clear();
                    foreach (var entry in filteredEntries.OrderByDescending(e => e.CreatedDate))
                    {
                        Entries.Add(entry);
                    }

                    StatusMessage = $"Loaded: {Entries.Count} records";
                });

                Console.WriteLine($"Entries in collection: {Entries.Count}");
                Console.WriteLine($"=== LoadEntries completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== LoadEntries ERROR: {ex.Message} ===");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error loading entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Error loading entries";
                });
            }
        }

        [RelayCommand]
        private void AddEntry()
        {
            try
            {
                var addWindow = new AddEditWindow(_masterPassword);
                addWindow.Owner = Application.Current.MainWindow;

                if (addWindow.ShowDialog() == true)
                {
                    LoadEntriesCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening add window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void EditEntry()
        {
            if (SelectedEntry == null)
            {
                MessageBox.Show("Please select an entry to edit", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string entryTitle = SelectedEntry.Title;

                var editWindow = new AddEditWindow(SelectedEntry, _masterPassword);
                editWindow.Owner = Application.Current.MainWindow;

                if (editWindow.ShowDialog() == true)
                {
                    LoadEntriesCommand.Execute(null);
                    StatusMessage = $"Updated: {entryTitle}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SearchEntries()
        {
            await LoadEntries();
        }

        [RelayCommand]
        private async Task FilterByCategory()
        {
            await LoadEntries();
        }

        [RelayCommand]
        private void TogglePasswordGenerator()
        {
            ShowPasswordGenerator = !ShowPasswordGenerator;
        }

        [RelayCommand]
        private void GeneratePassword()
        {
            try
            {
                var generator = new PasswordGenerator();
                GeneratedPassword = generator.GeneratePassword(PasswordLength, UseUppercase, UseDigits, UseSpecial);
                StatusMessage = "Password generated";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                GeneratedPassword = "Error";
            }
        }

        [RelayCommand]
        private void CopyGeneratedPassword()
        {
            if (string.IsNullOrEmpty(GeneratedPassword))
            {
                MessageBox.Show("No password to copy", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(GeneratedPassword);
                StatusMessage = "Password copied to clipboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void UseGeneratedPassword()
        {
            if (string.IsNullOrEmpty(GeneratedPassword))
            {
                MessageBox.Show("Generate a password first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var addWindow = new AddEditWindow(_masterPassword);

                if (addWindow.DataContext is AddEditViewModel viewModel)
                {
                    viewModel.Password = GeneratedPassword;
                }

                addWindow.Owner = Application.Current.MainWindow;

                if (addWindow.ShowDialog() == true)
                {
                    LoadEntriesCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening add window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteEntry()
        {
            if (SelectedEntry == null)
            {
                MessageBox.Show("Please select an entry to delete", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int entryId = SelectedEntry.Id;
            string entryTitle = SelectedEntry.Title;

            var result = MessageBox.Show($"Delete '{entryTitle}'?\nThis action cannot be undone.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await DatabaseService.DeleteEntryAsync(_masterPassword, entryId);
                    await LoadEntries();

                    StatusMessage = $"Deleted: {entryTitle}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting entry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void CopyLogin()
        {
            if (SelectedEntry == null)
            {
                MessageBox.Show("Select an entry first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(SelectedEntry.Username);
                StatusMessage = $"Login copied: {SelectedEntry.Title}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void CopyPassword()
        {
            if (SelectedEntry == null)
            {
                MessageBox.Show("Select an entry first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var encryptionService = new EncryptionService(_masterPassword);
                string decryptedPassword = encryptionService.Decrypt(SelectedEntry.EncryptedPassword);

                Clipboard.SetText(decryptedPassword);
                StatusMessage = $"Password copied: {SelectedEntry.Title}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowEntryDetails()
        {
            if (SelectedEntry == null)
            {
                MessageBox.Show("Select an entry first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var encryptionService = new EncryptionService(_masterPassword);
                string decryptedPassword = encryptionService.Decrypt(SelectedEntry.EncryptedPassword);

                var details = $"Title: {SelectedEntry.Title}\n" +
                             $"Username: {SelectedEntry.Username}\n" +
                             $"Password: {decryptedPassword}\n" +
                             $"Category: {SelectedEntry.Category}\n" +
                             $"Created: {SelectedEntry.CreatedDate:dd.MM.yyyy HH:mm}";

                MessageBox.Show(details, "Entry Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ExportToCsv()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    FileName = $"passwords_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Получаем все записи из базы данных
                    var allEntries = await DatabaseService.GetAllEntriesAsync(_masterPassword);

                    // Применяем фильтры как в LoadEntries
                    var filteredEntries = allEntries.AsEnumerable();

                    if (SelectedCategory != "All Categories" && !string.IsNullOrEmpty(SelectedCategory))
                    {
                        filteredEntries = filteredEntries.Where(e => e.Category == SelectedCategory);
                    }

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var searchLower = SearchText.ToLower();
                        filteredEntries = filteredEntries.Where(e =>
                            e.Title.ToLower().Contains(searchLower) ||
                            e.Username.ToLower().Contains(searchLower) ||
                            (e.Category != null && e.Category.ToLower().Contains(searchLower)));
                    }

                    var entries = filteredEntries.OrderByDescending(e => e.CreatedDate).ToList();

                    var csv = new StringBuilder();
                    csv.AppendLine("Title;Username;Password;Category;CreatedDate");

                    var encryptionService = new EncryptionService(_masterPassword);
                    foreach (var entry in entries)
                    {
                        string decryptedPassword = encryptionService.Decrypt(entry.EncryptedPassword);
                        // Экранируем кавычки в полях
                        string escapedTitle = entry.Title?.Replace("\"", "\"\"") ?? "";
                        string escapedUsername = entry.Username?.Replace("\"", "\"\"") ?? "";
                        string escapedPassword = decryptedPassword?.Replace("\"", "\"\"") ?? "";
                        string escapedCategory = entry.Category?.Replace("\"", "\"\"") ?? "";

                        csv.AppendLine($"\"{escapedTitle}\";\"{escapedUsername}\";\"{escapedPassword}\";\"{escapedCategory}\";\"{entry.CreatedDate:yyyy-MM-dd HH:mm:ss}\"");
                    }

                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString(), Encoding.UTF8);

                    StatusMessage = $"Exported {entries.Count} entries to CSV";
                    MessageBox.Show($"Successfully exported {entries.Count} entries to:\n{saveDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Export failed";
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadEntries();
        }

        [RelayCommand]
        private async Task ClearSearch()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SearchText = string.Empty;
                SelectedCategory = "All Categories";
            });
            await LoadEntries();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            try
            {
                // Здесь можно добавить окно настроек
                MessageBox.Show("Settings feature coming soon!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadCategories()
        {
            try
            {
                var dbCategories = await DatabaseService.GetCategoriesAsync(_masterPassword);

                // Обновляем через Dispatcher
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Добавляем новые категории из базы данных
                    foreach (var category in dbCategories)
                    {
                        if (!Categories.Contains(category) && !string.IsNullOrEmpty(category))
                        {
                            Categories.Add(category);
                        }
                    }

                    StatusMessage = $"Categories loaded: {dbCategories.Count}";
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task GetStatistics()
        {
            try
            {
                var count = await DatabaseService.GetEntriesCountAsync(_masterPassword);
                var categories = await DatabaseService.GetCategoriesAsync(_masterPassword);

                var stats = $"Total entries: {count}\n" +
                           $"Categories: {categories.Count}\n" +
                           $"Last updated: {DateTime.Now:HH:mm:ss}";

                MessageBox.Show(stats, "Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting statistics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}