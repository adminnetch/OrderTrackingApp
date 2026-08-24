using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderTrackingApp.Models
{
    public class ODGOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string DayRec { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        [Required]
        public string Film { get; set; } = string.Empty;

        [Required]
        public string Regista { get; set; } = string.Empty;

        [Required]
        public string Produttore { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;
        public string Meteo { get; set; } = string.Empty;
        public string SceneDaGirare { get; set; } = string.Empty;
        public string Catering { get; set; } = string.Empty;

        public string ProntiAGirare { get; set; } = string.Empty;
        public string InizioRiprese { get; set; } = string.Empty;
        public string PausaPranzo { get; set; } = string.Empty;
        public string FineRiprese { get; set; } = string.Empty;
        public string TermineLavorazione { get; set; } = string.Empty;

        public string NoteProduzione { get; set; } = string.Empty;
        public string NoteRegia { get; set; } = string.Empty;
        public string InformazioniUtili { get; set; } = string.Empty;
        public string MezziTecnici { get; set; } = string.Empty;
        public string Costumi { get; set; } = string.Empty;
        public string TruccoeCapelli { get; set; } = string.Empty;
        public string SFX_VFX { get; set; } = string.Empty;
        public string Stunt { get; set; } = string.Empty;
        public string SpecialEquipment { get; set; } = string.Empty;

        public int CinemaOrderId { get; set; }
        public CinemaOrder? CinemaOrder { get; set; }

        // ✅ Inizializziamo le liste per evitare null reference
        public List<TroupeOrari> TroupeOrari { get; set; } = new();
        public List<CastConvocazioni> CastConvocazioni { get; set; } = new();
        public List<Trasporti> Trasporti { get; set; } = new();
        public List<Contatto> Contatti { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
