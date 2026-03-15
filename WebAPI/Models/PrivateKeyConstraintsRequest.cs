namespace BLS.WebAPI.Models
{
    public class PrivateKeyConstraintsRequest
    {
        public string PRIME_Q { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public string? Language { get; set; } = "he";
    }
}
