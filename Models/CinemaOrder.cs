using System;
using System.Collections.Generic;

namespace OrderTrackingApp.Models
{
    public class CinemaOrder
    {
        public int Id { get; set; }

        // Informazioni principali
        public string Title { get; set; } = string.Empty;
        public string Director { get; set; } = string.Empty;
        public string Producer { get; set; } = string.Empty;
        public string AssProducer { get; set; } = string.Empty;
        public string DoP { get; set; } = string.Empty;

        // Stato e metadati
        public string Status { get; set; } = "Progetto Creato";
        public string ProjectNumber { get; set; } = string.Empty;
        public string DriveLink { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;

        // RELAZIONE: Ogni CinemaOrder può avere molti ODG
        public List<ODGOrder> ODGs { get; set; } = new List<ODGOrder>();
        public List<Location> Locations { get; set; } = new();
        public List<PianoDiLavorazione> PianiDiLavorazione { get; set; } = new();
        public List<CentroCosto> CentriCosto { get; set; } = new();
        public List<RentalRequest> RentalRequests { get; set; } = new();



    }
}
