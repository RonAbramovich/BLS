namespace BLS.WebAPI.Models
{
    public class AggregatedSignatureResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CalculationStep> CommonSteps { get; set; } = new();
        public List<IndividualSignature> IndividualSignatures { get; set; } = new();
        public List<IndividualSignature> Signatures { get; set; } = new();
        public string AggregatedPublicKey { get; set; } = string.Empty;
        public string AggregatedSignature { get; set; } = string.Empty;
        public string VerificationResult { get; set; } = string.Empty;
        public bool VerificationPassed { get; set; }
        public AggregationDetails? AggregationDetails { get; set; }
    }

    public class IndividualSignature
    {
        public string ParticipantName { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public List<CalculationStep> Steps { get; set; } = new();
    }

    public class AggregationDetails
    {
        public string MessageHash { get; set; } = string.Empty;
        public List<string> IndividualPublicKeys { get; set; } = new();
        public List<string> IndividualSignatures { get; set; } = new();
        public string AggregatedPublicKeyCalculation { get; set; } = string.Empty;
        public string AggregatedSignatureCalculation { get; set; } = string.Empty;
        public string VerificationPoint { get; set; } = string.Empty;
        public string AggregationFormula { get; set; } = string.Empty;
        public string AggregatedValue { get; set; } = string.Empty;
        public bool PairingCheck { get; set; }
    }
}
