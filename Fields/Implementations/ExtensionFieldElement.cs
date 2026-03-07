using System;
using System.Numerics;
using BLS.Fields.Interfaces;

namespace BLS.Fields.Implementations
{
    public class ExtensionFieldElement : IFieldElement<ExtensionFieldElement>
    {
        public ExtensionField Field { get; }
        public Polynomial Poly { get; }

        public ExtensionFieldElement(ExtensionField field, Polynomial poly)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            ArgumentNullException.ThrowIfNull(poly);

            if (poly.Modulus != field.Characteristic)
            {
                throw new ArgumentException("Polynomial coefficients must be over the field characteristic.", nameof(poly));
            }

            // canonical representative: reduce modulo modulus polynomial
            Poly = Polynomial.Mod(poly, field.ModulusPolynomial);
        }

        public bool IsZero => Poly.IsZero;

        public ExtensionFieldElement Add(ExtensionFieldElement other)
        {
            EnsureSameField(other);
            var sum = Polynomial.Add(Poly, other.Poly);
            return new ExtensionFieldElement(Field, sum);
        }

        public ExtensionFieldElement Sub(ExtensionFieldElement other)
        {
            EnsureSameField(other);
            var diff = Polynomial.Sub(Poly, other.Poly);
            return new ExtensionFieldElement(Field, diff);
        }

        public ExtensionFieldElement Multiply(ExtensionFieldElement other)
        {
            EnsureSameField(other);
            var prod = Polynomial.Mul(Poly, other.Poly);
            var reduced = Polynomial.Mod(prod, Field.ModulusPolynomial);
            return new ExtensionFieldElement(Field, reduced);
        }

        public ExtensionFieldElement AdditiveInverse()
        {
            if (IsZero)
            {
                return this;
            }

            int p = Field.Characteristic;
            int deg = Poly.Degree;
            var coeffs = new int[deg + 1];
            for (int i = 0; i <= deg; i++)
            {
                coeffs[i] = (p - Poly[i]) % p;
            }
            return new ExtensionFieldElement(Field, new Polynomial(p, coeffs));
        }

        public ExtensionFieldElement MultiplicativeInverse()
        {
            if (IsZero)
            {
                throw new InvalidOperationException("Cannot invert zero.");
            }

            var inv = Polynomial.InverseMod(Poly, Field.ModulusPolynomial);
            return new ExtensionFieldElement(Field, inv);
        }

        public ExtensionFieldElement Power(long exponent)
        {
            if (exponent == 0)
            {
                return Field.One;
            }

            if (exponent < 0)
            {
                return MultiplicativeInverse().Power(-exponent); // g^{-n} = (g^{-1})^n
            }

            var resPoly = Polynomial.PowMod(Poly, new BigInteger(exponent), Field.ModulusPolynomial);
            return new ExtensionFieldElement(Field, resPoly);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ExtensionFieldElement other || Field.Characteristic != other.Field.Characteristic 
                || Field.ExtensionDegree != other.Field.ExtensionDegree || Poly.Degree != other.Poly.Degree)
            {
                return false;
            }

            var fieldPolynomial = Field.ModulusPolynomial;
            var otherFieldPolynomial = other.Field.ModulusPolynomial;
            if (fieldPolynomial.Degree != otherFieldPolynomial.Degree)
            {
                return false;
            }

            for (int i = 0; i <= fieldPolynomial.Degree; i++)
            {
                if (fieldPolynomial[i] != otherFieldPolynomial[i])
                {
                    return false;
                }
            }

            for (int i = 0; i <= Poly.Degree; i++)
            {
                if (Poly[i] != other.Poly[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            var h = HashCode.Combine(Field.Characteristic, Field.ExtensionDegree);
            // include modulus polynomial coefficients to distinguish fields with same characteristic and degree
            var m = Field.ModulusPolynomial;
            for (int i = 0; i <= m.Degree; i++)
            {
                h = HashCode.Combine(h, m[i]);
            }

            for (int i = 0; i <= Poly.Degree; i++)
            {
                h = HashCode.Combine(h, Poly[i]);
            }

            return h;
        }

        #region Operator overloads
        public static ExtensionFieldElement operator +(ExtensionFieldElement a, ExtensionFieldElement b) => a.Add(b);
        public static ExtensionFieldElement operator -(ExtensionFieldElement a, ExtensionFieldElement b) => a.Sub(b);
        public static ExtensionFieldElement operator *(ExtensionFieldElement a, ExtensionFieldElement b) => a.Multiply(b);
        public static ExtensionFieldElement operator -(ExtensionFieldElement a) => a.AdditiveInverse();
        public static ExtensionFieldElement operator /(ExtensionFieldElement a, ExtensionFieldElement b) => a.Multiply(b.MultiplicativeInverse());
        #endregion

        public override string ToString() => Poly.ToString();

        private void EnsureSameField(ExtensionFieldElement other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (Field.Characteristic != other.Field.Characteristic)
            {
                throw new InvalidOperationException("Field characteristic mismatch between extension elements.");
            }
        }
    }
}
