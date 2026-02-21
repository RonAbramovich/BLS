using BLS.Fields.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLS.Fields.Implementations
{
    public class PrimeField : IField<PrimeFieldElement>
    {
        public PrimeField()
        {
        }

        public int Characteristic => throw new NotImplementedException();

        public int ExtensionDegree => throw new NotImplementedException();

        public PrimeFieldElement Zero => throw new NotImplementedException();

        public PrimeFieldElement One => throw new NotImplementedException();

        public PrimeFieldElement FromInt(long value)
        {
            throw new NotImplementedException();
        }

        public bool IsValid(PrimeFieldElement x)
        {
            throw new NotImplementedException();
        }
    }
}
