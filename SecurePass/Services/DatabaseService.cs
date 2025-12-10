using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SecureUstuj.Models;

namespace SecureUstuj.Services
{
    public class DatabaseService : DbContext
    {
        private readonly EncryptionService _encryptionService;
        private readonly string _masterPassword;

        public DbSet<PasswordEntry> PasswordEntries { get; set; }

        public DatabaseService() : this("default") { }

        public DatabaseService(string masterPassword)
        {
            _masterPassword = masterPassword;
            _encryptionService = new EncryptionService(masterPassword);
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=SecureUstuj.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.EncryptedPassword).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        public async Task AddPasswordEntryAsync(PasswordEntry entry)
        {
            //entry.EncryptedPassword = _encryptionService.Encrypt(entry.EncryptedPassword);
            await PasswordEntries.AddAsync(entry);
            await SaveChangesAsync();
        }

        public async Task<List<PasswordEntry>> GetAllEntriesAsync()
        {
            return await PasswordEntries
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task<PasswordEntry?> GetEntryByIdAsync(int id)
        {
            return await PasswordEntries.FindAsync(id);
        }

        public async Task UpdateEntryAsync(PasswordEntry entry)
        {
            var existingEntry = await PasswordEntries.FindAsync(entry.Id);
            if (existingEntry != null)
            {
                existingEntry.Title = entry.Title;
                existingEntry.Username = entry.Username;
                existingEntry.EncryptedPassword = entry.EncryptedPassword; 
                existingEntry.Category = entry.Category;

                await SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException($"Entry with id {entry.Id} not found");
            }
        }

        public async Task DeleteEntryAsync(int id)
        {
            var entry = await GetEntryByIdAsync(id);
            if (entry != null)
            {
                PasswordEntries.Remove(entry);
                await SaveChangesAsync();
            }
        }

        public async Task<List<PasswordEntry>> SearchEntriesAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllEntriesAsync();

            string searchLower = searchText.ToLower();

            return await PasswordEntries
                .Where(e => e.Title.ToLower().Contains(searchLower) ||
                           e.Username.ToLower().Contains(searchLower) ||
                           e.Category.ToLower().Contains(searchLower))
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public async Task FixDoubleEncryptedPasswords()
        {
            var entries = await GetAllEntriesAsync();

            foreach (var entry in entries)
            {
                try
                {
                    string onceDecrypted = _encryptionService.Decrypt(entry.EncryptedPassword);
                    string twiceDecrypted = _encryptionService.Decrypt(onceDecrypted);

                    if (!string.IsNullOrEmpty(twiceDecrypted) && twiceDecrypted != "Decryption error")
                    {
                        entry.EncryptedPassword = _encryptionService.Encrypt(twiceDecrypted);
                        PasswordEntries.Update(entry);
                    }
                }
                catch
                {
                }
            }

            await SaveChangesAsync();
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            return await PasswordEntries
                .Select(e => e.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .ToListAsync();
        }

        public async Task InitializeTestDataAsync()
        {
            var entries = await GetAllEntriesAsync();
            if (entries.Count == 0)
            {
                var testEntries = new List<PasswordEntry>
                {
                    new PasswordEntry
                    {
                        Title = "Yandex",
                        Username = "user@yandex.ru",
                        EncryptedPassword = "encrypted_password_1",
                        Category = "Email",
                        CreatedDate = DateTime.Now.AddDays(-10)
                    },
                    new PasswordEntry
                    {
                        Title = "VKontakte",
                        Username = "my_login",
                        EncryptedPassword = "encrypted_password_2",
                        Category = "Social",
                        CreatedDate = DateTime.Now.AddDays(-5)
                    },
                    new PasswordEntry
                    {
                        Title = "Steam",
                        Username = "gamer123",
                        EncryptedPassword = "encrypted_password_3",
                        Category = "Games",
                        CreatedDate = DateTime.Now.AddDays(-2)
                    }
                };

                foreach (var entry in testEntries)
                {
                    await AddPasswordEntryAsync(entry);
                }
            }
        }

        public async Task<int> GetEntriesCountAsync()
        {
            return await PasswordEntries.CountAsync();
        }

        public async Task ExportToCsvAsync(string filePath)
        {
            var entries = await GetAllEntriesAsync();
            var csv = new StringBuilder();

            csv.AppendLine("Id;Title;Username;Category;CreatedDate");

            foreach (var entry in entries)
            {
                csv.AppendLine($"{entry.Id};{entry.Title};{entry.Username};{entry.Category};{entry.CreatedDate}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        }
    }
}