using BLS.Fields.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLS.Fields.Implementations
{
    public class PrimeField : IField<PrimeFieldElement>
    {
        public int Characteristic { get; }

        public PrimeField(int prime)
        {
            if (!IsPrime(prime))
            {
                throw new ArgumentException("The characteristic must be a prime number.", nameof(prime));
            }

            Characteristic = prime;
        }

        public int ExtensionDegree => 1; // Relevant only for Extension fields. 

        public PrimeFieldElement Zero => new(this, 0);

        public PrimeFieldElement One => new PrimeFieldElement(this, 1);

        public PrimeFieldElement FromInt(long value) => new PrimeFieldElement(this, value);

        public bool IsValid(PrimeFieldElement x)
        {
            return x.Field != null && x.Field.Characteristic == Characteristic &&
                x.Value >= 0 && x.Value < Characteristic;
        }

        private static bool IsPrime(int n)
        {
            if (n < 2)
            {
                return false;
            }
            if (n == 2) {
                return true; 
            }
            if ((n % 2) == 0)
            {
                return false;
            }

            for (int i = 3; i <= (int)Math.Sqrt(n); i += 2)
            {
                if (n % i == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
