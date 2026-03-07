using System;
using System.Numerics;
using System.Collections.Generic;

namespace BLS.Fields.Implementations
{
    public static class PolynomialUtils
    {
        // Rabin irreducibility test over F_p where p is taken from poly.Modulus
        public static bool IsIrreducible(Polynomial poly)
        {
            ArgumentNullException.ThrowIfNull(poly);
            int p = poly.Modulus;
            int d = poly.Degree;
            if (d <= 0)
            {
                return false;
            }

            var x = Polynomial.X(p);
            var pBig = new BigInteger(p);
            var xPow = Polynomial.PowMod(x, BigInteger.Pow(pBig, d), poly);
            if (!Polynomial.Sub(xPow, x).IsZero)
            {
                return false;
            }

            var primeDivisors = NumberTheoryUtils.GetPrimeDivisors(d);
            foreach (var q in primeDivisors)
            {
                int subDegree = d / q;
                var xPowSub = Polynomial.PowMod(x, BigInteger.Pow(pBig, subDegree), poly);
                var common = Polynomial.Gcd(poly, Polynomial.Sub(xPowSub, x));
                if (!(common.Degree == 0 && common[0] == 1))
                {
                    return false;
                }
            }
            return true;
        }

        // Overload that validates provided PrimeField matches the polynomial's modulus
        public static bool IsIrreducible(Polynomial poly, PrimeField field)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(poly);
            if (poly.Modulus != field.Characteristic)
            {
                throw new ArgumentException("Polynomial modulus does not match provided field.", nameof(poly));
            }

            return IsIrreducible(poly);
        }
    }
}
