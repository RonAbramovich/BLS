using System;
using Xunit;
using BLS.ElipticCurve.Implementations;
using BLS.Fields.Implementations;

namespace BLS.Tests
{
    public class HashToPointTests
    {
        [Fact]
        public void HashToCurve_IsDeterministic_And_InRSubgroup()
        {
            var p = 7;
            var baseField = new PrimeField(p);
            var a = baseField.FromInt(0);
            var b = baseField.FromInt(1);
            var curve = new EllipticCurve<PrimeFieldElement>(baseField, a, b);

            var m = "hello";
            var h1 = HashToPoint.HashToCurve(curve, m);
            var h2 = HashToPoint.HashToCurve(curve, m);

            Assert.False(h1.IsInfinity);
            Assert.Equal(h1, h2); // deterministic

            // ensure point is in r-subgroup: r * H == Infinity
            var r = curve.R;
            var check = h1.Multiply(r);
            Assert.True(check.IsInfinity);
        }

        [Fact]
        public void HashToCurve_Supports_Hebrew_Message()
        {
            var p = 7;
            var baseField = new PrimeField(p);
            var a = baseField.FromInt(0);
            var b = baseField.FromInt(1);
            var curve = new EllipticCurve<PrimeFieldElement>(baseField, a, b);

            var m = "שלום"; // Hebrew
            var h = HashToPoint.HashToCurve(curve, m);

            Assert.False(h.IsInfinity);
            var check = h.Multiply(curve.R);
            Assert.True(check.IsInfinity);
        }

        [Fact]
        public void HashToCurve_Throws_When_P_Mod4_Not3()
        {
            var p = 5; // p % 4 == 1
            var baseField = new PrimeField(p);
            var a = baseField.FromInt(0);
            var b = baseField.FromInt(1);
            var curve = new EllipticCurve<PrimeFieldElement>(baseField, a, b);

            Assert.Throws<NotSupportedException>(() => HashToPoint.HashToCurve(curve, "hello"));
        }
    }
}
