using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace BLS.Fields.Interfaces
{
    public interface IField<TElement> where TElement : IFieldElement<TElement>
    {
        BigInteger Characteristic { get; }  // p
        int ExtensionDegree { get; } // 1 for Fp, k for Fp^k

        // Canonical elements
        TElement Zero { get; }
        TElement One { get; }
        TElement FromInt(BigInteger value);
        bool IsValid(TElement x);
    }
}
