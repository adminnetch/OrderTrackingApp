using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OrderTrackingApp.Models
{
    public class VoceSpesa
    {
        public int Id { get; set; }

        [Required]
        public DateTime Data { get; set; }

        [Required]
        public string Tipo { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        public decimal Importo { get; set; }

        public string? Nota { get; set; }

        public string? ScontrinoPath { get; set; }

        [ForeignKey("CentroCosto")]
        public int CentroCostoId { get; set; }

        [ValidateNever]
        public CentroCosto CentroCosto { get; set; } = null!;
    }
}
