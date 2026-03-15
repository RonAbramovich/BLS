namespace BLS.WebAPI.Models
{
    public class AggregatedSignatureRequest
    {
        public string PRIME_Q { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<ParticipantData> Participants { get; set; } = new();
        public string? Language { get; set; } = "he";
        public bool IncludeDetailedReport { get; set; }
    }

    public class ParticipantData
    {
        public string Name { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
    }
}
