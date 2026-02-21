using BLS.Fields.Interfaces;

namespace BLS.Fields.Implementations
{
    public class PrimeFieldElement : IFieldElement<PrimeFieldElement>
    {
        public PrimeField Field { get; }
        public int Value { get; }
        
        public PrimeFieldElement(PrimeField field, long value)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Value = ModuloNormalize(value);
        }

        public bool IsZero => Value == 0;

        public PrimeFieldElement Add(PrimeFieldElement other)
        {
            EnsureSameField(other);
            return new PrimeFieldElement(Field, ModuloNormalize(Value + other.Value));
        }

        public PrimeFieldElement Sub(PrimeFieldElement other)
        {
            EnsureSameField(other);
            return new PrimeFieldElement(Field, ModuloNormalize(Value - other.Value));
        }
        public PrimeFieldElement Multiply(PrimeFieldElement other)
        {
            EnsureSameField(other);
            return new PrimeFieldElement(Field, ModuloNormalize(ModuloNormalize(Value) * ModuloNormalize(other.Value)));
        }

        public PrimeFieldElement AdditiveInverse()
        {
            return new PrimeFieldElement(Field, ModuloNormalize(-Value));
        }

        public PrimeFieldElement MultiplicativeInverse()
        {
            if (IsZero)
            {
                throw new InvalidOperationException("Cannot compute the multiplicative inverse of zero.");
            }

            // r = remainder, t = coefficient
            var modulus = Field.Characteristic;
            var remainder = modulus;
            var nextRemainder = Value;
            var coefficient = 0;
            var nextCoefficient = 1;

            while (nextRemainder != 0)
            {
                var quotient = remainder / nextRemainder;
                (remainder, nextRemainder) = (nextRemainder, remainder - quotient * nextRemainder);
                var nextT = coefficient - quotient * nextCoefficient;
                (coefficient, nextCoefficient) = (nextCoefficient, nextT);
            }

            // In a Prime Field, the GCD (the final 'remainder') must be 1.
            if (remainder != 1)
            {
                throw new InvalidOperationException("Inverse does not exist. Ensure Field.Characteristic is prime.");
            }

            int result = ModuloNormalize(coefficient);
            return new PrimeFieldElement(Field, result);
        }

        public PrimeFieldElement Power(long exponent)
        {
            // We find the binary representation of the exponent and use exponentiation by squaring.
            if (exponent < 0)
            {
                // For negative exponents, find the multiplicative inverse first, then raise that inverse to the positive power.
                return MultiplicativeInverse().Power(-exponent);
            }

            if (exponent == 0)
            {
                return new PrimeFieldElement(Field, 1);
            }

            if (IsZero)
            {
                return new PrimeFieldElement(Field, 0);
            }

            long baseValue = Value;
            long result = 1;
            var p = Field.Characteristic;

            while (exponent > 0)
            {
                // If the current bit of the exponent is 1, multiply the result by the current base
                if ((exponent & 1) == 1)
                {
                    result = ModuloNormalize(result * baseValue);
                }

                // Square the base for the next bit
                baseValue = ModuloNormalize(baseValue * baseValue);

                // Shift the exponent to the right to process the next bit
                exponent >>= 1;
            }

            return new PrimeFieldElement(Field, (int)result);
        }
        public override bool Equals(object other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (other == null || GetType() != other.GetType())
            {
                return false;
            }
            var otherElement = (PrimeFieldElement)other;
            return Field.Characteristic == otherElement.Field.Characteristic && Value == otherElement.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Field.Characteristic, Value);
        }

        #region Operator-Overloads
        public static PrimeFieldElement operator +(PrimeFieldElement a, PrimeFieldElement b) => a.Add(b);
        public static PrimeFieldElement operator -(PrimeFieldElement a, PrimeFieldElement b) => a.Sub(b);
        public static PrimeFieldElement operator *(PrimeFieldElement a, PrimeFieldElement b) => a.Multiply(b);
        public static PrimeFieldElement operator -(PrimeFieldElement a) => a.AdditiveInverse();
        public static PrimeFieldElement operator /(PrimeFieldElement a, PrimeFieldElement b) => a.Multiply(b.MultiplicativeInverse());

        public static bool operator ==(PrimeFieldElement a, PrimeFieldElement b) =>
            ReferenceEquals(a, b) || (a is not null && a.Equals(b));

        public static bool operator !=(PrimeFieldElement a, PrimeFieldElement b) => !(a == b);
        #endregion

        #region privateHelpers
        private void EnsureSameField(PrimeFieldElement other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (Field.Characteristic != other.Field.Characteristic)
            {
                throw new InvalidOperationException($"Field mismatch: Other is from prime field {other.Field.Characteristic}, current prime field {Field.Characteristic}.");
            }
        }

        private int ModuloNormalize(long x)
        {
            var reminder = x % Field.Characteristic;
            if (reminder < 0)
            {
                reminder += Field.Characteristic;
            }

            return (int)reminder;
        }

        #endregion
    }
}