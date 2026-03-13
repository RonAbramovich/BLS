using System;
using System.Numerics;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;

namespace BLS.Pairing.Implementations
{
    /// <summary>
    /// Tate pairing implementation combining Miller's algorithm with final exponentiation.
    /// Provides a bilinear pairing e: G1 × G2 → GT
    /// </summary>
    public static class TatePairing
    {
        /// <summary>
        /// Computes the reduced Tate pairing e(P, Q).
        /// e(P, Q) = f_{r,P}(Q)^((q^k - 1)/r)
        /// </summary>
        /// <param name="P">Point in G1 (base field E(F_q))</param>
        /// <param name="Q">Point in G2 (extension field E(F_q^k))</param>
        /// <param name="r">Prime order of points</param>
        /// <param name="baseCurve">Elliptic curve over base field</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <returns>Pairing value e(P,Q) in GT (multiplicative subgroup of F_q^k*)</returns>
        public static ExtensionFieldElement Compute(
            IECPoint<PrimeFieldElement> P,
            IECPoint<ExtensionFieldElement> Q,
            BigInteger r,
            EllipticCurve<PrimeFieldElement> baseCurve,
            ExtensionField extensionField)
        {
            // Step 1: Compute Miller function f_{r,P}(Q)
            var f = MillerAlgorithm.ComputeMillerFunction(P, Q, r, baseCurve, extensionField);

            // Step 2: Apply final exponentiation
            var e = FinalExponentiation(f, extensionField, r);

            return e;
        }

        /// <summary>
        /// Applies final exponentiation to get from Miller function to pairing.
        /// Computes f^((q^k - 1)/r)
        /// </summary>
        /// <param name="f">Miller function value f_{r,P}(Q)</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <param name="r">Prime order</param>
        /// <returns>f^((q^k - 1)/r)</returns>
        public static ExtensionFieldElement FinalExponentiation(
            ExtensionFieldElement f,
            ExtensionField extensionField,
            BigInteger r)
        {
            BigInteger q = extensionField.BaseField.Characteristic;
            int k = extensionField.ExtensionDegree;

            // Calculate exponent: (q^k - 1) / r
            BigInteger qk = BigInteger.Pow(q, k);
            BigInteger exponent = (qk - 1) / r;

            // Verify r divides (q^k - 1)
            if ((qk - 1) % r != 0)
            {
                throw new ArgumentException(
                    $"r={r} does not divide (q^{k} - 1) = {qk - 1}. " +
                    "This violates the embedding degree property.");
            }

            // Compute f^exponent
            var result = f.Power(exponent);

            return result;
        }
    }
}
