using System;
using System.Collections.Generic;
using System.Text;

namespace SecureUstuj.Models
{
    public class PasswordEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}