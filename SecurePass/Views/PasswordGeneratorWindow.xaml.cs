using SecureUstuj.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SecureUstuj.Views
{
    public partial class PasswordGeneratorWindow : Window
    {
        public string GeneratedPassword { get; private set; } = string.Empty;

        public PasswordGeneratorWindow()
        {
            InitializeComponent();

            GenerateButton.Click += GenerateButton_Click;
            CopyButton.Click += CopyButton_Click;
            CancelButton.Click += CancelButton_Click;
            UseButton.Click += UseButton_Click;

            LengthTextBox.TextChanged += (s, e) => GeneratePassword();
            UppercaseCheckBox.Checked += (s, e) => GeneratePassword();
            UppercaseCheckBox.Unchecked += (s, e) => GeneratePassword();
            DigitsCheckBox.Checked += (s, e) => GeneratePassword();
            DigitsCheckBox.Unchecked += (s, e) => GeneratePassword();
            SpecialCheckBox.Checked += (s, e) => GeneratePassword();
            SpecialCheckBox.Unchecked += (s, e) => GeneratePassword();

            GeneratePassword();
        }

        public PasswordGeneratorWindow(string currentPassword) : this()
        {
            if (!string.IsNullOrEmpty(currentPassword))
            {
                GeneratedPasswordBox.Text = currentPassword;
                GeneratedPassword = currentPassword;
                UpdateStrengthIndicator(currentPassword);
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            GeneratePassword();
        }

        private void GeneratePassword()
        {
            try
            {
                if (!int.TryParse(LengthTextBox.Text, out int length))
                {
                    length = 12;
                    LengthTextBox.Text = "12";
                }

                if (length < 8) length = 8;
                if (length > 32) length = 32;

                bool useUpper = UppercaseCheckBox.IsChecked ?? true;
                bool useDigits = DigitsCheckBox.IsChecked ?? true;
                bool useSpecial = SpecialCheckBox.IsChecked ?? true;

                var generator = new PasswordGenerator();
                GeneratedPassword = generator.GeneratePassword(length, useUpper, useDigits, useSpecial);

                GeneratedPasswordBox.Text = GeneratedPassword;
                UpdateStrengthIndicator(GeneratedPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating password: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void UpdateStrengthIndicator(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                StrengthIndicator.Width = 0;
                StrengthText.Text = "Empty";
                StrengthText.Foreground = Brushes.Gray;
                return;
            }

            int score = 0;

            if (password.Length >= 12) score += 2;
            else if (password.Length >= 8) score += 1;

            if (Regex.IsMatch(password, "[A-Z]")) score += 1;
            if (Regex.IsMatch(password, "[0-9]")) score += 1;
            if (Regex.IsMatch(password, "[!@#$%^&*()]")) score += 1;

            double percent = (score / 6.0) * 100;
            StrengthIndicator.Width = (percent / 100) * 300;

            if (score >= 5)
            {
                StrengthText.Text = "Strong";
                StrengthText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                StrengthIndicator.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(76, 175, 80));
            }
            else if (score >= 3)
            {
                StrengthText.Text = "Medium";
                StrengthText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 193, 7)); // Yellow
                StrengthIndicator.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 193, 7));
            }
            else
            {
                StrengthText.Text = "Weak";
                StrengthText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                StrengthIndicator.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(244, 67, 54));
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedPasswordBox.Text))
            {
                Clipboard.SetText(GeneratedPasswordBox.Text);
                MessageBox.Show("Password copied to clipboard!",
                              "Success",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedPasswordBox.Text))
            {
                GeneratedPassword = GeneratedPasswordBox.Text;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please generate a password first",
                              "Warning",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }
    }
}