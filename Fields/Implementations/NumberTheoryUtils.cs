using System;
using System.Collections.Generic;
using System.Numerics;

namespace BLS.Fields.Implementations
{
    public static class NumberTheoryUtils
    {
        public static BigInteger ModNormalize(BigInteger value, BigInteger modulus)
        {
            if (modulus <= 0) throw new ArgumentException("Modulus must be positive.", nameof(modulus));
            BigInteger r = value % modulus;
            if (r < 0) r += modulus;
            return r;
        }

        public static List<KeyValuePair<BigInteger,int>> Factorize(BigInteger n)
        {
            var res = new List<KeyValuePair<BigInteger,int>>();
            if (n <= 1) return res;

            BigInteger m = n;
            int count = 0;
            while (m % 2 == 0)
            {
                count++; m /= 2;
            }
            if (count > 0) res.Add(new KeyValuePair<BigInteger,int>(2, count));

            BigInteger f = 3;
            while (f * f <= m)
            {
                count = 0;
                while (m % f == 0)
                {
                    count++; m /= f;
                }
                if (count > 0) res.Add(new KeyValuePair<BigInteger,int>(f, count));
                f += 2;
            }
            if (m > 1) res.Add(new KeyValuePair<BigInteger,int>(m, 1));
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
                    while (t % p == 0) t /= p;
                }
            }
            if (t > 1) res.Add(t);
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
    }
}
