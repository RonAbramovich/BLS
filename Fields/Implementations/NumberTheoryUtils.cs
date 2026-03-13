using System;
using System.Collections.Generic;
using System.Numerics;

namespace BLS.Fields.Implementations
{
    public static class NumberTheoryUtils
    {
        public static BigInteger ModNormalize(BigInteger value, BigInteger modulus)
        {
            if (modulus <= 0)
            {
                throw new ArgumentException("Modulus must be positive.", nameof(modulus));
            }

            var r = value % modulus;
            if (r < 0)
            {
                r += modulus;
            }

            return r;
        }

        public static List<KeyValuePair<BigInteger,int>> Factorize(BigInteger n)
        {
            var res = new List<KeyValuePair<BigInteger,int>>();
            if (n <= 1)
            {
                return res;
            }

            var m = n;
            int count = 0;
            while (m % 2 == 0)
            {
                count++; m /= 2;
            }

            if (count > 0)
            {
                res.Add(new KeyValuePair<BigInteger, int>(2, count));
            }

            var f = 3;
            while (f * f <= m)
            {
                count = 0;
                while (m % f == 0)
                {
                    count++;
                    m /= f;
                }
                if (count > 0)
                {
                    res.Add(new KeyValuePair<BigInteger, int>(f, count));
                }

                f += 2;
            }

            if (m > 1)
            {
                res.Add(new KeyValuePair<BigInteger, int>(m, 1));
            }

            return res;
        }

        public static List<int> GetPrimeDivisors(int n)
        {
            var res = new List<int>();
            int t = n;
            for (int p = 2; p * p <= t; p++)
            {
                if (t % p == 0)
                {
                    res.Add(p);
                    while (t % p == 0)
                    {
                        t /= p;
                    }
                }
            }

            if (t > 1)
            {
                res.Add(t);
            }

            return res;
        }

        // Compute modular square root for primes p where p % 4 == 3 using exponentiation shortcut.
        // Returns -1 when no square root exists. Assumes p is an odd prime and p % 4 == 3.
        public static BigInteger SqrtModP(BigInteger z, BigInteger p)
        {
            z = ModNormalize(z, p);
            if (z == 0) return 0;
            if (p == 2) return z;

            if (p % 4 != 3)
            {
                throw new NotSupportedException("SqrtModP currently supports only primes p where p % 4 == 3.");
            }

            // Check Legendre symbol: z^{(p-1)/2} mod p should be 1 if a square
            var leg = BigInteger.ModPow(z, (p - 1) / 2, p);
            if (leg != 1) return -1; // no square root

            // For p % 4 == 3, sqrt(z) = z^{(p+1)/4} (mod p)
            var y = BigInteger.ModPow(z, (p + 1) / 4, p);
            return y;
        }

        /// <summary>
        /// Computes modular multiplicative inverse using Fermat's Little Theorem (a^p = a modp).
        /// => By multiplying both sides in a^-2 we get a^-1 ≡ a^(p-2) (mod p).
        /// </summary>
        /// <param name="value">The value to invert</param>
        /// <param name="modulus">Prime modulus</param>
        /// <returns>Modular inverse of value mod modulus</returns>
        /// <exception cref="InvalidOperationException">If gcd(value, modulus) != 1</exception>
        public static BigInteger ModularInverse(BigInteger value, BigInteger modulus)
        {
            value = ModNormalize(value, modulus);

            if (BigInteger.GreatestCommonDivisor(value, modulus) != 1)
            {
                throw new InvalidOperationException(
                    $"Cannot compute modular inverse: gcd({value}, {modulus}) != 1");
            }

            return BigInteger.ModPow(value, modulus - 2, modulus);
        }

        public static BigInteger ModularDivide(BigInteger numerator, BigInteger denominator, BigInteger modulus)
        {
            numerator = ModNormalize(numerator, modulus);
            denominator = ModNormalize(denominator, modulus);
            var inverse = ModularInverse(denominator, modulus);
            var result = (numerator * inverse) % modulus;

            return ModNormalize(result, modulus);
        }
    }
}
