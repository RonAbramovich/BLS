using System;
using System.Numerics;
using Xunit;
using BLS.Fields.Implementations;
using BLS.ElipticCurve.Implementations;

namespace BLS.Tests
{
    public class GroupOrderVerificationTests
    {
        [Fact]
        public void VerifyGroupOrder_F43_k2_Curve_y2_x3_x_8()
        {
     
            BigInteger q = 43;
            var baseField = new PrimeField(q);
            var baseCurve = new EllipticCurve<PrimeFieldElement>(
                baseField,
                baseField.FromInt(1),  // A = 1
                baseField.FromInt(8)   // B = 8
            );

            // Step 1: Get base field group order
            BigInteger N_1 = baseCurve.GroupOrder;
            Console.WriteLine($"Step 1: |E(F_{q})| = {N_1}");

            // Step 2: Calculate Frobenius trace t = q + 1 - N_1
            BigInteger t = q + 1 - N_1;
            Console.WriteLine($"Step 2: Frobenius trace t = {q} + 1 - {N_1} = {t}");

            // Step 3: Use recurrence relation to compute a_k
            // a_n = t*a_{n-1} - q*a_{n-2}
            // a_0 = 2, a_1 = t
            int k = 2;
            BigInteger a_0 = 2;
            BigInteger a_1 = t;
            
            Console.WriteLine($"Step 3: Recurrence for k={k}:");
            Console.WriteLine($"  a_0 = {a_0}");
            Console.WriteLine($"  a_1 = {a_1}");

            BigInteger a_2 = t * a_1 - q * a_0;
            Console.WriteLine($"  a_2 = t*a_1 - q*a_0 = {t}*{a_1} - {q}*{a_0} = {a_2}");

            // Step 4: Calculate N_k = q^k + 1 - a_k
            BigInteger qk = BigInteger.Pow(q, k);
            BigInteger N_k_calculated = qk + 1 - a_2;
            
            Console.WriteLine($"\nStep 4: N_{k} = q^{k} + 1 - a_{k}");
            Console.WriteLine($"       = {qk} + 1 - {a_2}");
            Console.WriteLine($"       = {N_k_calculated}");

            // Step 5: Compare with what the implementation gives
            var irreduciblePoly = FindIrreduciblePolynomial(q, k);
            var extensionField = new ExtensionField(baseField, irreduciblePoly);
            Console.WriteLine($"\nUsing irreducible polynomial: {irreduciblePoly}");

            var extensionCurve = new EllipticCurve<ExtensionFieldElement>(
                extensionField,
                extensionField.FromInt(1),
                extensionField.FromInt(8)
            );

            BigInteger N_k_fromImpl = extensionCurve.GroupOrder;
            
            Console.WriteLine($"\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"COMPARISON:");
            Console.WriteLine($"  Manual calculation:     N_k = {N_k_calculated}");
            Console.WriteLine($"  Implementation returns: N_k = {N_k_fromImpl}");
            Console.WriteLine($"  Match: {N_k_calculated == N_k_fromImpl}");
            Console.WriteLine($"═══════════════════════════════════════════════════════════════");

            // Also check what external source says
            Console.WriteLine($"\nExternal source claims: N_k = 1936");
            Console.WriteLine($"Our calculation gives:  N_k = {N_k_calculated}");
            
            // Let's also verify by brute force for small field
            Console.WriteLine($"\n--- BRUTE FORCE VERIFICATION (may take time) ---");
            BigInteger bruteForceCount = CountPointsBruteForce(extensionCurve, extensionField, q, k);
            Console.WriteLine($"Brute force count: {bruteForceCount}");

            // Final assertion
            Assert.Equal(N_k_calculated, N_k_fromImpl);
        }

        /// <summary>
        /// Brute force point counting on extension field (slow but accurate for small fields)
        /// </summary>
        private BigInteger CountPointsBruteForce(
            EllipticCurve<ExtensionFieldElement> curve,
            ExtensionField field,
            BigInteger q,
            int k)
        {
            BigInteger count = 1; // Point at infinity
            BigInteger fieldSize = BigInteger.Pow(q, k);

            Console.WriteLine($"Enumerating all {fieldSize}² = {fieldSize * fieldSize} possible (x,y) pairs...");
            
            // For each possible x in F_q^k
            for (BigInteger x_index = 0; x_index < fieldSize; x_index++)
            {
                if (x_index % 100 == 0)
                {
                    Console.WriteLine($"  Progress: {x_index}/{fieldSize}...");
                }

                var x = IndexToExtensionElement(x_index, field, q, k);
                var rhs = x.Power(3) + curve.A * x + curve.B;

                // For each possible y in F_q^k
                for (BigInteger y_index = 0; y_index < fieldSize; y_index++)
                {
                    var y = IndexToExtensionElement(y_index, field, q, k);
                    var lhs = y * y;

                    if (lhs.Equals(rhs))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Convert index to extension field element
        /// </summary>
        private ExtensionFieldElement IndexToExtensionElement(
            BigInteger index,
            ExtensionField field,
            BigInteger q,
            int k)
        {
            var coeffs = new BigInteger[k];
            BigInteger temp = index;
            
            for (int i = 0; i < k; i++)
            {
                coeffs[i] = temp % q;
                temp /= q;
            }

            var poly = new Polynomial(q, coeffs);
            return new ExtensionFieldElement(field, poly);
        }

        private Polynomial FindIrreduciblePolynomial(BigInteger q, int degree)
        {
            for (int c = 1; c < (int)q && c < 100; c++)
            {
                var coeffs = new BigInteger[degree + 1];
                coeffs[0] = c;
                coeffs[degree] = 1;
                var poly = new Polynomial(q, coeffs);

                try
                {
                    var testField = new PrimeField(q);
                    var ext = new ExtensionField(testField, poly);
                    return poly;
                }
                catch
                {
                    continue;
                }
            }
            throw new InvalidOperationException($"Could not find irreducible polynomial of degree {degree} over F_{q}");
        }
    }
}
