using System;

namespace kanc_integrator
{
    public class ActionModel
    {
        public Guid Id { get; set; }
        public Guid SprawaId { get; set; }
        public string NazwaSprawy { get; set; }
        public int KlientId { get; set; }
        public string Wykonawca { get; set; }
        public DateTime DataWykonania { get; set; }
        public string Opis { get; set; }
        public bool? IsEdited { get; set; }
        public string WhoEditing { get; set; }
        public decimal CzasWykonania { get; set; }
        public string Status { get; set; }
    }
}