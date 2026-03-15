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

        /// <summary>
        /// Compute modular square root for primes p where p % 4 == 3 using exponentiation shortcut.
        /// Returns -1 when no square root exists. Assumes p is an odd prime and p % 4 == 3.
        /// </summary>
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

        /// <summary>
        /// Converts a BigInteger to its binary representation as a bit array.
        /// Returns bits array s.t index 0 = LSB.
        /// </summary>
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
        /// Computes square root of element 'a' in extension field F_q^k using Tonelli-Shanks algorithm.
        /// Returns null if 'a' is not a quadratic residue.
        /// This is the extension field analogue of SqrtModP for base fields.
        /// </summary>
        /// <param name="a">Element to find square root of</param>
        /// <param name="field">The extension field F_q^k</param>
        /// <param name="random">Random number generator for finding non-residues (used only when q^k ≡ 1 mod 4)</param>
        /// <returns>Square root of 'a' if it exists, null otherwise</returns>
        public static ExtensionFieldElement? SqrtModExtensionField(
            ExtensionFieldElement a,
            ExtensionField field,
            Random random)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(random);

            if (a.IsZero)
            {
                return field.Zero;
            }

            BigInteger q = field.BaseField.Characteristic;
            int k = field.ExtensionDegree;
            BigInteger fieldSize = BigInteger.Pow(q, k);

            // Check if 'a' is a quadratic residue using Legendre symbol: a^((q^k - 1)/2) == 1
            BigInteger legendreExp = (fieldSize - 1) / 2;
            var legendre = a.Power(legendreExp);
            if (!legendre.Equals(field.One))
            {
                return null; // Not a quadratic residue
            }

            // Special case: q^k ≡ 3 (mod 4) - simple formula
            // sqrt(a) = a^((q^k + 1)/4)
            if (fieldSize % 4 == 3)
            {
                BigInteger exp = (fieldSize + 1) / 4;
                return a.Power(exp);
            }

            // General case: Tonelli-Shanks algorithm for q^k ≡ 1 (mod 4)
            // Write q^k - 1 = 2^s * t with t odd
            BigInteger qkMinus1 = fieldSize - 1;
            int s = 0;
            BigInteger t = qkMinus1;
            while (t % 2 == 0)
            {
                s++;
                t /= 2;
            }

            // Find a quadratic non-residue n
            var n = FindQuadraticNonResidue(field, random);
            if (n == null)
            {
                return null; // Failed to find non-residue (very unlikely)
            }

            // Tonelli-Shanks initialization
            var c = n.Power(t);              // c = n^t
            var r = a.Power((t + 1) / 2);    // r = a^((t+1)/2)
            var tt = a.Power(t);             // tt = a^t
            int m = s;

            // Tonelli-Shanks main loop
            while (!tt.Equals(field.One))
            {
                // Find the least i such that tt^(2^i) = 1
                int i = 1;
                var temp = tt;
                while (i < m)
                {
                    temp = temp.Multiply(temp);
                    if (temp.Equals(field.One))
                    {
                        break;
                    }
                    i++;
                }

                if (i >= m)
                {
                    return null; // Algorithm failed (should not happen if a is a QR)
                }

                // Update values
                var b = c;
                for (int j = 0; j < m - i - 1; j++)
                {
                    b = b.Multiply(b);
                }

                r = r.Multiply(b);      // r = r * b
                c = b.Multiply(b);      // c = b^2
                tt = tt.Multiply(c);    // tt = tt * c
                m = i;
            }

            return r;
        }

        /// <summary>
        /// Finds a quadratic non-residue in the extension field F_q^k by random search.
        /// A quadratic non-residue 'a' satisfies: a^((q^k - 1)/2) ≠ 1 (mod irreducible polynomial).
        /// Used in Tonelli-Shanks algorithm for finding square roots in extension fields.
        /// </summary>
        /// <param name="field">The extension field F_q^k</param>
        /// <param name="random">Random number generator for candidate selection</param>
        /// <param name="maxAttempts">Maximum number of random attempts (default 100)</param>
        /// <returns>A quadratic non-residue element, or null if not found within maxAttempts</returns>
        public static ExtensionFieldElement? FindQuadraticNonResidue(
            ExtensionField field, 
            Random random, 
            int maxAttempts = 100)
        {
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(random);

            BigInteger q = field.BaseField.Characteristic;
            int k = field.ExtensionDegree;
            BigInteger fieldSize = BigInteger.Pow(q, k);
            BigInteger legendreExp = (fieldSize - 1) / 2;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Generate random coefficients for polynomial
                var coeffs = new BigInteger[k];
                for (int i = 0; i < k; i++)
                {
                    coeffs[i] = RandomBigInteger(random, q);
                }

                var candidatePoly = new Polynomial(q, coeffs);
                var candidate = new ExtensionFieldElement(field, candidatePoly);

                if (!candidate.IsZero)
                {
                    // Check Legendre symbol: candidate^((q^k - 1)/2)
                    var legendre = candidate.Power(legendreExp);
                    if (!legendre.Equals(field.One))
                    {
                        return candidate; // Found a non-residue
                    }
                }
            }

            return null; // Failed to find non-residue within maxAttempts
        }

        /// <summary>
        /// Generates a random BigInteger in the range [0, max).
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="max">Exclusive upper bound</param>
        /// <returns>Random BigInteger in [0, max)</returns>
        public static BigInteger RandomBigInteger(Random random, BigInteger max)
        {
            ArgumentNullException.ThrowIfNull(random);

            if (max <= 0)
            {
                throw new ArgumentException("Max must be positive", nameof(max));
            }

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
    }
}
