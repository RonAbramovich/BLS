using System;
using Xunit;
using System.Numerics;
using BLS.Fields.Implementations;

namespace BLS.Tests
{
    public class ExtensionFieldTests
    {
        [Fact]
        public void Inverse_F2_Power2_x_has_inverse_x_plus_1()
        {
            var p = 2;
            var modulus = new Polynomial(p, 1, 1, 1); // x^2 + x + 1
            var baseField = new PrimeField(p);
            Assert.True(PolynomialUtils.IsIrreducible(modulus, baseField));
            var field = new ExtensionField(baseField, modulus);

            var x = new ExtensionFieldElement(field, Polynomial.X(p));
            var inv = x.MultiplicativeInverse();

            Assert.Equal("x + 1", inv.ToString());
            var prod = (x * inv).ToString();
            Assert.Equal("1", prod);
        }

        [Fact]
        public void Inverse_F22_xplus1_has_inverse_x()
        {
            var p = 2;
            var modulus = new Polynomial(p, 1, 1, 1); // x^2 + x + 1
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            var a = new ExtensionFieldElement(field, new Polynomial(p, 1, 1)); // x + 1
            var inv = a.MultiplicativeInverse();

            Assert.Equal("x", inv.ToString());
            var prod = (a * inv).ToString();
            Assert.Equal("1", prod);
        }

        [Fact]
        public void Inverse_F3_x_returns_2x()
        {
            var p = 3;
            var modulus = new Polynomial(p, 1, 0, 1); // x^2 + 1
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);
            Assert.True(PolynomialUtils.IsIrreducible(modulus, baseField));

            var x = new ExtensionFieldElement(field, Polynomial.X(p));
            var inv = x.MultiplicativeInverse();

            Assert.Equal("2x", inv.ToString());
            var prod = (x * inv).ToString();
            Assert.Equal("1", prod);
        }

        [Fact]
        public void Inverse_Zero_Throws_ExtensionField()
        {
            var p = 5;
            var modulus = new Polynomial(p, 1, 0, 0, 1); // x^3 + 1
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus, enforceIrreducible: false);

            var zero = field.Zero;
            Assert.Throws<InvalidOperationException>(() => zero.MultiplicativeInverse());
        }

        [Fact]
        public void EquivalentPolynomialReducesToSameElement()
        {
            // Verify different representations of the same polynomial reduce to the same canonical element in the field
            var p = 3;
            var modulus = new Polynomial(p, 1, 0, 1); // x^2 + 1
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            // x^2 in this field equals -1 which is 2 in F3
            var polyX2 = new Polynomial(p, 0, 0, 1); // x^2
            var elemFromX2 = new ExtensionFieldElement(field, polyX2);
            var two = field.FromInt(2);

            Assert.Equal(two, elemFromX2);
        }

        [Fact]
        public void FromInt_Addition_Works()
        {
            var p = 7;
            var modulus = new Polynomial(p, 1, 0, 1); // x^2 + 1 (degree 2)
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            var a = field.FromInt(3);
            var b = field.FromInt(5);
            var sum = a + b;
            // 3 + 5 = 8 mod 7 = 1
            Assert.Equal(field.FromInt(1), sum);
        }

        [Fact]
        public void AdditionAndSubtraction_Works()
        {
            var p = 7;
            var modulus = new Polynomial(p, 1, 0, 1);
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            var a = field.FromInt(4);
            var b = field.FromInt(6);
            var sum = a + b; // 4 + 6 = 10 mod 7 = 3
            Assert.Equal(field.FromInt(3), sum);

            var diff = a - b; // 4 - 6 = -2 mod 7 = 5
            Assert.Equal(field.FromInt(5), diff);
        }

        [Fact]
        public void Multiplication_Works()
        {
            var p = 3;
            var modulus = new Polynomial(p, 1, 0, 1); // x^2 + 1 over F3
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            var x = new ExtensionFieldElement(field, Polynomial.X(p));

            // x * x = x^2 = -1 = 2 (mod 3)
            var prod = x * x;
            Assert.Equal(field.FromInt(2), prod);

            // (x + 1) * (x + 2) -> expand and reduce should produce consistent result
            var a = new ExtensionFieldElement(field, new Polynomial(p, 1, 1)); // x + 1
            var b = new ExtensionFieldElement(field, new Polynomial(p, 2, 1)); // x + 2
            var ab = a * b;
            // compute expected: (x+1)(x+2)=x^2+3x+2 = x^2 + 2 = -1 + 2 = 1
            Assert.Equal(field.FromInt(1), ab);
        }

        [Fact]
        public void Power_Works()
        {
            var p = 3;
            var modulus = new Polynomial(p, 1, 0, 1);
            var baseField = new PrimeField(p);
            var field = new ExtensionField(baseField, modulus);

            var x = new ExtensionFieldElement(field, Polynomial.X(p));

            // x^3 = x * x^2 = x * (-1) = -x = 2x
            var xCubed = x.Power(3);
            var expected = new ExtensionFieldElement(field, new Polynomial(p, 0, 2)); // 2x
            Assert.Equal(expected, xCubed);
        }
    }
}
