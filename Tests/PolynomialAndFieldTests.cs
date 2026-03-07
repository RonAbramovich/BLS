using System;
using Xunit;
using System.Numerics;
using BLS.Fields.Implementations;

namespace BLS.Tests
{
    public class PolynomialAndFieldTests
    {
        [Fact]
        public void InverseMod_F2_x_has_inverse_x_plus_1()
        {
            var p = 2;
            var modulus = new Polynomial(p, 1, 1, 1); // x^2 + x + 1

            var x = Polynomial.X(p);
            var inv = Polynomial.InverseMod(x, modulus);

            Assert.Equal("x + 1", inv.ToString());

            var prod = Polynomial.Mod(Polynomial.Mul(x, inv), modulus);
            Assert.Equal("1", prod.ToString());
        }

        [Fact]
        public void InverseMod_F2_x_plus_1_has_inverse_x()
        {
            var p = 2;
            var modulus = new Polynomial(p, 1, 1, 1); // x^2 + x + 1

            var a = new Polynomial(p, 1, 1); // x + 1
            var inv = Polynomial.InverseMod(a, modulus);

            Assert.Equal("x", inv.ToString());
            var prod = Polynomial.Mod(Polynomial.Mul(a, inv), modulus);
            Assert.Equal("1", prod.ToString());
        }

        [Fact]
        public void InverseMod_F3_x_returns_2x()
        {
            var p = 3;
            var modulus = new Polynomial(p, 1, 0, 1); // x^2 + 1 over F3 (irreducible)

            var x = Polynomial.X(p);
            var inv = Polynomial.InverseMod(x, modulus);

            // expected inverse is 2*x because x*(2x) = 2*x^2 = 2*(-1) = -2 = 1 (mod 3)
            Assert.Equal("2x", inv.ToString());

            var prod = Polynomial.Mod(Polynomial.Mul(x, inv), modulus);
            Assert.Equal("1", prod.ToString());
        }

        [Fact]
        public void InverseMod_Zero_Throws()
        {
            var p = 5;
            var modulus = new Polynomial(p, 1, 0, 0, 1); // x^3 + 1 (not necessarily irreducible here)
            var zero = new Polynomial(p);
            Assert.Throws<InvalidOperationException>(() => Polynomial.InverseMod(zero, modulus));
        }
    }
}
