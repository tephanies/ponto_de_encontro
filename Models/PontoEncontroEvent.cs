namespace PontoDeEncontro.Models
{
    public class PontoEncontroEvent
    {
        public int Id { get; set; }
        public string? Pin { get; set; }
        public int AmbNumero { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Processed { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
