using System.Numerics;
using Xunit;
using BLS.Fields.Implementations;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;

namespace BLS.Tests
{
    public class EllipticCurveTests
    {
        [Fact]
        public void PrimeFieldElement_BasicArithmeticAndInverse()
        {
            var field = new PrimeField(7);
            var a = field.FromInt(3);
            var b = field.FromInt(5);

            // addition
            var sum = a + b;
            Assert.Equal(field.FromInt(1), sum); // 3+5=8 mod7 =1

            // multiplication
            var prod = a * b;
            Assert.Equal(field.FromInt(1), prod); // 3*5=15 mod7=1

            // additive inverse
            var negA = -a;
            Assert.Equal(field.FromInt(4), negA); // -3 mod7 =4

            // multiplicative inverse
            var invA = a.MultiplicativeInverse();
            Assert.Equal(field.FromInt(5), invA);
            Assert.Equal(field.One, a * invA);

            // power
            var sq = a.Power(2);
            Assert.Equal(field.FromInt(2), sq); // 3^2 =9 mod7=2

            var negPow = a.Power(-1); // should be multiplicative inverse
            Assert.Equal(invA, negPow);
        }

        [Fact]
        public void EllipticCurve_GroupOrder_Factors_R()
        {
            var field = new PrimeField(5);
            var A = field.FromInt(0);
            var B = field.FromInt(2);
            var curve = new EllipticCurve<PrimeFieldElement>(field, A, B);

            // Known group order for this curve is 6
            Assert.Equal(new BigInteger(6), curve.GroupOrder);

            // Factors should include 2 and 3
            var factors = curve.GroupOrderFactors;
            Assert.Contains(factors, f => f.Prime == 2 && f.Power == 1);
            Assert.Contains(factors, f => f.Prime == 3 && f.Power == 1);

            // R (largest prime divisor) should be 3
            Assert.Equal(new BigInteger(3), curve.R);
        }

        [Fact]
        public void ECPoint_IsOnCurve_Negate_Double_Multiply_Order()
        {
            var field = new PrimeField(5);
            var A = field.FromInt(0);
            var B = field.FromInt(2);
            var curve = new EllipticCurve<PrimeFieldElement>(field, A, B);

            // Points known on the curve: (2,0), (3,2),(3,3),(4,1),(4,4) and infinity
            var falsePoint = curve.CreatePoint(field.FromInt(2), field.FromInt(3));
            var p1 = curve.CreatePoint(field.FromInt(2), field.FromInt(0));
            var p2 = curve.CreatePoint(field.FromInt(3), field.FromInt(2));
            var p2neg = curve.CreatePoint(field.FromInt(3), field.FromInt(3));

            Assert.True(curve.IsOnCurve(p1));
            Assert.True(curve.IsOnCurve(p2));
            Assert.True(curve.IsOnCurve(p2neg));
            Assert.False(curve.IsOnCurve(falsePoint));

            // Negate
            var neg = p2.Negate();
            Assert.True(neg.Equals(p2neg));

            // Double p2 -> should equal p2neg (we computed manually)
            var dbl = p2.Double();
            Assert.True(dbl.Equals(p2neg));

            // 3 * p2 == infinity (order 3)
            var triple = p2.Multiply(3);
            Assert.True(triple.IsInfinity);

            Assert.Equal(new BigInteger(3), p2.Order);

            // p1 has y=0 so doubling -> infinity and order 2
            Assert.True(p1.Double().IsInfinity);
            Assert.Equal(new BigInteger(2), p1.Order);

            // multiply by negative scalar
            var negMul = p2.Multiply(-1);
            Assert.True(negMul.Equals(p2.Negate()));
        }

        [Fact]
        public void ECPoint_EqualsAndHashCode()
        {
            var field = new PrimeField(5);
            var A = field.FromInt(0);
            var B = field.FromInt(2);
            var curve = new EllipticCurve<PrimeFieldElement>(field, A, B);

            var p = curve.CreatePoint(field.FromInt(4), field.FromInt(1));
            var same = curve.CreatePoint(field.FromInt(4), field.FromInt(1));
            var different = curve.CreatePoint(field.FromInt(3), field.FromInt(2));

            Assert.True(p.Equals(same));
            Assert.False(p.Equals(different));
            Assert.Equal(p.GetHashCode(), same.GetHashCode());
        }
    }
}
