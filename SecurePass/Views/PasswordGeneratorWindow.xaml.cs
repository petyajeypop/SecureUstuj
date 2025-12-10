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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating password: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
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