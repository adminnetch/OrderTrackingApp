using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Models
{
    public class DamageReport
    {
        public int Id { get; set; }

        public int RentalRequestId { get; set; }
        public RentalRequest RentalRequest { get; set; } = null!;

        public DateTime ReportedAt { get; set; } = DateTime.Now;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public bool IsResolved { get; set; } = false;
    }
}

