using System;
using System.Numerics;

namespace BLS.Fields.Implementations
{
    /// <summary>
    /// Utility for calculating embedding degrees for pairing-based cryptography.
    /// The embedding degree k is the smallest positive integer such that r | (q^k - 1),
    /// </summary>
    public static class EmbeddingDegreeCalculator
    {
        /// <summary>
        /// Finds the embedding degree k: the smallest positive integer k such that r divides (q^k - 1).
        public static int FindEmbeddingDegree(BigInteger r, BigInteger q, int maxK = 100)
        {
            if (r < 2)
            {
                throw new ArgumentException("r must be at least 2 (should be prime)", nameof(r));
            }

            if (q < 2)
            {
                throw new ArgumentException("q must be at least 2 (should be prime)", nameof(q));
            }

            if (maxK < 2)
            {
                throw new ArgumentException("maxK must be at least 2", nameof(maxK));
            }

            // Search for smallest k >= 2 where r | (q^k - 1)
            for (int k = 2; k <= maxK; k++)
            {
                BigInteger qk_minus_1 = BigInteger.Pow(q, k) - 1;
                
                if (qk_minus_1 % r == 0)
                {
                    return k;
                }
            }

            // No suitable k found within the search limit
            return -1;
        }
    }
}
