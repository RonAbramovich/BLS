using System;
using System.Collections.Generic;
using System.Numerics;

namespace BLS.Fields.Implementations
{
    public static class NumberTheoryUtils
    {
        public static int ModNormalize(long value, int modulus)
        {
            if (modulus <= 0) throw new ArgumentException("Modulus must be positive.", nameof(modulus));
            long r = value % modulus;
            if (r < 0) r += modulus;
            return (int)r;
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
    }
}
