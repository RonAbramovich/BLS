namespace BLS.WebAPI.Models
{
    public class PrivateKeyConstraintsResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string Prime { get; set; } = string.Empty;
        public string CurveEquation { get; set; } = string.Empty;
        public string GroupOrder { get; set; } = string.Empty;
        public string R { get; set; } = string.Empty;
        public string GeneratorPoint { get; set; } = string.Empty;
        public List<string> ValidKeyExamples { get; set; } = new();
        public List<string> InvalidKeyExamples { get; set; } = new();
        public string ValidationRule { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string ConstraintDescription { get; set; } = string.Empty;
    }
}
