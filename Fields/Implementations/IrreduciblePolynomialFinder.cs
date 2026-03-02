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
        public static Polynomial FindIrreduciblePolynomial(PrimeField field, BigInteger order, int maxDegreeSearch = 50)
        {
            ValidateFieldAndOrder(field, order);
            int characteristic = field.Characteristic;
            int degree = FindEmbeddingDegree(order, maxDegreeSearch, characteristic);
            int maxAttempts = ValidateMaxAttemptsRange(characteristic, degree);
            var distinctPrimeFactors = GetPrimeDivisors(degree);
            var characteristicToDegree = BigInteger.Pow(new BigInteger(characteristic), degree);
            var x = Polynomial.X(characteristic);

            for (int candidateValue = 0; candidateValue < maxAttempts; candidateValue++)
            {
                var coefficients = new int[degree + 1];
                int remainder = candidateValue;
                for (int i = 0; i < degree; i++)
                {
                    coefficients[i] = remainder % characteristic;
                    remainder /= characteristic;
                }
                coefficients[degree] = 1; // Ensure monic polynomial

                var candidatePoly = new Polynomial(characteristic, coefficients);

                // (1) Check if candidatePoly divides x^{p^d} - x
                var xPowerPd = Polynomial.PowMod(x, characteristicToDegree, candidatePoly);
                if (!Polynomial.Sub(xPowerPd, x).IsZero) continue;

                bool isIrreducible = true;
                foreach (var q in distinctPrimeFactors)
                {
                    int subDegree = degree / q;
                    var characteristicToSubDegree = BigInteger.Pow(new BigInteger(characteristic), subDegree);

                    var xPowerSubDegree = Polynomial.PowMod(x, characteristicToSubDegree, candidatePoly);
                    var commonFactorTerm = Polynomial.Sub(xPowerSubDegree, x);

                    // If gcd(candidatePoly, x^{p^{d/q}} - x) != 1, it has a factor in a subfield
                    if (commonFactorTerm.IsZero) { isIrreducible = false; break; }

                    var gcd = Polynomial.Gcd(candidatePoly, commonFactorTerm);
                    if (!IsUnity(gcd)) { isIrreducible = false; break; }
                }

                if (isIrreducible) return candidatePoly;
            }

            throw new InvalidOperationException(
                $"No irreducible polynomial of degree {degree} found over F_{characteristic}.");
        }

        private static int ValidateMaxAttemptsRange(int characteristic, int degree)
        {
            // Enumeration limit check
            BigInteger totalCandidates = BigInteger.Pow(new BigInteger(characteristic), degree);
            if (totalCandidates > new BigInteger(int.MaxValue))
                throw new InvalidOperationException($"Search space too large: p^d = {totalCandidates}");

            int maxAttempts = (int)totalCandidates;
            return maxAttempts;
        }

        private static void ValidateFieldAndOrder(PrimeField field, BigInteger order)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (order <= 1) throw new ArgumentException("Order must be > 1", nameof(order));
        }

        private static int FindEmbeddingDegree(BigInteger r, int maxDegreeSearch, int p)
        {
            // find embedding degree k: smallest k>=1 s.t. r | (p^k - 1)
            int k = -1;
            for (int cand = 1; cand <= maxDegreeSearch; cand++)
            {
                var p_pow_k = BigInteger.Pow(new BigInteger(p), cand);
                if ((p_pow_k - 1) % r == 0)
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

        private static bool IsUnity(Polynomial poly)
        {
            return poly != null && !poly.IsZero && poly.Degree == 0 && poly[0] == 1;
        }
    }
}
