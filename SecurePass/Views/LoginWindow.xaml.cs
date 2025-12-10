using SecureUstuj.Services;
using SecureUstuj.Views;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
using System.IO;

namespace SecureUstuj.Views
{
    public partial class LoginWindow : Window
    {
        private bool _isFirstLaunch = true;
        private bool _isLoggingIn = false;
        private static string _masterPassword = "";

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
            PasswordBox.KeyDown += PasswordBox_KeyDown;

            // Убедитесь, что обработчик привязан к кнопке в конструкторе
            var loginButton = Template.FindName("LoginButton", this) as Button;
            if (loginButton != null)
            {
                loginButton.Click += LoginButton_Click;
            }
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isFirstLaunch = !File.Exists("master.pwd");
            PasswordBox.Focus();
        }

        private void SavePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                File.WriteAllBytes("master.pwd", hash);
            }
        }

        private bool VerifyPassword(string password)
        {
            if (!File.Exists("master.pwd")) return false;

            using (var sha256 = SHA256.Create())
            {
                byte[] inputHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] savedHash = File.ReadAllBytes("master.pwd");

                if (inputHash.Length != savedHash.Length) return false;

                for (int i = 0; i < inputHash.Length; i++)
                {
                    if (inputHash[i] != savedHash[i]) return false;
                }
                return true;
            }
        }

        private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isLoggingIn)
            {
                await AttemptLoginAsync();
            }
        }

        // Этот метод уже есть, но убедимся, что он правильный
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoggingIn)
            {
                await AttemptLoginAsync();
            }
        }

        // Если у вас есть кнопка "Exit", добавьте этот метод
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task AttemptLoginAsync()
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter master password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _isLoggingIn = false;
                return;
            }

            // Найдем кнопку LoginButton и отключим ее
            var loginButton = Template.FindName("LoginButton", this) as Button;
            if (loginButton != null)
            {
                loginButton.IsEnabled = false;
            }
            else
            {
                // Если не нашли через Template, попробуем напрямую
                if (LoginButton != null)
                {
                    LoginButton.IsEnabled = false;
                }
            }

            try
            {
                if (_isFirstLaunch)
                {
                    SavePasswordHash(password);
                    _masterPassword = password;

                    MessageBox.Show("Master password set!\nRemember it - you won't be able to recover access without it.",
                                  "First Launch",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);

                    // Используем статический метод вместо создания экземпляра DatabaseService
                    await DatabaseService.InitializeTestDataAsync(_masterPassword);

                    var mainWindow = new MainWindow(_masterPassword);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    if (VerifyPassword(password))
                    {
                        _masterPassword = password;

                        var mainWindow = new MainWindow(_masterPassword);
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Incorrect master password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        PasswordBox.Password = "";
                        PasswordBox.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Включаем кнопку обратно
                if (loginButton != null)
                {
                    loginButton.IsEnabled = true;
                }
                else if (LoginButton != null)
                {
                    LoginButton.IsEnabled = true;
                }
                _isLoggingIn = false;
            }
        }
    }
}