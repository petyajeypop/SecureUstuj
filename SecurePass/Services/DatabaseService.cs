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
        private readonly string _masterPassword;

        public DbSet<PasswordEntry> PasswordEntries { get; set; }

        public DatabaseService(string masterPassword)
        {
            _masterPassword = masterPassword;
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

        public static async Task AddPasswordEntryAsync(string masterPassword, PasswordEntry entry)
        {
            using var db = new DatabaseService(masterPassword);
            await db.PasswordEntries.AddAsync(entry);
            await db.SaveChangesAsync();
        }

        public static async Task<List<PasswordEntry>> GetAllEntriesAsync(string masterPassword)
        {
            using var db = new DatabaseService(masterPassword);
            return await db.PasswordEntries
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public static async Task<PasswordEntry?> GetEntryByIdAsync(string masterPassword, int id)
        {
            using var db = new DatabaseService(masterPassword);
            return await db.PasswordEntries.FindAsync(id);
        }

        public static async Task UpdateEntryAsync(string masterPassword, PasswordEntry entry)
        {
            using var db = new DatabaseService(masterPassword);
            try
            {
                Console.WriteLine($"=== UpdateEntryAsync called for ID: {entry.Id} ===");

                var existingEntry = await db.PasswordEntries.FindAsync(entry.Id);
                if (existingEntry == null)
                {
                    Console.WriteLine($"ERROR: Entry with ID {entry.Id} not found!");
                    throw new Exception($"Entry with ID {entry.Id} not found!");
                }

                Console.WriteLine($"Before update - Title: {existingEntry.Title}, Username: {existingEntry.Username}, Category: {existingEntry.Category}");

                existingEntry.Title = entry.Title;
                existingEntry.Username = entry.Username;
                existingEntry.EncryptedPassword = entry.EncryptedPassword;
                existingEntry.Category = entry.Category;

                Console.WriteLine($"After update - Title: {existingEntry.Title}, Username: {existingEntry.Username}, Category: {existingEntry.Category}");

                await db.SaveChangesAsync();

                Console.WriteLine("=== Update successful ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Update ERROR: {ex.Message} ===");
                throw;
            }
        }

        public static async Task DeleteEntryAsync(string masterPassword, int id)
        {
            using var db = new DatabaseService(masterPassword);
            var entry = await db.PasswordEntries.FindAsync(id);
            if (entry != null)
            {
                db.PasswordEntries.Remove(entry);
                await db.SaveChangesAsync();
            }
        }

        public static async Task<List<PasswordEntry>> SearchEntriesAsync(string masterPassword, string searchText)
        {
            using var db = new DatabaseService(masterPassword);

            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllEntriesAsync(masterPassword);

            string searchLower = searchText.ToLower();

            return await db.PasswordEntries
                .Where(e => e.Title.ToLower().Contains(searchLower) ||
                           e.Username.ToLower().Contains(searchLower) ||
                           e.Category.ToLower().Contains(searchLower))
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();
        }

        public static async Task FixDoubleEncryptedPasswords(string masterPassword)
        {
            using var db = new DatabaseService(masterPassword);
            var encryptionService = new EncryptionService(masterPassword);
            var entries = await db.PasswordEntries.ToListAsync();

            foreach (var entry in entries)
            {
                try
                {
                    string onceDecrypted = encryptionService.Decrypt(entry.EncryptedPassword);
                    string twiceDecrypted = encryptionService.Decrypt(onceDecrypted);

                    if (!string.IsNullOrEmpty(twiceDecrypted) && twiceDecrypted != "Decryption error")
                    {
                        entry.EncryptedPassword = encryptionService.Encrypt(twiceDecrypted);
                        db.PasswordEntries.Update(entry);
                    }
                }
                catch
                {
                }
            }

            await db.SaveChangesAsync();
        }

        public static async Task<List<string>> GetCategoriesAsync(string masterPassword)
        {
            using var db = new DatabaseService(masterPassword);
            return await db.PasswordEntries
                .Select(e => e.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .ToListAsync();
        }

        public static async Task InitializeTestDataAsync(string masterPassword)
        {
            using var db = new DatabaseService(masterPassword);
            var entries = await db.PasswordEntries.ToListAsync();
            if (entries.Count == 0)
            {
                var encryptionService = new EncryptionService(masterPassword);
                var testEntries = new List<PasswordEntry>
                {
                    new PasswordEntry
                    {
                        Title = "Yandex",
                        Username = "user@yandex.ru",
                        EncryptedPassword = encryptionService.Encrypt("test123"),
                        Category = "Email",
                        CreatedDate = DateTime.Now.AddDays(-10)
                    },
                    new PasswordEntry
                    {
                        Title = "VKontakte",
                        Username = "my_login",
                        EncryptedPassword = encryptionService.Encrypt("test456"),
                        Category = "Social",
                        CreatedDate = DateTime.Now.AddDays(-5)
                    },
                    new PasswordEntry
                    {
                        Title = "Steam",
                        Username = "gamer123",
                        EncryptedPassword = encryptionService.Encrypt("test789"),
                        Category = "Games",
                        CreatedDate = DateTime.Now.AddDays(-2)
                    }
                };

                foreach (var entry in testEntries)
                {
                    await AddPasswordEntryAsync(masterPassword, entry);
                }
            }
        }

        public static async Task<int> GetEntriesCountAsync(string masterPassword)
        {
            using var db = new DatabaseService(masterPassword);
            return await db.PasswordEntries.CountAsync();
        }

        public static async Task ExportToCsvAsync(string masterPassword, string filePath)
        {
            var entries = await GetAllEntriesAsync(masterPassword);
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