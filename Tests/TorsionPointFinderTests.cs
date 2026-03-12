using System;
using System.Numerics;
using Xunit;
using BLS.Fields.Implementations;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;

namespace BLS.Tests
{
    public class TorsionPointFinderTests
    {
        [Fact]
        public void FindIndependentTorsionPoint_ProperBLS_SameR()
        {
            // LARGE FIELD: y^2 = x^3 + 1 over F_101            
            BigInteger q = 101;
            var baseField = new PrimeField(q);
            var baseCurve = new EllipticCurve<PrimeFieldElement>(
                baseField,
                baseField.FromInt(0),  // A = 0
                baseField.FromInt(1)   // B = 1  → y² = x³ + 1
            );

            BigInteger baseGroupOrder = baseCurve.GroupOrder;
            BigInteger r = baseCurve.R;
            var irreduciblePoly = new Polynomial(q, 2, 0, 1); // x^2 + 2
            ExtensionField extensionField;

            try
            {
                extensionField = new ExtensionField(baseField, irreduciblePoly);
            }
            catch
            {
                irreduciblePoly = new Polynomial(q, 3, 0, 1);
                extensionField = new ExtensionField(baseField, irreduciblePoly);
            }

            Console.WriteLine($"Extension field: F_{q}² using {irreduciblePoly}");

            var extensionCurve = new EllipticCurve<ExtensionFieldElement>(
                extensionField,
                extensionField.FromInt(0),
                extensionField.FromInt(1)
            );

            BigInteger N_k = extensionCurve.GroupOrder;
            Assert.True(N_k % r == 0, $"r must divide extension curve order");

            var P = FindPointOfOrderR(baseCurve, r, baseField);
            Assert.NotNull(P);
            Assert.True(P.Multiply(r).IsInfinity);

            Console.WriteLine($"Found P in E(F_{q}) of order {r}");
            Console.WriteLine($"Now finding Q in E(F_{q}²) with same order...\n");

            var Q = TorsionPointFinder.FindIndependentTorsionPoint(extensionCurve, r, maxAttempts: 100);

            Assert.False(Q.IsInfinity);
            Assert.True(extensionCurve.IsOnCurve(Q));
            Assert.True(Q.Multiply(r).IsInfinity);

            bool isRational = (Q.X.Poly.Degree <= 0 && Q.Y.Poly.Degree <= 0);
            Assert.False(isRational, "Q must be irrational!");
        }

        [Fact]
        public void FindIndependentTorsionPoint_ThrowsWhen_R_DoesNotDivide_ExtensionCurveOrder()
        {
            var baseField = new PrimeField(new BigInteger(5));
            var irreduciblePoly = new Polynomial(5, 2, 0, 1);
            var extensionField = new ExtensionField(baseField, irreduciblePoly);
            var A = extensionField.FromInt(0);
            var B = extensionField.FromInt(1);
            var curve = new EllipticCurve<ExtensionFieldElement>(extensionField, A, B);

            BigInteger invalidR = 997;

            var exception = Assert.Throws<ArgumentException>(() => 
                TorsionPointFinder.FindIndependentTorsionPoint(curve, invalidR));

            Assert.Contains("does not divide", exception.Message);
        }

        #region Helper Methods

        /// <summary>
        /// Finds a point P \in E(F_q) with exact order r.
        /// By Cauchy's theorem, such a point exists since r | |E(F_q)|.
        /// </summary>
        private IECPoint<PrimeFieldElement> FindPointOfOrderR(
            EllipticCurve<PrimeFieldElement> curve,
            BigInteger r,
            PrimeField field)
        {
            BigInteger q = field.Characteristic;
            BigInteger groupOrder = curve.GroupOrder;

            // Try all points until we find one of order r
            for (BigInteger x_val = 0; x_val < q; x_val++)
            {
                var x = field.FromInt(x_val);
                var rhs = x.Power(3).Add(curve.A.Multiply(x)).Add(curve.B);

                if (rhs.IsZero)
                {
                    var P = curve.CreatePoint(x, field.Zero);
                    if (HasOrderR(P, r))
                    {
                        return P;
                    }
                    continue;
                }

                // Check if rhs is a quadratic residue
                long residueExp = (long)((q - 1) / 2);
                var legendre = rhs.Power(residueExp);

                if (legendre.Equals(field.One))
                {
                    // Try all y values for small fields
                    for (BigInteger y_val = 0; y_val < q; y_val++)
                    {
                        var y = field.FromInt(y_val);
                        var y_squared = y.Multiply(y);

                        if (y_squared.Equals(rhs))
                        {
                            var P = curve.CreatePoint(x, y);
                            if (HasOrderR(P, r))
                            {
                                return P;
                            }
                        }
                    }
                }
            }

            throw new InvalidOperationException($"Could not find point of order {r}");
        }

        /// <summary>
        /// Checks if point P has exact order r
        /// </summary>
        private bool HasOrderR(IECPoint<PrimeFieldElement> P, BigInteger r)
        {
            if (P.IsInfinity) return false;

            // Check that r*P = O
            var rP = P.Multiply(r);
            if (!rP.IsInfinity) return false;

            // Check that smaller divisors of r don't give O
            // For small r, just check r/2 if r is even
            if (r % 2 == 0)
            {
                var halfP = P.Multiply(r / 2);
                if (halfP.IsInfinity) return false;
            }

            // For prime r, the only divisors are 1 and r, so we're done
            return true;
        }

        #endregion
    }
}
