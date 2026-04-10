using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace BLS.Fields.Implementations
{
    /// <summary>
    /// Finds an irreducible polynomial g(x) in F_p[x] of degree k where k is the embedding degree of r wrt p.
    /// Uses random sampling instead of brute-force enumeration: ~1/k of all monic degree-k
    /// polynomials are irreducible, so we expect to find one in O(k) random tries regardless
    /// of field size. Irreducibility is verified via Rabin's test (see PolynomialUtils).
    /// </summary>
    public static class IrreduciblePolynomialFinder
    {
        private const int MaxRandomAttempts = 10_000;

        public static Polynomial FindIrreduciblePolynomial(PrimeField field, BigInteger order, int maxDegreeSearch = 50)
        {
            ValidateFieldAndOrder(field, order);
            BigInteger characteristic = field.Characteristic;
            int degree = FindEmbeddingDegree(order, maxDegreeSearch, characteristic);
            var random = new Random(Guid.NewGuid().GetHashCode());

            for (int attempt = 0; attempt < MaxRandomAttempts; attempt++)
            {
                // Build a random monic polynomial of the required degree:
                // free coefficients c_0..c_{k-1} are random in [0, p), leading coeff is 1.
                var coefficients = new BigInteger[degree + 1];
                for (int i = 0; i < degree; i++)
                {
                    coefficients[i] = NumberTheoryUtils.RandomBigInteger(random, characteristic);
                }
                coefficients[degree] = 1;

                var candidatePoly = new Polynomial(characteristic, coefficients);

                if (PolynomialUtils.IsIrreducible(candidatePoly, field))
                {
                    return candidatePoly;
                }
            }

            throw new InvalidOperationException(
                $"No irreducible polynomial of degree {degree} found over F_{characteristic} after {MaxRandomAttempts} random attempts.");
        }

        private static void ValidateFieldAndOrder(PrimeField field, BigInteger order)
        {
            ArgumentNullException.ThrowIfNull(field);
            if (order <= 1)
            {
                throw new ArgumentException("Order must be > 1", nameof(order));
            }
        }

        private static int FindEmbeddingDegree(BigInteger r, int maxDegreeSearch, BigInteger p)
        {
            // find embedding degree k: smallest k>=1 s.t. r | (p^k - 1)
            int k = -1;
            for (int cand = 1; cand <= maxDegreeSearch; cand++)
            {
                var p_pow_k = BigInteger.Pow(p, cand);
                if ((p_pow_k - 1) % r == 0)
                {
                    k = cand;
                    break;
                }
            }
            return k == -1 ? throw new InvalidOperationException($"Embedding degree not found up to {maxDegreeSearch}") : k;
        }
    }
}
