using System;
using System.ComponentModel.DataAnnotations;

namespace OrderTrackingApp.Models
{
    public enum ProductionRole
    {
        Regista,
        Produttore,
        Produttore_Esecutvo,
        Line_Producer,
        Location_Manager,
        Coordinatore_Produzione,
        Aiuto_Regia,
        OP_Camera,
        First_Camera_Assistant,
        DoP,
        Script,
        Backstager,
        Story_Editor,
        Fonico,
        Microfonista,
        Runner,
        DiT,
        Montatore,
        Colorist,
        Elettricista,
        Ass_Regia,
        Scenografo,
        Ass_Produzione,
        Attori

    }

    [Flags]
    public enum TransportSubscription
    {
        None                  = 0,

        // Nazionali
        [Display(Name = "Carta Halbtax (50% di sconto)")]
        HalfFareCard          = 1 << 0,

        [Display(Name = "GA (abbonamento illimitato)")]
        GAFull                = 1 << 1,

        [Display(Name = "GA metà prezzo")]
        GAHalf                = 1 << 2,

        [Display(Name = "Swiss Travel Pass")]
        SwissTravelPass       = 1 << 3,

        [Display(Name = "Swiss Travel Pass FLEX")]
        SwissTravelPassFlex   = 1 << 4,

        [Display(Name = "Saver Day Pass")]
        SaverDayPass          = 1 << 5,

        // Regionali
        [Display(Name = "ZVV (Zurigo)")]
        ZVV                   = 1 << 6,

        [Display(Name = "Libero (Berna / Basilea)")]
        Libero                = 1 << 7,

        [Display(Name = "Mobilis (Vaud)")]
        Mobilis               = 1 << 8,

        [Display(Name = "Passepartout (Friburgo)")]
        Passepartout          = 1 << 9,

        [Display(Name = "Unireso (Ginevra)")]
        Unireso               = 1 << 10,

        [Display(Name = "on (Neuchâtel)")]
        On                    = 1 << 11,

        [Display(Name = "TarifJurassien (Giura)")]
        TarifJurassien        = 1 << 12,

        [Display(Name = "Z-Pass (Zugo / Lucerna)")]
        ZPass                 = 1 << 13,

        [Display(Name = "Arcobaleno (Ticino / Moesano)")]
        Arcobaleno            = 1 << 14,

        [Display(Name = "TPER Bellinzona (urbano)")]
        TPER                  = 1 << 15,

        [Display(Name = "Abbonamento regionale SBB")]
        SBBRegional           = 1 << 16
    }

    [Flags]
    public enum SwissLicense
    {
        None = 0,
        A1   = 1 << 0,   // moto leggere ≤125 cc
        A2   = 1 << 1,   // moto ≤35 kW
        A    = 1 << 2,   // tutte le moto
        B    = 1 << 3,   // auto ≤3.5 t
        BE   = 1 << 4,   // auto + rimorchio
        C1   = 1 << 5,   // camion ≤7.5 t
        C1E  = 1 << 6,   // C1 + rimorchio
        C    = 1 << 7,   // camion >7.5 t
        CE   = 1 << 8,   // C + rimorchio
        D1   = 1 << 9,   // autobus <16 posti
        D1E  = 1 << 10,  // D1 + rimorchio
        D    = 1 << 11,  // autobus >16 posti
        DE   = 1 << 12,  // D + rimorchio
        F    = 1 << 13,  // macchine agricole
        G    = 1 << 14,  // trattori
        M    = 1 << 15   // ciclomotori <50 cc
    }

    public class TroupeCastContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CinemaOrderId { get; set; }

        // <<< nullable, senza [Required]
        public CinemaOrder? CinemaOrder { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public ProductionRole Role { get; set; }

        public TransportSubscription Subscription { get; set; }

        public SwissLicense? License { get; set; }

        public bool HasAccidentInsurance { get; set; }

        // <<< nullable, senza [Required]
        public EmergencyContact? EmergencyContact { get; set; }
    }
}
