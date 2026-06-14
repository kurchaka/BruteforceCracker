using System;
using System.Security.Cryptography;
using System.Text;

namespace BruteForceCracker
{
    public class PasswordHasher
    {
        private const string SALT = "ManoSlaptaStatinėDruska2026!";

        public string ComputeHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + SALT));
                return Convert.ToHexString(bytes);
            }
        }
    }
}
