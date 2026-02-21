using BLS.Fields.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;

namespace BLS.ElipticCurve.Interfaces
{
    public interface IEllipticCurve<T> where T : IFieldElement<T>
    {
        // Curve: y^2 = x^3 + A x + B over the underlying field of T
        T A { get; }
        T B { get; }

        BigInteger GroupOrder { get; }
        [Description("The prime factorization of the group order, used for efficient order computations.")]
        IReadOnlyList<(BigInteger Prime, int Power)> GroupOrderFactors { get; }
        [Description("The largest prime dividing the group order (r).")]
        BigInteger R { get; }
        IECPoint<T> Infinity { get; }

        bool IsOnCurve(IECPoint<T> p);
        IECPoint<T> CreatePoint(T x, T y);
    }

}
