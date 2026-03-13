using System;
using System.Numerics;
using BLS.ElipticCurve.Interfaces;
using BLS.ElipticCurve.Utilities;
using BLS.Fields.Implementations;

namespace BLS.Pairing.Implementations
{
    /// <summary>
    /// Line function evaluation for Miller's algorithm.
    /// Uses shared utilities from EllipticCurveUtils to avoid code duplication.
    /// </summary>
    public static class LineFunctionUtils
    {
        /// <summary>
        /// Evaluates the tangent line at point T, evaluated at point Q.
        /// Used during point doubling in Miller's algorithm.
        /// </summary>
        /// <param name="T">Point on base curve E(F_q)</param>
        /// <param name="Q">Point on extension curve E(F_q^k)</param>
        /// <param name="extensionField">Extension field F_q^k</param>
        /// <param name="curveACoeff">Curve parameter A from y² = x³ + Ax + B</param>
        /// <returns>Value of tangent line l_{T,T}(Q) ∈ F_q^k</returns>
        public static ExtensionFieldElement EvaluateTangentLine(
            IECPoint<PrimeFieldElement> T,
            IECPoint<ExtensionFieldElement> Q,
            ExtensionField extensionField,
            PrimeFieldElement curveACoeff)
        {
            if (T.IsInfinity || Q.IsInfinity)
            {
                return extensionField.One;
            }

            var x_T = T.X.Value;
            var y_T = T.Y.Value;
            var q = extensionField.BaseField.Characteristic;
            var A = curveACoeff.Value;

            // Use shared utility to compute tangent slope
            var slopeNullable = EllipticCurveUtils.ComputeTangentSlope(x_T, y_T, A, q);

            // Handle vertical tangent (slope is null when y = 0)
            if (slopeNullable == null)
            {
                // Vertical line: x = x_T, evaluate at Q: result = x_Q - x_T
                var x_T_lifted = extensionField.FromInt(x_T);
                return Q.X - x_T_lifted;
            }

            var slope = slopeNullable.Value;

            // Lift T's coordinates and slope to extension field F_q^k
            var x_T_lifted_main = extensionField.FromInt(x_T);
            var y_T_lifted = extensionField.FromInt(y_T);
            var slope_lifted = extensionField.FromInt(slope);

            // Evaluate line function: l(Q) = y_Q - y_T - slope*(x_Q - x_T)
            return Q.Y - y_T_lifted - slope_lifted * (Q.X - x_T_lifted_main);
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
            if (T.IsInfinity || S.IsInfinity || Q.IsInfinity)
            {
                return extensionField.One;
            }

            var x_T = T.X.Value;
            var y_T = T.Y.Value;
            var x_S = S.X.Value;
            var y_S = S.Y.Value;
            var q = extensionField.BaseField.Characteristic;

            // Use shared utility to compute chord slope
            var slopeNullable = EllipticCurveUtils.ComputeChordSlope(x_T, y_T, x_S, y_S, q);

            // Handle vertical line (slope is null when x_T = x_S)
            if (slopeNullable == null)
            {
                // Vertical line: x = x_T, evaluate at Q: result = x_Q - x_T
                var x_T_lifted = extensionField.FromInt(x_T);
                return Q.X - x_T_lifted;
            }

            var slope = slopeNullable.Value;

            // Lift coordinates and slope to extension field
            var x_T_lifted_main = extensionField.FromInt(x_T);
            var y_T_lifted = extensionField.FromInt(y_T);
            var slope_lifted = extensionField.FromInt(slope);

            // Evaluate line function: l(Q) = y_Q - y_T - slope * (x_Q - x_T)
            return Q.Y - y_T_lifted - slope_lifted * (Q.X - x_T_lifted_main);
        }
    }
}
