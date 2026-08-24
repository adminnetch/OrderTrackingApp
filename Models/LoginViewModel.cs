using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty; // Inizializzato

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty; // Inizializzato

        public bool RememberMe { get; set; } = false; // Valore di default
    }
}
