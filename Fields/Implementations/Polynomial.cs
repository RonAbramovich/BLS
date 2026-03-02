using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace BLS.Fields.Implementations
{
    /// <summary>
    /// Simple polynomial over a prime field F_p. Coefficients are stored from lowest degree to highest.
    /// Minimal functionality required for Rabin irreducibility test: add, mul, div-rem, gcd, powmod.
    /// </summary>
    public class Polynomial
    {
        [Description("coefficients[i] is coefficient for x^i")]
        private int[] _coefficients;
        public int Modulus { get; }

        public Polynomial(int modulus, params int[] coefficients)
        {
            if (modulus <= 1)
            {
                throw new ArgumentException("Modulus must be a prime > 1", nameof(modulus));
            }
            Modulus = modulus;
            _coefficients = [.. (coefficients ?? Array.Empty<int>()).Select(c => Modulu(c))];
            Trim();
        }

        public int Degree => _coefficients.Length == 0 ? -1 : _coefficients.Length - 1;

        public bool IsZero => Degree == -1;

        public int this[int i] //Easy retriever for the ith coefficient
        {
            get
            {
                if (i < 0 || i >= _coefficients.Length) return 0;
                return _coefficients[i];
            }
        }

        public static Polynomial X(int p)
        {
            return new Polynomial(p, 0, 1); // (0,1) corresponds to the polynomial f(x)=x 
        }

        public Polynomial Clone()
        {
            return new Polynomial(Modulus, _coefficients.Select(c => c).ToArray());
        }

        public static Polynomial Add(Polynomial a, Polynomial b)
        {
            if (a.Modulus != b.Modulus)
            {
                throw new ArgumentException("Modulus mismatch");
            }

            int p = a.Modulus;
            int max = Math.Max(a.Degree, b.Degree);
            var res = new int[max + 1];
            for (int i = 0; i <= max; i++)
            {
                res[i] = (a[i] + b[i]) % p;
            }
            return new Polynomial(p, res);
        }

        public static Polynomial Sub(Polynomial a, Polynomial b)
        {
            if (a.Modulus != b.Modulus)
            {
                throw new ArgumentException("Modulus mismatch");
            }

            int p = a.Modulus;
            int max = Math.Max(a.Degree, b.Degree);
            var res = new int[max + 1];
            for (int i = 0; i <= max; i++)
            {
                res[i] = (a[i] - b[i]) % p;    
                if (res[i] < 0)
                {
                    res[i] += p;
                }
            }

            return new Polynomial(p, res);
        }

        public static Polynomial Mul(Polynomial a, Polynomial b)
        {
            if (a.Modulus != b.Modulus)
            {
                throw new ArgumentException("Modulus mismatch");
            }

            int p = a.Modulus;
            if (a.IsZero || b.IsZero)
            {
                return new Polynomial(p);
            }

            var res = new int[a.Degree + b.Degree + 1];
            for (int i = 0; i <= a.Degree; i++)
            {
                if (a[i] == 0)
                {
                    continue;
                }

                for (int j = 0; j <= b.Degree; j++)
                {
                    res[i + j] = (res[i + j] + a[i] * b[j]) % p;
                }
            }
            return new Polynomial(p, res);
        }

        /// <summary>
        /// Polynomial division with remainder: Ron TODO : Improve.
        /// </summary>
        public static (Polynomial Quotient, Polynomial Remainder) DivRem(Polynomial a, Polynomial b)
        {
            if (a.Modulus != b.Modulus)
            {
                throw new ArgumentException("Modulus mismatch");
            }

            int p = a.Modulus;
            if (b.IsZero)
            {
                throw new DivideByZeroException();
            }

            var rCoeffs = a._coefficients.Select(c => c).ToList();
            int degB = b.Degree;
            var quotient = new int[Math.Max(0, a.Degree - degB + 1)];
            int invLeading = ModInverse(b[b.Degree], p);
            for (int i = a.Degree; i >= b.Degree; i--)
            {
                int coef = rCoeffs.Count > i ? rCoeffs[i] : 0;
                if (coef == 0) continue;
                int qcoef = (int)((long)coef * invLeading % p);
                quotient[i - degB] = qcoef;
                // subtract qcoef * b * x^{i-degB}
                for (int j = 0; j <= degB; j++)
                {
                    int idx = j + i - degB;
                    rCoeffs[idx] = (rCoeffs[idx] - qcoef * b[j]) % p;
                    if (rCoeffs[idx] < 0) rCoeffs[idx] += p;
                }
            }
            var qpoly = new Polynomial(p, quotient);
            // remainder is rCoeffs[0..degB-1]
            var rem = rCoeffs.Take(Math.Min(rCoeffs.Count, degB)).ToArray();
            var rpoly = new Polynomial(p, rem);
            return (qpoly, rpoly);
        }

        public static Polynomial Mod(Polynomial a, Polynomial modulus)
        {
            return DivRem(a, modulus).Remainder;
        }

        // Fast exponentiation mod a polynomial: Ron TODO : Improve.
        public static Polynomial PowMod(Polynomial value, BigInteger exponent, Polynomial mod)
        {
            if (value.Modulus != mod.Modulus) throw new ArgumentException("Modulus mismatch");
            int p = value.Modulus;
            var result = new Polynomial(p, 1); // 1
            var basePoly = value.Clone();
            while (exponent > 0)
            {
                if ((exponent & 1) == 1)
                {
                    result = Mod(Mul(result, basePoly), mod);
                }
                basePoly = Mod(Mul(basePoly, basePoly), mod);
                exponent >>= 1;
            }
            return result;
        }

        public static Polynomial Gcd(Polynomial a, Polynomial b)
        {
            if (a.Modulus != b.Modulus) throw new ArgumentException("Modulus mismatch");
            // Euclidean algorithm
            var A = a.Clone();
            var B = b.Clone();
            while (!B.IsZero)
            {
                var r = DivRem(A, B).Remainder;
                A = B;
                B = r;
            }
            // Make monic
            if (A.IsZero) return A;
            int lead = A[A.Degree];
            int invLead = ModInverse(lead, A.Modulus);
            var coeffs = new int[A.Degree + 1];
            for (int i = 0; i <= A.Degree; i++) coeffs[i] = (int)((long)A[i] * invLead % A.Modulus);
            return new Polynomial(A.Modulus, coeffs);
        }

        private static int ModInverse(int a, int mod)
        {
            // extended euclidean for small modulus
            int t = 0, newt = 1;
            int r = mod, newr = a % mod;
            while (newr != 0)
            {
                int q = r / newr;
                (t, newt) = (newt, t - q * newt);
                (r, newr) = (newr, r - q * newr);
            }
            if (r > 1) throw new ArgumentException("a is not invertible");
            if (t < 0) t += mod;
            return t;
        }

        public override string ToString()
        {
            if (IsZero) return "0";
            var parts = new List<string>();
            for (int i = Degree; i >= 0; i--)
            {
                int c = this[i];
                if (c == 0) continue;
                if (i == 0) parts.Add(c.ToString());
                else if (i == 1) parts.Add(c == 1 ? "x" : c + "x");
                else parts.Add(c == 1 ? $"x^{i}" : c + $"x^{i}");
            }
            return string.Join(" + ", parts);
        }

        #region privateHelpers
        private void Trim()
        {
            int d = _coefficients.Length - 1;
            while (d >= 0 && _coefficients[d] == 0)
            {
                d--;
            }

            if (d + 1 != _coefficients.Length)
            {
                Array.Resize(ref _coefficients, Math.Max(d + 1, 0)); // Fix Coefficients list
            }
        }

        private int Modulu(long v)
        {
            var r = v % Modulus;
            if (r < 0)
            {
                r += Modulus;
            }

            return (int)r;
        }
        #endregion
    }
}
