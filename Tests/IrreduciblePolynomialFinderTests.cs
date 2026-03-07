using System.Numerics;
using Xunit;
using BLS.Fields.Implementations;

namespace BLS.Tests
{
    public class IrreduciblePolynomialFinderTests
    {
        [Fact]
        public void FindIrreduciblePolynomial_F2_Degree2_Returns_x2_x_1()
        {
            var field = new PrimeField(2);
            // r = 3, embedding degree for p=2 is k=2 because 2^2 - 1 = 3
            var r = new BigInteger(3);

            var poly = IrreduciblePolynomialFinder.FindIrreduciblePolynomial(field, r);

            // For F2 degree 2 the expected irreducible monic polynomial is x^2 + x + 1
            Assert.NotNull(poly);
            Assert.Equal("x^2 + x + 1", poly.ToString());
        }
    }
}
