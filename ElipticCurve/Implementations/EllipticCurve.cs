using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BLS.ElipticCurve.Implementations
{
   
    public class EllipticCurve<T> : IEllipticCurve<T> where T : IFieldElement<T>
    {
        private BigInteger? _groupOrder;
        private List<(BigInteger Prime, int Power)>? _factors;
        private IECPoint<T>? _infinity;
        private BigInteger? _largestPrimeDivisor;

        public IField<T> Field { get; }

        public T A { get; }
        public T B { get; }

        public EllipticCurve(IField<T> field, T a, T b)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            A = a ?? throw new ArgumentNullException(nameof(a));
            B = b ?? throw new ArgumentNullException(nameof(b));
            ValidateSmoothCurve();
        }

        public BigInteger GroupOrder
        {
            get
            {
                if (_groupOrder.HasValue) return _groupOrder.Value;

                // For extension degree != 1 a different counting method is required. In BLS ALgorithm we need only the group order of EC over prime field.
                if (Field.ExtensionDegree != 1)
                {
                    throw new NotSupportedException("Group order enumeration is supported only for prime fields (extension degree 1). Provide GroupOrder manually for extension fields.");
                }

                int characteristic = Field.Characteristic;
                BigInteger totalPoints = 1; // include the point at infinity

                // Euler's criterion: for a non-zero field element z in F_p (p odd),
                // z^{(p-1)/2} = 1 (mod p) iff z is a quadratic residue (has two square roots),
                // and z^{(p-1)/2} = -1 (mod p) iff z is a non-residue (no square roots).
                // We compute z = x^3 + A*x + B for every x in F_p and use the criterion
                // to decide whether to add 0, 1 or 2 points for that x.
                long residueExponent = (characteristic - 1) / 2L;

                for (int primeFieldElement = 0; primeFieldElement < characteristic; primeFieldElement++)
                {
                    var x = Field.FromInt(primeFieldElement);
                    var rhs = x.Power(3) + A * x + B; // right hand side: x^3 + A*x + B

                    if (rhs.IsZero)
                    {
                        // y^2 = 0 has exactly one solution y = 0
                        totalPoints += 1;
                        continue;
                    }

                    // Use Euler's criterion for odd characteristic
                    var legendre = rhs.Power(residueExponent);
                    if (legendre.Equals(Field.One))
                    {
                        // Quadratic residue -> two distinct y values
                        totalPoints += 2;
                    }
                    // non-residue -> add 0
                }

                _groupOrder = totalPoints;
                // compute and cache prime factorization of the group order
                _factors = Factor(_groupOrder.Value);
                return _groupOrder.Value;
            }
        }

        public IReadOnlyList<(BigInteger Prime, int Power)> GroupOrderFactors
        {
            get
            {
                // Ensure GroupOrder computed (and factors cached)
                if (_factors != null)
                {
                    return _factors;
                }
                _ = GroupOrder;
                return _factors ?? new List<(BigInteger, int)>();
            }
        }

        /// <summary>
        /// The largest prime divisor of the group order
        /// </summary>
        public BigInteger R
        {
            get
            {
                if (_largestPrimeDivisor.HasValue) return _largestPrimeDivisor.Value;

                var factors = GroupOrderFactors;
                if (factors == null || factors.Count == 0)
                {
                    _largestPrimeDivisor = 1;
                    return _largestPrimeDivisor.Value;
                }

                BigInteger maxPrime = 1;
                foreach (var f in factors)
                {
                    if (f.Prime > maxPrime) maxPrime = f.Prime;
                }

                _largestPrimeDivisor = maxPrime;
                return _largestPrimeDivisor.Value;
            }
        }

        public IECPoint<T> Infinity
        {
            get
            {
                if (_infinity == null)
                {
                    _infinity = new ECPoint<T>(this);
                }
                return _infinity;
            }
        }

        public bool IsOnCurve(IECPoint<T> p)
        {
            if (p == null)
            {
                return false;
            }
            if (p.IsInfinity)
            {
                return true;
            }

            // y^2 == x^3 + A*x + B
            var ySquared = p.Y * p.Y;
            var rightHandSide = p.X.Power(3) + A * p.X + B;
            return ySquared.Equals(rightHandSide);
        }

        public IECPoint<T> CreatePoint(T x, T y)
        {
            return new ECPoint<T>(this, x, y);
        }

        private static List<(BigInteger, int)> Factor(BigInteger n)
        {
            // Trial-division factorization producing list of (prime, exponent)
            var factors = new List<(BigInteger, int)>();
            if (n < 2)
            {
                return factors;
            }

            int factorCounter = 0;
            while ((n & 1) == 0) // while n is even
            {
                n >>= 1; // divide n by 2
                factorCounter++;
            }
            if (factorCounter > 0)
            {
                factors.Add((2, factorCounter));
            }

            BigInteger i = 3;
            while (n >= i * i) // only need to check up to sqrt(n)
            {
                if (n % i == 0)
                {
                    factorCounter = 0;
                    while (n % i == 0)
                    {
                        n /= i;
                        factorCounter++;
                    }
                    factors.Add((i, factorCounter));
                }
                i += 2;
            }

            if (n > 1) // n is prime
            {
                factors.Add((n, 1));
            }

            return factors;
        }
        private void ValidateSmoothCurve()
        {
            // Validate that the curve is smooth (non-singular).
            // For short Weierstrass form y^2 = x^3 + A*x + B over a field,
            // the curve is non-singular iff 4*A^3 + 27*B^2 != 0 in the field.
            var four = Field.FromInt(4);
            var twentySeven = Field.FromInt(27);
            var aCubed = A.Power(3);
            var bSquared = B.Power(2);
            var discriminantTerm = four * aCubed + twentySeven * bSquared;
            if (discriminantTerm.IsZero)
            {
                throw new ArgumentException("The curve is singular (not smooth): 4*A^3 + 27*B^2 == 0 over the field.", nameof(A));
            }
        }
    }
}
