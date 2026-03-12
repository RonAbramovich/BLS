using System;
using System.Numerics;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;

namespace BLS.Pairing.Implementations
{
    /// <summary>
    /// Line function evaluation for Miller's algorithm.
    /// Evaluates tangent/chord lines needed for pairing computation.
    /// </summary>
    public static class LineFunction
    {
        /// <summary>
        /// Evaluates the tangent line at point T, evaluated at point Q.
        /// Used during point doubling in Miller's algorithm.
        /// </summary>
        /// <param name="T">Point on base curve E(F_q)</param>
        /// <param name="Q">Point on extension curve E(F_q^k)</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <param name="curveA">Curve parameter A from y² = x³ + Ax + B</param>
        /// <returns>Value of tangent line l_{T,T}(Q) ∈ F_q^k</returns>
        public static ExtensionFieldElement EvaluateTangentLine(
            IECPoint<PrimeFieldElement> T,
            IECPoint<ExtensionFieldElement> Q,
            ExtensionField extensionField,
            PrimeFieldElement curveA)
        {
            // Handle special cases: point at infinity
            if (T.IsInfinity || Q.IsInfinity)
            {
                return extensionField.One;
            }

            // Extract coordinates from T (in base field F_q)
            BigInteger x_T = T.X.Value;
            BigInteger y_T = T.Y.Value;
            BigInteger q = extensionField.BaseField.Characteristic;
            BigInteger A = curveA.Value;

            // Calculate slope λ = (3x_T² + A) / (2y_T) in F_q
            BigInteger numerator = (3 * x_T * x_T + A) % q;
            if (numerator < 0) numerator += q;

            BigInteger denominator = (2 * y_T) % q;
            if (denominator < 0) denominator += q;

            // Check for vertical tangent (denominator = 0) - RON TODO : Understand
            if (denominator == 0)
            {
                // Vertical line: x = x_T, evaluate at Q: result = x_Q - x_T
                var x_T_lifted_vertical = extensionField.FromInt(x_T);
                return Q.X - x_T_lifted_vertical;
            }

            BigInteger lambda = ModularDivide(numerator, denominator, q);

            // Lift T's coordinates and slope to extension field F_q^k
            var x_T_lifted = extensionField.FromInt(x_T);
            var y_T_lifted = extensionField.FromInt(y_T);
            var lambda_lifted = extensionField.FromInt(lambda);

            // Extract Q's coordinates (already in F_q^k)
            var x_Q = Q.X;
            var y_Q = Q.Y;

            // Evaluate line function: l(Q) = y_Q - y_T - λ(x_Q - x_T)
            var result = y_Q - y_T_lifted - lambda_lifted * (x_Q - x_T_lifted);

            return result;
        }

        /// <summary>
        /// Evaluates the chord line through points T and S, evaluated at point Q.
        /// Used during point addition in Miller's algorithm.
        /// </summary>
        /// <param name="T">First point on base curve E(F_q)</param>
        /// <param name="S">Second point on base curve E(F_q)</param>
        /// <param name="Q">Point on extension curve E(F_q^k)</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <returns>Value of chord line l_{T,S}(Q) ∈ F_q^k</returns>
        public static ExtensionFieldElement EvaluateChordLine(
            IECPoint<PrimeFieldElement> T,
            IECPoint<PrimeFieldElement> S,
            IECPoint<ExtensionFieldElement> Q,
            ExtensionField extensionField)
        {
            // Handle special cases
            if (T.IsInfinity || S.IsInfinity || Q.IsInfinity)
            {
                return extensionField.One;
            }

            // Extract coordinates from T and S (in base field F_q)
            BigInteger x_T = T.X.Value;
            BigInteger y_T = T.Y.Value;
            BigInteger x_S = S.X.Value;
            BigInteger y_S = S.Y.Value;
            BigInteger q = extensionField.BaseField.Characteristic;

            // Check for vertical line (x_T == x_S)
            if (x_T == x_S)
            {
                // Vertical line: x = x_T
                // Evaluate at Q: result = x_Q - x_T
                var x_T_lifted_vertical = extensionField.FromInt(x_T);
                return Q.X - x_T_lifted_vertical;
            }

            // Calculate slope λ = (y_S - y_T) / (x_S - x_T) in F_q
            BigInteger numerator = (y_S - y_T) % q;
            if (numerator < 0) numerator += q;

            BigInteger denominator = (x_S - x_T) % q;
            if (denominator < 0) denominator += q;

            BigInteger lambda = ModularDivide(numerator, denominator, q);

            // Lift coordinates and slope to extension field
            var x_T_lifted = extensionField.FromInt(x_T);
            var y_T_lifted = extensionField.FromInt(y_T);
            var lambda_lifted = extensionField.FromInt(lambda);

            // Extract Q's coordinates
            var x_Q = Q.X;
            var y_Q = Q.Y;

            // Evaluate line function: l(Q) = y_Q - y_T - λ(x_Q - x_T)
            var result = y_Q - y_T_lifted - lambda_lifted * (x_Q - x_T_lifted);

            return result;
        }

        /// <summary>
        /// Performs modular division: (numerator / denominator) mod modulus.
        /// Uses Fermat's Little Theorem for prime modulus: a^(-1) = a^(p-2) mod p
        /// </summary>
        private static BigInteger ModularDivide(
            BigInteger numerator,
            BigInteger denominator,
            BigInteger modulus)
        {
            if (BigInteger.GreatestCommonDivisor(denominator, modulus) != 1)
            {
                throw new InvalidOperationException(
                    $"Cannot compute modular inverse: gcd({denominator}, {modulus}) != 1");
            }

            // Calculate modular inverse using Fermat's Little Theorem
            // For prime p: a^(-1) ≡ a^(p-2) (mod p)
            BigInteger inverse = BigInteger.ModPow(denominator, modulus - 2, modulus);

            // Multiply and reduce
            BigInteger result = (numerator * inverse) % modulus;
            if (result < 0) result += modulus;

            return result;
        }
    }
}
