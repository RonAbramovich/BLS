using System;
using System.Numerics;
using BLS.ElipticCurve.Interfaces;
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
        /// Formula: λ = (3x² + A) / (2y) mod q
        /// Used in both ECPoint.Double() and Miller's algorithm.
        /// </summary>
        /// <param name="x">X-coordinate of point T</param>
        /// <param name="y">Y-coordinate of point T</param>
        /// <param name="curveA">Curve parameter A from y² = x³ + Ax + B</param>
        /// <param name="modulus">Field characteristic q</param>
        /// <returns>Tangent slope λ, or null if vertical tangent (y = 0)</returns>
        public static BigInteger? ComputeTangentSlope(
            BigInteger x,
            BigInteger y,
            BigInteger curveA,
            BigInteger modulus)
        {
            // Normalize inputs
            x = NumberTheoryUtils.ModNormalize(x, modulus);
            y = NumberTheoryUtils.ModNormalize(y, modulus);
            curveA = NumberTheoryUtils.ModNormalize(curveA, modulus);

            // Check for vertical tangent (y = 0)
            if (y == 0)
            {
                return null; // Vertical tangent, slope is undefined
            }

            // λ = (3x² + A) / (2y)
            BigInteger numerator = (3 * x * x + curveA) % modulus;
            numerator = NumberTheoryUtils.ModNormalize(numerator, modulus);

            BigInteger denominator = (2 * y) % modulus;
            denominator = NumberTheoryUtils.ModNormalize(denominator, modulus);

            return NumberTheoryUtils.ModularDivide(numerator, denominator, modulus);
        }

        /// <summary>
        /// Calculates the chord line slope between points T and S for point addition.
        /// Formula: λ = (y₂ - y₁) / (x₂ - x₁) mod q
        /// Used in both ECPoint.Add() and Miller's algorithm.
        /// </summary>
        /// <param name="x1">X-coordinate of point T</param>
        /// <param name="y1">Y-coordinate of point T</param>
        /// <param name="x2">X-coordinate of point S</param>
        /// <param name="y2">Y-coordinate of point S</param>
        /// <param name="modulus">Field characteristic q</param>
        /// <returns>Chord slope λ, or null if vertical line (x₁ = x₂)</returns>
        public static BigInteger? ComputeChordSlope(
            BigInteger x1,
            BigInteger y1,
            BigInteger x2,
            BigInteger y2,
            BigInteger modulus)
        {
            // Normalize inputs
            x1 = NumberTheoryUtils.ModNormalize(x1, modulus);
            y1 = NumberTheoryUtils.ModNormalize(y1, modulus);
            x2 = NumberTheoryUtils.ModNormalize(x2, modulus);
            y2 = NumberTheoryUtils.ModNormalize(y2, modulus);

            // Check for vertical line (x₁ = x₂)
            if (x1 == x2)
            {
                return null; // Vertical line, slope is undefined
            }

            // λ = (y₂ - y₁) / (x₂ - x₁)
            BigInteger numerator = (y2 - y1) % modulus;
            numerator = NumberTheoryUtils.ModNormalize(numerator, modulus);

            BigInteger denominator = (x2 - x1) % modulus;
            denominator = NumberTheoryUtils.ModNormalize(denominator, modulus);

            return NumberTheoryUtils.ModularDivide(numerator, denominator, modulus);
        }

        /// <summary>
        /// Evaluates a line in point-slope form at a given point.
        /// Formula: l(x, y) = y - y₀ - λ(x - x₀)
        /// </summary>
        /// <param name="x">X-coordinate to evaluate at</param>
        /// <param name="y">Y-coordinate to evaluate at</param>
        /// <param name="x0">X-coordinate of line point</param>
        /// <param name="y0">Y-coordinate of line point</param>
        /// <param name="slope">Line slope λ</param>
        /// <param name="modulus">Field modulus</param>
        /// <returns>Line evaluation result</returns>
        public static BigInteger EvaluateLine(
            BigInteger x,
            BigInteger y,
            BigInteger x0,
            BigInteger y0,
            BigInteger slope,
            BigInteger modulus)
        {
            // l(x, y) = y - y₀ - λ(x - x₀)
            BigInteger result = y - y0 - slope * (x - x0);
            return NumberTheoryUtils.ModNormalize(result, modulus);
        }
    }
}
