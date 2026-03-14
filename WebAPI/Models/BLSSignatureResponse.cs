using System.Collections.Generic;

namespace BLS.WebAPI.Models
{
    /// <summary>
    /// Response model containing step-by-step calculation results
    /// </summary>
    public class BLSSignatureResponse
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// List of calculation steps with descriptions and results
        /// </summary>
        public List<CalculationStep> Steps { get; set; } = new();

        /// <summary>
        /// Alice's signature value (as string representation)
        /// </summary>
        public string? AliceSignature { get; set; }

        /// <summary>
        /// Bob's verification value (as string representation)
        /// </summary>
        public string? BobVerification { get; set; }

        /// <summary>
        /// Indicates if signature verification passed (Alice signature == Bob verification)
        /// </summary>
        public bool VerificationPassed { get; set; }

        /// <summary>
        /// Comprehensive calculation report with all intermediate values
        /// </summary>
        public DetailedCalculationReport? DetailedReport { get; set; }
    }

    /// <summary>
    /// Represents a single calculation step
    /// </summary>
    public class CalculationStep
    {
        /// <summary>
        /// Step number (1-9)
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// Description of the step (in requested language)
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Technical description (English, for debugging)
        /// </summary>
        public string TechnicalDescription { get; set; } = string.Empty;

        /// <summary>
        /// Result of the calculation (formatted string)
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Additional details about the calculation
        /// </summary>
        public Dictionary<string, string>? Details { get; set; }
    }
}
