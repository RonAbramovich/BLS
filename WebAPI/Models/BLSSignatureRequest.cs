using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BLS.WebAPI.Models
{
    /// <summary>
    /// Request model for BLS signature generation and verification
    /// </summary>
    public class BLSSignatureRequest
    {
        /// <summary>
        /// Prime characteristic q for the base field F_q (must be prime and q = 3 mod 4)
        /// </summary>
        [Required]
        [JsonPropertyName("q")]
        public string PRIME_Q { get; set; } = string.Empty;

        /// <summary>
        /// Elliptic curve parameter A (for curve y^2 = x^3 + Ax + B)
        /// </summary>
        [Required]
        [JsonPropertyName("a")]
        public string A { get; set; } = string.Empty;

        /// <summary>
        /// Elliptic curve parameter B (for curve y^2 = x^3 + Ax + B)
        /// </summary>
        [Required]
        [JsonPropertyName("b")]
        public string B { get; set; } = string.Empty;

        /// <summary>
        /// Private key (scalar value)
        /// </summary>
        [Required]
        [JsonPropertyName("privateKey")]
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Message to sign
        /// </summary>
        [Required]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Language preference: "he" for Hebrew, "en" for English
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = "he";

        /// <summary>
        /// If true, includes detailed calculation traces in the response
        /// </summary>
        [JsonPropertyName("includeDetailedReport")]
        public bool IncludeDetailedReport { get; set; } = true;
    }
}
