using System.Numerics;
using BLS.Fields.Implementations;

namespace BLS.ElipticCurve.Utilities
{
    /// <summary>
    /// Utility methods for elliptic curve operations.
    /// Centralizes slope calculations used in both point arithmetic and pairing computations.
    /// </summary>
    public static class EllipticCurveUtils
    {
        /// <summary>
        /// Calculates the tangent line slope at point T for point doubling.
        /// Formula: slope = (3x^2 + A) / (2y) mod q
        /// </summary>
        /// <param name="x">X-coordinate of point T</param>
        /// <param name="y">Y-coordinate of point T</param>
        /// <param name="curveACoeff">Curve parameter A </param>
        /// <param name="modulus">Field characteristic q</param>
        /// <returns>Tangent slope , or null if vertical tangent (y = 0)</returns>
        public static BigInteger? ComputeTangentSlope(BigInteger x, BigInteger y, BigInteger curveACoeff, BigInteger modulus)
        {
            x = NumberTheoryUtils.ModNormalize(x, modulus);
            y = NumberTheoryUtils.ModNormalize(y, modulus);
            curveACoeff = NumberTheoryUtils.ModNormalize(curveACoeff, modulus);

            // Check for vertical tangent (y = 0)
            if (y == 0)
            {
                return null; // Vertical tangent, slope is undefined
            }

            BigInteger numerator = 3 * x * x + curveACoeff;
            BigInteger denominator = 2 * y;
            numerator = NumberTheoryUtils.ModNormalize(numerator, modulus);
            denominator = NumberTheoryUtils.ModNormalize(denominator, modulus);

            return NumberTheoryUtils.ModularDivide(numerator, denominator, modulus);
        }

        /// <summary>
        /// Calculates the chord line slope between points T and S for point addition.
        /// Formula: slope = (y2 - y1) / (x2 - x1) mod q
        /// </summary>
        /// <param name="x1">X-coordinate of point T</param>
        /// <param name="y1">Y-coordinate of point T</param>
        /// <param name="x2">X-coordinate of point S</param>
        /// <param name="y2">Y-coordinate of point S</param>
        /// <param name="modulus">Field characteristic q</param>
        /// <returns>Chord slope , or null if vertical line (x₁ = x₂)</returns>
        public static BigInteger? ComputeChordSlope(BigInteger x1, BigInteger y1, BigInteger x2, BigInteger y2, BigInteger modulus)
        {
            // Normalize inputs
            x1 = NumberTheoryUtils.ModNormalize(x1, modulus);
            y1 = NumberTheoryUtils.ModNormalize(y1, modulus);
            x2 = NumberTheoryUtils.ModNormalize(x2, modulus);
            y2 = NumberTheoryUtils.ModNormalize(y2, modulus);

            // Check for vertical line
            if (x1 == x2)
            {
                return null; // Vertical line, slope is undefined
            }

            var numerator = y2 - y1;
            var denominator = x2 - x1;
            numerator = NumberTheoryUtils.ModNormalize(numerator, modulus);
            denominator = NumberTheoryUtils.ModNormalize(denominator, modulus);

            return NumberTheoryUtils.ModularDivide(numerator, denominator, modulus);
        }
    }
}
