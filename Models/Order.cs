using System;
using System.Security.Cryptography;

namespace OrderTrackingApp.Models
{
    public class Order
    {
        public int Id { get; set; } // Identificativo univoco per il database

        // Numero ordine generato automaticamente (6 cifre)
        public string OrderNumber { get; set; }

        // Numero di tracking generato automaticamente (12 cifre)
        public string TrackingNumber { get; set; }

        // Dettagli cliente
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;

        // Dettagli ordine
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Progetto Creato";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now; // Inizialmente impostata alla creazione

        // Campo per il link alla cartella Drive
        public string DriveLink { get; set; } = string.Empty; // Link alla cartella Drive

        // Note per il cliente (opzionali, modificate solo in seguito)
        public string? CustomerNotes { get; set; }

        // Campo opzionale per eventuali informazioni aggiuntive
        public string? AdditionalInfo { get; set; }

        // Proprietà per il percorso del file caricato (documento PDF)
        public string? DocumentPath { get; set; } // Path del file caricato

        // Costruttore della classe per generare automaticamente i numeri
        public Order()
        {
            OrderNumber = GenerateOrderNumber(); // Genera numero ordine
            TrackingNumber = GenerateTrackingNumber(); // Genera numero tracking
        }

        // Generazione thread-safe di un numero d'ordine di 6 cifre
        private static string GenerateOrderNumber()
        {
            return GenerateNumericString(6);
        }

        // Generazione thread-safe di un numero di tracking di 12 cifre
        private static string GenerateTrackingNumber()
        {
            return GenerateNumericString(12);
        }

        // Generatore di stringa numerica thread-safe usando RandomNumberGenerator
        private static string GenerateNumericString(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            // Trasforma ogni byte in cifra 0-9, garantendo che sia sempre una cifra
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = (char)('0' + (bytes[i] % 10));
            }
            return new string(result);
        }
    }
}
