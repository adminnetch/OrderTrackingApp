using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class PianoDiLavorazione
    {
        public int Id { get; set; }

        [Required]
        public string TitoloCortometraggio { get; set; } = string.Empty;

        [Required]
        public string NomeProduzione { get; set; } = string.Empty;

        [Required]
        public string Regista { get; set; } = string.Empty;

        [Required]
        public string Produttore { get; set; } = string.Empty;

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        [ForeignKey("CinemaOrder")]
        public int CinemaOrderId { get; set; }
        public CinemaOrder CinemaOrder { get; set; } = null!;

        // ✅ Lista completa dei Giorni di Ripresa associati a questo Piano
        public List<GiornoRipresa> GiorniRipresa { get; set; } = new();
    }
}
