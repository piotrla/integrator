using System;

namespace kanc_integrator
{
    public class ClientModel
    {
        public int Id { get; set; }
        public string NazwaKlienta { get; set; }
        public string NazwaKlientaPelna { get; set; }
        public string Nip { get; set; }
        public string Ulica { get; set; }
        public string Miejscowosc { get; set; }
        public string KodPocztowy { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public string Notatka { get; set; }
        public int? Stawka { get; set; }
        public string RodzajKlienta { get; set; }
        public string Pesel { get; set; }
    }
}