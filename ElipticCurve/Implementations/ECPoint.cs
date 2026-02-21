using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Interfaces;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace BLS.ElipticCurve.Implementations
{
    public class ECPoint<T> : IECPoint<T> where T : IFieldElement<T>
    {
        private BigInteger _order;

        public IEllipticCurve<T> Curve { get; }
        public bool IsInfinity { get; }
        public T X { get; }
        public T Y { get; }
        public BigInteger Order
        {
            get
            {
                if (_order != 0) return _order;

                if (IsInfinity)
                {
                    _order = 1;
                    return _order;
                }

                // Reduces the group order to the element's order by "trimming" redundant prime factors.
                // If (ord/p)*P != Infinity, the remaining power of p is necessary for the order, otherwise we can reduce ord by p.
                var groupOrder = Curve.GroupOrder;
                var factors = Curve.GroupOrderFactors;
                BigInteger ord = groupOrder;

                foreach (var factor in factors)
                {
                    var p = factor.Prime;
                    var e = factor.Power;

                    for (int i = 0; i < e; i++)
                    {
                        var candidate = ord / p;
                        var multiple = Multiply(candidate);
                        if (multiple.IsInfinity)
                        {
                            ord = candidate;
                        }
                        else
                        {
                            break; // cannot reduce further by this prime
                        }
                    }
                }

                _order = ord;
                return _order;
            }
        }

        
        public ECPoint(IEllipticCurve<T> curve)
        {
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            IsInfinity = true;
            X = default!;
            Y = default!;
        }

        public ECPoint(IEllipticCurve<T> curve, T x, T y)
        {
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            X = x ?? throw new ArgumentNullException(nameof(x));
            Y = y ?? throw new ArgumentNullException(nameof(y));
            IsInfinity = false;
        }

        public IECPoint<T> Negate()
        {
            if (IsInfinity)
            {
                return Curve.Infinity;
            }

            return Curve.CreatePoint(X, -Y);
        }

        public IECPoint<T> Add(IECPoint<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (IsInfinity)
            {
                return other;
            }

            if (other.IsInfinity)
            {
                return this;
            }

            // If x1 == x2 - This is infinite slope case or points are the same
            if (X.Equals(other.X))
            {
                // y1 == -y2 ?
                if (Y.Equals(-other.Y))
                {
                    return Curve.Infinity;
                }
                else // points are the same -> double
                {
                    return Double();
                }
            }

            return CalculateSumFromFiniteSlope(other);
        }


        public IECPoint<T> Double()
        {
            if (IsInfinity)
            {
                return Curve.Infinity;
            }

            // If y == 0 => tangent is vertical -> point at infinity
            if (Y.IsZero) return Curve.Infinity;

            // lambda = (3*x^2 + A) / (2*y)
            var x2 = X.Power(2);
            var threeX2 = x2 + x2 + x2;
            var numerator = threeX2 + Curve.A;
            var denominator = Y + Y;
            var slope = numerator / denominator;

            var x3 = slope * slope - X - X;
            var y3 = slope * (X - x3) - Y;

            return Curve.CreatePoint(x3, y3);
        }

        public IECPoint<T> Multiply(BigInteger k)
        {
            if (k == 0)
            {
                return Curve.Infinity;
            }
            if (k < 0)
            {
                return Negate().Multiply(BigInteger.Negate(k));
            }

            // Double-and-add algorithm
            var result = Curve.Infinity;
            var addend = (IECPoint<T>)this;

            var bits = k;
            while (bits > 0)
            {
                if (!bits.IsEven)
                {
                    result = result.Add(addend);
                }
                addend = addend.Double();
                bits >>= 1;
            }

            return result;
        }

        public bool Equals(IECPoint<T> other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            if (IsInfinity && other.IsInfinity) return true;
            if (IsInfinity || other.IsInfinity) return false;

            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IECPoint<T>);
        }

        public override int GetHashCode()
        {
            if (IsInfinity) return 0;
            return HashCode.Combine(X, Y);
        }
        
        private IECPoint<T> CalculateSumFromFiniteSlope(IECPoint<T> other)
        {
            // slope: (y2 - y1) / (x2 - x1)
            var numerator = other.Y - Y;
            var denominator = other.X - X;
            var slope = numerator / denominator;

            var x3 = slope * slope - X - other.X;
            var y3 = slope * (X - x3) - Y;

            return Curve.CreatePoint(x3, y3);
        }
    }
}
