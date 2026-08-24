using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public enum RentalStatus
    {
        Pending,
        Approved,
        MaterialDelivered,
        RejectedWithReason,
        RejectedWithoutReason,
        Closed,
        Archived
    }

    public class RentalRequest
    {
        public int Id { get; set; }

        // Utente (visual name preso da User.VisualName)
        [Required]
        [Display(Name = "Utente")]
        public string UserVisualName { get; set; } = string.Empty;

        // Periodo
        [Required, Display(Name = "Check-in")]
        public DateTime CheckIn { get; set; }

        [Required, Display(Name = "Check-out")]
        public DateTime CheckOut { get; set; }

        // Progetto: FK verso CinemaOrder
        [Required, Display(Name = "Progetto")]
        public int CinemaOrderId { get; set; }
        public CinemaOrder? CinemaOrder { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tipologia")]
        public string Type { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Cliente")]
        public string Client { get; set; } = string.Empty;

        // Stato richiesta
        public RentalStatus Status { get; set; } = RentalStatus.Pending;

        // Se la richiesta è modificabile lato utente (es. dopo rifiuto con motivo)
        public bool IsEditableByUser { get; set; } = true;

        // Motivo rifiuto (opzionale)
        public string? RejectionReason { get; set; }

        // Annotazioni da parte dell'admin su eventuali modifiche
        public string? AdminModificationNote { get; set; }

        // Percorso del PDF generato (es. riepilogo o ricevuta)
        public string? ReceiptPdfPath { get; set; }

        // Oggetti richiesti
        public List<RentalRequestItem> RequestItems { get; set; } = new();
    }

    public class RentalRequestItem
    {
        public int Id { get; set; }

        // Link alla richiesta
        public int RentalRequestId { get; set; }
        public RentalRequest RentalRequest { get; set; } = null!;

        // Link all’oggetto noleggiato
        public int RentalItemId { get; set; }
        public RentalItem RentalItem { get; set; } = null!;
    }
}
