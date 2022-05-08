namespace kanc_integrator
{
    public class AuthenticationConfig
    {
        public string Scopes { get; set; } = "https://graph.microsoft.com/.default";
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; } //set it as ENV param
        public string EndPoint { get; set; } //set it as ENV param
        public string DatabaseConnection { get; set; } //set it as ENV param
        public int Interval { get; set; } //set it as ENV param
    }
}