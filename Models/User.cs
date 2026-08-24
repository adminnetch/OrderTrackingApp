using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace OrderTrackingApp.Models
{
    public class User : IdentityUser // Eredita da IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty; // Nome

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty; // Cognome

        public string VisualName { get; set; } = string.Empty; // Nome Visualizzato

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty; // Ruolo (Admin o User)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Data di creazione
        public DateTime? LastUpdated { get; set; } // Data dell'ultimo aggiornamento

        private string _passwordHashManual = string.Empty;

        public string PasswordHashManual
        {
            get => _passwordHashManual;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _passwordHashManual = HashPassword(value);
                }
            }
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password)
        {
            string hashedPassword = HashPassword(password);
            return _passwordHashManual == hashedPassword;
        }

        public void UpdateLastUpdated()
        {
            LastUpdated = DateTime.UtcNow;
        }
    }
}
