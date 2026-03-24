namespace PontoDeEncontro.Models
{
    public class Ambiente
    {
        public int AmbNumero { get; set; }
        public string AmbDescricao { get; set; } = string.Empty;
        public bool AmbPontoEncontro { get; set; }
        public bool AmbExterno { get; set; }
    }
}
