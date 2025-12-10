using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace SecureUstuj.Services
{
    public class PasswordGenerator
    {
        private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Specials = "!@#$%^&*()-_=+[]{}|;:,.<>?";

        public string GeneratePassword(int length = 12, bool useUpper = true, bool useDigits = true, bool useSpecial = true)
        {
            var chars = new StringBuilder(Lowercase);

            if (useUpper) chars.Append(Uppercase);
            if (useDigits) chars.Append(Digits);
            if (useSpecial) chars.Append(Specials);

            var charSet = chars.ToString();
            var result = new StringBuilder();

            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[4];

                for (int i = 0; i < length; i++)
                {
                    rng.GetBytes(buffer);
                    var num = BitConverter.ToUInt32(buffer, 0);
                    var index = (int)(num % (uint)charSet.Length);
                    result.Append(charSet[index]);
                }
            }

            return result.ToString();
        }

        public string GeneratePassword()
        {
            return GeneratePassword(12, true, true, true);
        }
    }
}