using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace BLS.Fields.Implementations
{
    /// <summary>
    /// Finds an irreducible polynomial g(x) in F_p[x] of degree k where k is the embedding degree of r wrt p.
    /// Uses Rabin's irreducibility test. This is a brute-force enumerator: it tries monic polynomials of degree k.
    /// </summary>
    public static class IrreduciblePolynomialFinder
    {
        /// <summary>
        /// Find the smallest embedding degree k such that r | (p^k - 1) and then search for a monic irreducible polynomial of degree k.
        /// Returns the polynomial if found; throws InvalidOperationException if no polynomial is found within the search limits.
        /// </summary>
        public static Polynomial FindIrreduciblePolynomial(PrimeField field, BigInteger r, int maxDegreeSearch = 50)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (r <= 1) throw new ArgumentException("r must be a prime or integer > 1", nameof(r));

            int p = field.Characteristic;
            int k = FindEmbeddingDegree(r, maxDegreeSearch, p);

            // prepare monic polynomial enumeration of degree k
            // there are p^k candidates for coefficients of x^{0..k-1}
            BigInteger total = BigInteger.Pow(new BigInteger(p), k);
            if (total > new BigInteger(int.MaxValue))
            {
                // avoid huge enumeration
                throw new InvalidOperationException($"Too many candidate polynomials to enumerate: p^{k} = {total}");
            }

            int candidates = (int)total;

            // prime divisors of k for Rabin's test
            var primeDivs = GetPrimeDivisors(k);

            var xPoly = Polynomial.X(p);
            var pkExp = BigInteger.Pow(new BigInteger(p), k);

            for (int idx = 0; idx < candidates; idx++)
            {
                // coefficients for degrees 0..k-1 in base-p representation of idx
                var coeffs = new int[k + 1]; // degree k polynomial: coeffs[0..k-1] are digits, coeffs[k]=1 (monic)
                int tmp = idx;
                for (int i = 0; i < k; i++)
                {
                    coeffs[i] = tmp % p;
                    tmp /= p;
                }
                coeffs[k] = 1; // leading coeff
                var g = new Polynomial(p, coeffs);

                // Rabin test
                // 1) g divides x^{p^k} - x  <=> x^{p^k} mod g == x mod g
                var xpk = Polynomial.PowMod(xPoly, pkExp, g);
                var diff = Polynomial.Sub(xpk, xPoly);
                if (!diff.IsZero) continue; // fails first condition

                bool ok = true;
                foreach (var d in primeDivs)
                {
                    int kd = k / d;
                    var exp = BigInteger.Pow(new BigInteger(p), kd);
                    var xp = Polynomial.PowMod(xPoly, exp, g);
                    var gcd = Polynomial.Gcd(g, Polynomial.Sub(xp, xPoly));
                    if (!gcd.IsZero && gcd.Degree >= 0)
                    {
                        // if gcd != 1 then reducible
                        if (!(gcd.Degree == 0 && gcd[0] == 1))
                        {
                            ok = false;
                            break;
                        }
                    }
                }

                if (ok)
                {
                    // found irreducible polynomial
                    return g;
                }
            }

            throw new InvalidOperationException($"No irreducible polynomial of degree {k} found over F_{p} after enumerating {candidates} candidates.");
        }

        private static int FindEmbeddingDegree(BigInteger r, int maxDegreeSearch, int p)
        {
            // find embedding degree k: smallest k>=1 s.t. r | (p^k - 1)
            int k = -1;
            for (int cand = 1; cand <= maxDegreeSearch; cand++)
            {
                var pk = BigInteger.Pow(new BigInteger(p), cand);
                if ((pk - 1) % r == 0)
                {
                    k = cand;
                    break;
                }
            }
            if (k == -1) throw new InvalidOperationException($"Embedding degree not found up to {maxDegreeSearch}");
            return k;
        }

        private static List<int> GetPrimeDivisors(int n)
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
