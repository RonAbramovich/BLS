using System;
using System.Numerics;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;

namespace BLS.Pairing.Implementations
{
    /// <summary>
    /// Miller's algorithm for computing the function f_{r,P}(Q).
    /// This is the core building block for pairing computations (Tate/Weil pairing).
    /// </summary>
    public static class MillerAlgorithm
    {
        /// <summary>
        /// Executes Miller's algorithm to compute f_{r,P}(Q).
        /// Uses double-and-add approach with line function accumulation.
        /// </summary>
        /// <param name="P">Point on base curve E(F_q)</param>
        /// <param name="Q">Point on extension curve E(F_q^k)</param>
        /// <param name="r">Order parameter (typically prime order of P)</param>
        /// <param name="baseCurve">Elliptic curve over base field</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <returns>Value f_{r,P}(Q) ∈ F_q^k</returns>
        /// <exception cref="ArgumentException">If P is at infinity or Q is at infinity</exception>
        public static ExtensionFieldElement ComputeMillerFunction(
            IECPoint<PrimeFieldElement> P,
            IECPoint<ExtensionFieldElement> Q,
            BigInteger r,
            EllipticCurve<PrimeFieldElement> baseCurve,
            ExtensionField extensionField)
        {
            // Validate inputs
            if (P.IsInfinity)
            {
                throw new ArgumentException("Point P cannot be at infinity", nameof(P));
            }

            if (Q.IsInfinity)
            {
                throw new ArgumentException("Point Q cannot be at infinity", nameof(Q));
            }

            if (r <= 1)
            {
                throw new ArgumentException("Order r must be > 1", nameof(r));
            }

            // Initialize accumulator f = 1 (in extension field)
            var f = extensionField.One;

            // Initialize working point T = P
            var T = P;

            // Get binary representation of r (bits from most significant to least)
            var rBits = GetBinaryBits(r);

            // Skip the most significant bit (always 1) - start from second bit
            for (int i = rBits.Length - 2; i >= 0; i--)
            {
                // ═══════════════════════════════════════════════════════
                // DOUBLING STEP: Always executed
                // ═══════════════════════════════════════════════════════
                
                // f = f² * l_{T,T}(Q)
                f = f * f;  // Square the accumulator
                
                var tangentLine = LineFunctionUtils.EvaluateTangentLine(
                    T, Q, extensionField, baseCurve.A);
                
                f = f * tangentLine;

                // T = 2T (point doubling on base curve)
                T = T.Double();

                // ═══════════════════════════════════════════════════════
                // ADDITION STEP: Only if current bit is 1
                // ═══════════════════════════════════════════════════════
                
                if (rBits[i] == 1)
                {
                    // f = f * l_{T,P}(Q)
                    var chordLine = LineFunctionUtils.EvaluateChordLine(
                        T, P, Q, extensionField);
                    
                    f = f * chordLine;

                    // T = T + P (point addition on base curve)
                    T = T.Add(P);
                }
            }

            // At the end, T should equal r*P (which should be infinity if r is the order of P)
            // The value f is our Miller function f_{r,P}(Q)

            return f;
        }

        /// <summary>
        /// Converts a BigInteger to its binary representation as a bit array.
        /// Returns bits from least significant (index 0) to most significant.
        /// </summary>
        /// <param name="value">Integer to convert</param>
        /// <returns>Array of bits (0 or 1), index 0 = LSB</returns>
        private static int[] GetBinaryBits(BigInteger value)
        {
            if (value <= 0)
            {
                return new int[] { 0 };
            }

            // Count number of bits needed
            int bitCount = 0;
            BigInteger temp = value;
            while (temp > 0)
            {
                bitCount++;
                temp >>= 1;
            }

            // Extract bits (LSB at index 0)
            var bits = new int[bitCount];
            for (int i = 0; i < bitCount; i++)
            {
                bits[i] = (value & (BigInteger.One << i)) != 0 ? 1 : 0;
            }

            return bits;
        }

        /// <summary>
        /// Helper method to get bit length of a BigInteger.
        /// </summary>
        private static int BitLength(BigInteger value)
        {
            if (value == 0) return 1;
            
            int length = 0;
            BigInteger temp = BigInteger.Abs(value);
            while (temp > 0)
            {
                length++;
                temp >>= 1;
            }
            return length;
        }
    }
}
