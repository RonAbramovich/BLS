using System;
using System.Numerics;
using System.Text;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;
using BLS.Fields.Interfaces;

namespace BLS.ElipticCurve.Implementations
{
    public static class HashToPoint
    {
        /// <summary>
        /// Deterministic try-and-increment hash-to-point for curves over prime fields.
        /// Produces a point in the subgroup of order r by cofactor clearing: H(m) = (|G|/r) * P_temp.
        /// </summary>
        public static IECPoint<PrimeFieldElement> HashToCurve(IEllipticCurve<PrimeFieldElement> curve, string message)
        {
            ArgumentNullException.ThrowIfNull(curve);
            ArgumentNullException.ThrowIfNull(message);

            // determine underlying prime field from curve coefficients (A,B are PrimeFieldElement)
            var baseField = curve.A.Field;
            var p = baseField.Characteristic;
            long x0 = EncodeMessageToInteger(message, p);

            // Coefficients A and B as integers
            int aInt = curve.A.Value;
            int bInt = curve.B.Value;

            // Try and increment x until we find a quadratic residue for z = x^3 + a*x + b
            for (int increametCandidate = 0; increametCandidate < p; increametCandidate++)
            {
                var xi = (int)((x0 + increametCandidate) % p);
                // compute z = xi^3 + a*xi + b (mod p)
                var zInt = (BigInteger.Pow(new BigInteger(xi), 3) + (BigInteger)aInt * xi + bInt) % p;
                if (zInt < 0)
                {
                    zInt += p;
                }

                var zVal = (int)zInt;
                int yInt = NumberTheoryUtils.SqrtModP(zVal, p);
                if (yInt == -1)
                {
                    // no square root for this z -> try next x
                    continue;
                }

                // build temporary point and clear cofactor
                var TempPointXCoordinate = baseField.FromInt(xi);
                var TempPointYCoordinate = baseField.FromInt(yInt);
                var Ptemp = curve.CreatePoint(TempPointXCoordinate, TempPointYCoordinate);

                var hashedPointFromMessage = ClearCofactor(curve, Ptemp);
                if (hashedPointFromMessage.IsInfinity)
                {
                    continue; // Validation, should not happen. 
                }

                return hashedPointFromMessage;
            }

            throw new InvalidOperationException("Failed to hash message to a curve point.");
        }

        private static long EncodeMessageToInteger(string message, int p)
        {
            // Use Windows-1255 encoding (provider is registered in test startup)
            var enc = Encoding.GetEncoding(1255);
            var bytes = enc.GetBytes(message);

            // Interpret bytes as big-endian integer modulo p
            long x0 = 0;
            foreach (var b in bytes)
            {
                x0 = (x0 * 256 + b) % p;
            }

            return x0;
        }

        private static IECPoint<PrimeFieldElement> ClearCofactor(IEllipticCurve<PrimeFieldElement> curve, IECPoint<PrimeFieldElement> p)
        {
            /*
             * Cofactor clearing: H(m) = (|G|/r) * P_temp, where |G| is the order of the group of points on the curve and r is the prime order of the subgroup.
             * This ensures that the resulting point H(m) lies in the subgroup of order r, as it's order is r (prime).
             */
            var cofactor = curve.GroupOrder / curve.R;
            var projectedPoint = p.Multiply(cofactor);
            return projectedPoint;
        }
    }
}
