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

        /// Converts a BigInteger to its binary representation as a bit array.
        /// Returns bits array s.t index 0 = LSB.
        public static int[] GetBinaryBits(BigInteger value)
        {
            if (value <= 0)
            {
                return new int[] { 0 };
            }

            var bitCount = (int)value.GetBitLength();
            var bits = new int[bitCount];

            for (int i = 0; i < bitCount; i++)
            {
                bits[i] = (value & (BigInteger.One << i)) != 0 ? 1 : 0;
            }

            return bits;
        }

        /// <summary>
        /// Generates a random BigInteger in the range [0, max).
        /// </summary>
        public static BigInteger RandomBigInteger(Random random, BigInteger max)
        {
            if (max <= int.MaxValue)
            {
                return random.Next((int)max);
            }

            // For larger values, generate random bytes
            byte[] bytes = max.ToByteArray();
            BigInteger result;
            do
            {
                random.NextBytes(bytes);
                bytes[bytes.Length - 1] &= 0x7F; // Ensure positive
                result = new BigInteger(bytes);
            } while (result >= max);

            return result;
        }

        /// <summary>
        /// Attempts to find square root of an element in extension field using Tonelli-Shanks algorithm.
        /// Returns null if no square root exists.
        /// </summary>
        public static ExtensionFieldElement? SqrtModExtensionField(ExtensionFieldElement a, ExtensionField field, Random random)
        {
            BigInteger q = field.BaseField.Characteristic;
            int k = field.ExtensionDegree;
            BigInteger qk = BigInteger.Pow(q, k);

            // For fields where q^k ≡ 3 (mod 4), use the simple formula
            if (qk % 4 == 3)
            {
                BigInteger exponent = (qk + 1) / 4;
                var candidate = a.Power(exponent);

                // Verify candidate² = a
                var squared = candidate.Multiply(candidate);
                if (squared.Equals(a))
                {
                    return candidate;
                }
                return null;
            }

            // For other cases, use Tonelli-Shanks algorithm
            return TonelliShanksExtensionField(a, field, random);
        }

        /// <summary>
        /// Tonelli-Shanks algorithm for finding square roots in extension fields.
        /// </summary>
        private static ExtensionFieldElement? TonelliShanksExtensionField(ExtensionFieldElement a, ExtensionField field, Random random)
        {
            BigInteger q = field.BaseField.Characteristic;
            int k = field.ExtensionDegree;
            BigInteger qk = BigInteger.Pow(q, k);

            // Check if a is a quadratic residue
            BigInteger legendreExp = (qk - 1) / 2;
            var legendre = a.Power(legendreExp);

            if (!legendre.Equals(field.One))
            {
                return null; // Not a quadratic residue
            }

            // Find Q and S such that qk - 1 = Q * 2^S with Q odd
            BigInteger Q = qk - 1;
            int S = 0;
            while (Q % 2 == 0)
            {
                Q /= 2;
                S++;
            }

            // Find a quadratic non-residue
            ExtensionFieldElement z = FindQuadraticNonResidue(field, random);

            // Initialize
            BigInteger M = S;
            var c = z.Power(Q);
            var t = a.Power(Q);
            var R = a.Power((Q + 1) / 2);

            while (!t.Equals(field.One))
            {
                // Find the least i such that t^(2^i) = 1
                int i = 1;
                var temp = t;
                for (; i < M; i++)
                {
                    temp = temp.Multiply(temp);
                    if (temp.Equals(field.One))
                        break;
                }

                if (i >= M)
                {
                    return null; // Should not happen if a is QR
                }

                // Update values
                var b = c;
                for (int j = 0; j < M - i - 1; j++)
                {
                    b = b.Multiply(b);
                }

                M = i;
                c = b.Multiply(b);
                t = t.Multiply(c);
                R = R.Multiply(b);
            }

            return R;
        }

        /// <summary>
        /// Finds a quadratic non-residue in the extension field.
        /// </summary>
        private static ExtensionFieldElement FindQuadraticNonResidue(ExtensionField field, Random random)
        {
            BigInteger q = field.BaseField.Characteristic;
            int k = field.ExtensionDegree;
            BigInteger qk = BigInteger.Pow(q, k);
            BigInteger legendreExp = (qk - 1) / 2;

            // Try random elements until we find a non-residue
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                var coeffs = new BigInteger[k];
                for (int i = 0; i < k; i++)
                {
                    coeffs[i] = RandomBigInteger(random, q);
                }

                var poly = new Polynomial(q, coeffs);
                var candidate = new ExtensionFieldElement(field, poly);

                if (candidate.IsZero)
                    continue;

                var legendre = candidate.Power(legendreExp);
                if (!legendre.Equals(field.One))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Failed to find quadratic non-residue in extension field");
        }
    }
}
