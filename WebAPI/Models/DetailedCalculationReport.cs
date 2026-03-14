using System.Collections.Generic;

namespace BLS.WebAPI.Models
{
    /// <summary>
    /// Comprehensive calculation report containing all intermediate values and computations
    /// </summary>
    public class DetailedCalculationReport
    {
        public Step3Details? MessageHashingDetails { get; set; }
        public Step7Details? TorsionPointDetails { get; set; }
        public Step8Details? SignatureCalculationDetails { get; set; }
        public Step9Details? VerificationCalculationDetails { get; set; }
    }

    /// <summary>
    /// Detailed calculations for Step 3: Hash message to curve point
    /// </summary>
    public class Step3Details
    {
        public string OriginalMessage { get; set; } = string.Empty;
        public string EncodingUsed { get; set; } = "Windows-1255";
        public string MessageBytesHex { get; set; } = string.Empty;
        public string EncodedInteger { get; set; } = string.Empty;
        public string InitialX0 { get; set; } = string.Empty;
        public int TrialsUntilSuccess { get; set; }
        public List<HashTrialAttempt> Attempts { get; set; } = new();
        public string FinalPointX { get; set; } = string.Empty;
        public string FinalPointY { get; set; } = string.Empty;
        public string CofactorUsed { get; set; } = string.Empty;
        public PointDetails? PointBeforeCofactorClearing { get; set; }
        public PointDetails? PointAfterCofactorClearing { get; set; }
    }

    public class HashTrialAttempt
    {
        public int Increment { get; set; }
        public string CandidateX { get; set; } = string.Empty;
        public string ComputedZ { get; set; } = string.Empty;
        public bool IsQuadraticResidue { get; set; }
        public string? SquareRootY { get; set; }
    }

    /// <summary>
    /// Detailed calculations for Step 7: Find torsion point Q
    /// </summary>
    public class Step7Details
    {
        public string TargetOrder { get; set; } = string.Empty;
        public string ExtensionFieldSize { get; set; } = string.Empty;
        public string FinalPointX_Polynomial { get; set; } = string.Empty;
        public string FinalPointY_Polynomial { get; set; } = string.Empty;
        public int PointX_Degree { get; set; }
        public int PointY_Degree { get; set; }
        public string CofactorMultiplier { get; set; } = string.Empty;
        public bool IsLinearlyIndependent { get; set; }
    }

    /// <summary>
    /// Detailed calculations for Step 8: Alice's signature computation
    /// </summary>
    public class Step8Details
    {
        public string PrivateKey { get; set; } = string.Empty;
        public string MessageHashPoint_X { get; set; } = string.Empty;
        public string MessageHashPoint_Y { get; set; } = string.Empty;
        public string TorsionPoint_X { get; set; } = string.Empty;
        public string TorsionPoint_Y { get; set; } = string.Empty;
        public string MultipliedPoint_X { get; set; } = string.Empty;
        public string MultipliedPoint_Y { get; set; } = string.Empty;
        public string FinalSignature { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed calculations for Step 9: Bob's verification computation
    /// </summary>
    public class Step9Details
    {
        public string PrivateKey { get; set; } = string.Empty;
        public string MessageHashPoint_X { get; set; } = string.Empty;
        public string MessageHashPoint_Y { get; set; } = string.Empty;
        public string TorsionPoint_X { get; set; } = string.Empty;
        public string TorsionPoint_Y { get; set; } = string.Empty;
        public string MultipliedPoint_X { get; set; } = string.Empty;
        public string MultipliedPoint_Y { get; set; } = string.Empty;
        public string FinalVerification { get; set; } = string.Empty;
        public bool SignatureMatches { get; set; }
    }

    public class PointDetails
    {
        public string X { get; set; } = string.Empty;
        public string Y { get; set; } = string.Empty;
        public bool IsInfinity { get; set; }
    }
}
