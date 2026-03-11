using BLS.Fields.Interfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BLS.Fields.Implementations
{
    public class PrimeField : IField<PrimeFieldElement>
    {
        public BigInteger Characteristic { get; }

        public PrimeField(BigInteger prime)
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

        public PrimeFieldElement FromInt(BigInteger value) => new PrimeFieldElement(this, value);

        public bool IsValid(PrimeFieldElement x)
        {
            return x.Field != null && x.Field.Characteristic == Characteristic &&
                x.Value >= 0 && x.Value < Characteristic;
        }

        private static bool IsPrime(BigInteger n)
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

            BigInteger sqrt = Sqrt(n);
            for (BigInteger i = 3; i <= sqrt; i += 2)
            {
                if (n % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        // Implementation of Math.Sqrt for BigInteger 
        private static BigInteger Sqrt(BigInteger n)
        {
            if (n == 0) return 0;
            BigInteger x = n / 2;
            BigInteger y = (x + n / x) / 2;
            while (y < x)
            {
                x = y;
                y = (x + n / x) / 2;
            }
            return x;
        }
    }
}
