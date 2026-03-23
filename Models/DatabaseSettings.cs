namespace PontoDeEncontro.Models
{
    public class DatabaseSettings
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseIntegratedSecurity { get; set; }
    }
}
