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

                if (Field.ExtensionDegree == 1)
                {
                    // Prime field: enumerate points using Euler's criterion
                    _groupOrder = ComputeGroupOrderForPrimeField();
                }
                else
                {
                    // Extension field: use Frobenius trace recurrence
                    _groupOrder = ComputeGroupOrderForExtensionField();
                }

                return _groupOrder.Value;
            }
        }

        private BigInteger ComputeGroupOrderForPrimeField()
        {
            BigInteger characteristic = Field.Characteristic;
            BigInteger totalPoints = 1; // include the point at infinity

            // Euler's criterion: for a non-zero field element z in F_p (p odd),
            // z^{(p-1)/2} = 1 (mod p) iff z is a quadratic residue (has two square roots),
            // and z^{(p-1)/2} = -1 (mod p) iff z is a non-residue (no square roots).
            // We compute z = x^3 + A*x + B for every x in F_p and use the criterion
            // to decide whether to add 0, 1 or 2 points for that x.
            long residueExponent = (long)((characteristic - 1) / 2);

            for (BigInteger primeFieldElement = 0; primeFieldElement < characteristic; primeFieldElement++)
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

            return totalPoints;
        }

        private BigInteger ComputeGroupOrderForExtensionField()
        {
            // This method requires that the curve is defined over an extension field F_{q^k}
            // where the base field is F_q (a prime field).
            // We use the Frobenius trace recurrence to compute #E(F_{q^k})
            // As presented in the paper Finding a point in E[r]


            var extensionField = Field as BLS.Fields.Implementations.ExtensionField;
            if (extensionField == null)
            {
                throw new InvalidOperationException("Extension field group order calculation requires ExtensionField type.");
            }

            var baseField = extensionField.BaseField;
            BigInteger q = baseField.Characteristic;
            int k = extensionField.ExtensionDegree;

            // First, we need the group order over the base prime field to compute the Frobenius trace
            // Create the same curve over the base field
            var baseCurveA = baseField.FromInt(GetCoefficientAsInteger(A));
            var baseCurveB = baseField.FromInt(GetCoefficientAsInteger(B));
            var baseCurve = new EllipticCurve<BLS.Fields.Implementations.PrimeFieldElement>(baseField, baseCurveA, baseCurveB);

            BigInteger baseGroupOrder = baseCurve.GroupOrder;

            // Step 1: Compute Frobenius trace t = q + 1 - #E(F_q)
            BigInteger t = q + 1 - baseGroupOrder;

            // Step 3: Compute #E(F_{q^k}) using recurrence relation
            // a_n = t*a_{n-1} - q*a_{n-2}, with a_0 = 2, a_1 = t
            // Then N_k = q^k + 1 - a_k
            BigInteger a_prev = 2;        // a_0
            BigInteger a_curr = t;        // a_1

            for (int n = 2; n <= k; n++)
            {
                BigInteger a_next = t * a_curr - q * a_prev;
                a_prev = a_curr;
                a_curr = a_next;
            }

            BigInteger N_k = BigInteger.Pow(q, k) + 1 - a_curr;
            return N_k;
        }

        private BigInteger GetCoefficientAsInteger(T element)
        {
            // For extension field elements, we need the constant term (coefficient of x^0)
            if (element is BLS.Fields.Implementations.ExtensionFieldElement extElement)
            {
                return extElement.Poly[0];
            }
            // For prime field elements
            if (element is BLS.Fields.Implementations.PrimeFieldElement primeElement)
            {
                return primeElement.Value;
            }
            throw new InvalidOperationException("Unsupported field element type.");
        }

        public IReadOnlyList<(BigInteger Prime, int Power)> GroupOrderFactors
        {
            get
            {
                if (_factors != null)
                {
                    return _factors;
                }
                _factors = Factor(GroupOrder);
                return _factors;
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
