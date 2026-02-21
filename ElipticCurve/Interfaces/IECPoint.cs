using BLS.Fields.Interfaces;
using System.Numerics;

namespace BLS.ElipticCurve.Interfaces
{
    public interface IECPoint<T> where T : IFieldElement<T>
    {
        IEllipticCurve<T> Curve { get; }

        bool IsInfinity { get; }
        T X { get; }                           
        T Y { get; }
        BigInteger Order { get; }

        IECPoint<T> Negate();
        IECPoint<T> Add(IECPoint<T> other);
        IECPoint<T> Double();
        IECPoint<T> Multiply(BigInteger k);
        bool Equals(IECPoint<T> other);
    }
}