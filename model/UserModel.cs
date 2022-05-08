namespace kanc_integrator
{
    public class UserModel
    {
        public int Id { get; set; }
        public string NazwaUzytkownika { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public string Funkcja { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool Aktywny { get; set; }
    }
}