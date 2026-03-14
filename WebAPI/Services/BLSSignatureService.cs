using System;
using System.Numerics;
using System.Text;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;
using BLS.Pairing.Implementations;
using BLS.WebAPI.Models;
using BLS.WebAPI.Resources;

namespace BLS.WebAPI.Services
{
    /// <summary>
    /// Service for executing BLS signature generation and verification with step-by-step tracking
    /// </summary>
    public class BLSSignatureService
    {
        public BLSSignatureResponse ExecuteSignature(BLSSignatureRequest request)
        {
            var response = new BLSSignatureResponse { Success = true };

            // Initialize detailed report if requested
            if (request.IncludeDetailedReport)
            {
                response.DetailedReport = new DetailedCalculationReport();
            }

            try
            {
                // Parse inputs
                var q = BigInteger.Parse(request.PRIME_Q);
                var a_param = BigInteger.Parse(request.A);
                var b_param = BigInteger.Parse(request.B);
                var privateKey = BigInteger.Parse(request.PrivateKey);
                var message = request.Message;
                var lang = request.Language;

                // Step 1: Create prime field and elliptic curve
                var (baseField, baseCurve) = ExecuteStep1(q, a_param, b_param, lang, response);

                // Step 2: Calculate group order
                var groupOrder = ExecuteStep2(baseCurve, lang, response);

                // Get largest prime divisor r
                var r = baseCurve.R;

                // Step 3: Hash message to point P = H(m)
                var P = ExecuteStep3(message, baseCurve, baseField, lang, response);

                // Step 4: Find embedding degree k
                var k = ExecuteStep4(r, q, lang, response);

                // Step 5: Find irreducible polynomial
                var irreduciblePoly = ExecuteStep5(q, k, r, lang, response);

                // Step 6: Create extension field and curve
                var (extensionField, extensionCurve) = ExecuteStep6(
                    baseField, irreduciblePoly, a_param, b_param, k, lang, response);

                // Step 7: Find torsion point Q
                var Q = ExecuteStep7(extensionCurve, r, lang, response);

                // Step 8: Calculate Alice signature: e_r(a*H(m), Q)
                var aliceSignature = ExecuteStep8(P, Q, r, privateKey, baseCurve, extensionField, lang, response);

                // Step 9: Calculate Bob verification: e_r(H(m), a*Q)
                var bobVerification = ExecuteStep9(P, Q, r, privateKey, baseCurve, extensionField, lang, response);

                // Final verification
                response.AliceSignature = aliceSignature.ToString();
                response.BobVerification = bobVerification.ToString();
                response.VerificationPassed = aliceSignature.Equals(bobVerification);

                // Update signature match status in detailed report
                if (response.DetailedReport?.VerificationCalculationDetails != null)
                {
                    response.DetailedReport.VerificationCalculationDetails.SignatureMatches = response.VerificationPassed;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        private (PrimeField, EllipticCurve<PrimeFieldElement>) ExecuteStep1(
            BigInteger q, BigInteger a, BigInteger b, string lang, BLSSignatureResponse response)
        {
            var baseField = new PrimeField(q);
            var baseCurve = new EllipticCurve<PrimeFieldElement>(
                baseField,
                baseField.FromInt(a),
                baseField.FromInt(b)
            );

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 1,
                Description = StepDescriptions.GetDescription(1, lang),
                TechnicalDescription = $"Created F_{q} and curve y^2 = x^3 + {a}x + {b}",
                Result = $"F_{q}, E: y^2 = x^3 + {a}x + {b}",
                Details = new()
                {
                    ["Field"] = $"F_{q}",
                    ["Curve_A"] = a.ToString(),
                    ["Curve_B"] = b.ToString()
                }
            });

            return (baseField, baseCurve);
        }

        private BigInteger ExecuteStep2(
            EllipticCurve<PrimeFieldElement> baseCurve, string lang, BLSSignatureResponse response)
        {
            var groupOrder = baseCurve.GroupOrder;
            var r = baseCurve.R;

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 2,
                Description = StepDescriptions.GetDescription(2, lang),
                TechnicalDescription = $"Calculated |E(F_q)| and found largest prime divisor r",
                Result = $"|E(F_q)| = {groupOrder}, r = {r}",
                Details = new()
                {
                    ["GroupOrder"] = groupOrder.ToString(),
                    ["LargestPrime_r"] = r.ToString()
                }
            });

            return groupOrder;
        }

        private IECPoint<PrimeFieldElement> ExecuteStep3(
            string message, EllipticCurve<PrimeFieldElement> baseCurve, 
            PrimeField baseField, string lang, BLSSignatureResponse response)
        {
            // Initialize Step3 details if detailed report is requested
            Step3Details? details = null;
            if (response.DetailedReport != null)
            {
                details = new Step3Details
                {
                    OriginalMessage = message,
                    Attempts = new()
                };
            }

            // Hash the message using HashToPoint with tracing
            var P = HashToPointWithTracing(baseCurve, message, details);

            // Populate detailed report
            if (details != null && response.DetailedReport != null)
            {
                details.FinalPointX = P.X.ToString();
                details.FinalPointY = P.Y.ToString();
                response.DetailedReport.MessageHashingDetails = details;
            }

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 3,
                Description = StepDescriptions.GetDescription(3, lang),
                TechnicalDescription = $"Hashed message '{message}' to curve point",
                Result = $"P = H(\"{message}\") = ({P.X}, {P.Y})",
                Details = new()
                {
                    ["Message"] = message,
                    ["Point_X"] = P.X.ToString(),
                    ["Point_Y"] = P.Y.ToString()
                }
            });

            return P;
        }

        private int ExecuteStep4(
            BigInteger r, BigInteger q, string lang, BLSSignatureResponse response)
        {
            var k = EmbeddingDegreeCalculator.FindEmbeddingDegree(r, q);

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 4,
                Description = StepDescriptions.GetDescription(4, lang),
                TechnicalDescription = $"Found smallest k where r | (q^k - 1)",
                Result = $"k = {k}",
                Details = new()
                {
                    ["EmbeddingDegree_k"] = k.ToString(),
                    ["Verification"] = $"r | ({q}^{k} - 1)"
                }
            });

            return k;
        }

        private Polynomial ExecuteStep5(
            BigInteger q, int k, BigInteger r, string lang, BLSSignatureResponse response)
        {
            var baseField = new PrimeField(q);
            var poly = IrreduciblePolynomialFinder.FindIrreduciblePolynomial(baseField, r, k);

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 5,
                Description = StepDescriptions.GetDescription(5, lang),
                TechnicalDescription = $"Found irreducible polynomial of degree {k} over F_{q}",
                Result = $"g(x) = {poly}",
                Details = new()
                {
                    ["Polynomial"] = poly.ToString(),
                    ["Degree"] = k.ToString()
                }
            });

            return poly;
        }

        private (ExtensionField, EllipticCurve<ExtensionFieldElement>) ExecuteStep6(
            PrimeField baseField, Polynomial irreduciblePoly, BigInteger a, BigInteger b,
            int k, string lang, BLSSignatureResponse response)
        {
            var extensionField = new ExtensionField(baseField, irreduciblePoly);
            var extensionCurve = new EllipticCurve<ExtensionFieldElement>(
                extensionField,
                extensionField.FromInt(a),
                extensionField.FromInt(b)
            );

            var N_k = extensionCurve.GroupOrder;

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 6,
                Description = StepDescriptions.GetDescription(6, lang),
                TechnicalDescription = $"Created F_q^{k} and curve E(F_q^{k})",
                Result = $"F_{baseField.Characteristic}^{k}, |E(F_q^k)| = {N_k}",
                Details = new()
                {
                    ["ExtensionDegree"] = k.ToString(),
                    ["ExtensionGroupOrder"] = N_k.ToString()
                }
            });

            return (extensionField, extensionCurve);
        }

        private IECPoint<ExtensionFieldElement> ExecuteStep7(
            EllipticCurve<ExtensionFieldElement> extensionCurve, BigInteger r,
            string lang, BLSSignatureResponse response)
        {
            var Q = TorsionPointFinder.FindIndependentTorsionPoint(extensionCurve, r);

            // Populate detailed report
            if (response.DetailedReport != null)
            {
                response.DetailedReport.TorsionPointDetails = new Step7Details
                {
                    TargetOrder = r.ToString(),
                    ExtensionFieldSize = $"{extensionCurve.Field.Characteristic}^{extensionCurve.Field.ExtensionDegree}",
                    FinalPointX_Polynomial = Q.X.ToString(),
                    FinalPointY_Polynomial = Q.Y.ToString(),
                    PointX_Degree = Q.X.Poly.Degree,
                    PointY_Degree = Q.Y.Poly.Degree,
                    CofactorMultiplier = (extensionCurve.GroupOrder / r).ToString(),
                    IsLinearlyIndependent = Q.X.Poly.Degree > 0 || Q.Y.Poly.Degree > 0
                };
            }

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 7,
                Description = StepDescriptions.GetDescription(7, lang),
                TechnicalDescription = $"Found Q in E[r] linearly independent from base field",
                Result = $"Q found (irrational point)",
                Details = new()
                {
                    ["Point_X_Degree"] = Q.X.Poly.Degree.ToString(),
                    ["Point_Y_Degree"] = Q.Y.Poly.Degree.ToString(),
                    ["IsIrrational"] = (Q.X.Poly.Degree > 0 || Q.Y.Poly.Degree > 0).ToString()
                }
            });

            return Q;
        }

        private ExtensionFieldElement ExecuteStep8(
            IECPoint<PrimeFieldElement> P, IECPoint<ExtensionFieldElement> Q,
            BigInteger r, BigInteger privateKey,
            EllipticCurve<PrimeFieldElement> baseCurve, ExtensionField extensionField,
            string lang, BLSSignatureResponse response)
        {
            var aP = P.Multiply(privateKey);  // a * H(m)
            var signature = TatePairing.Compute(aP, Q, r, baseCurve, extensionField);

            // Populate detailed report
            if (response.DetailedReport != null)
            {
                response.DetailedReport.SignatureCalculationDetails = new Step8Details
                {
                    PrivateKey = privateKey.ToString(),
                    MessageHashPoint_X = P.X.ToString(),
                    MessageHashPoint_Y = P.Y.ToString(),
                    TorsionPoint_X = Q.X.ToString(),
                    TorsionPoint_Y = Q.Y.ToString(),
                    MultipliedPoint_X = aP.X.ToString(),
                    MultipliedPoint_Y = aP.Y.ToString(),
                    FinalSignature = signature.ToString()
                };
            }

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 8,
                Description = StepDescriptions.GetDescription(8, lang),
                TechnicalDescription = $"Alice signature: e_r({privateKey}*H(m), Q)",
                Result = $"Signature computed",
                Details = new()
                {
                    ["aP_X"] = aP.X.ToString(),
                    ["aP_Y"] = aP.Y.ToString()
                }
            });

            return signature;
        }

        private ExtensionFieldElement ExecuteStep9(
            IECPoint<PrimeFieldElement> P, IECPoint<ExtensionFieldElement> Q,
            BigInteger r, BigInteger privateKey,
            EllipticCurve<PrimeFieldElement> baseCurve, ExtensionField extensionField,
            string lang, BLSSignatureResponse response)
        {
            var aQ = Q.Multiply(privateKey);  // a * Q
            var verification = TatePairing.Compute(P, aQ, r, baseCurve, extensionField);

            // Populate detailed report
            if (response.DetailedReport != null)
            {
                response.DetailedReport.VerificationCalculationDetails = new Step9Details
                {
                    PrivateKey = privateKey.ToString(),
                    MessageHashPoint_X = P.X.ToString(),
                    MessageHashPoint_Y = P.Y.ToString(),
                    TorsionPoint_X = Q.X.ToString(),
                    TorsionPoint_Y = Q.Y.ToString(),
                    MultipliedPoint_X = aQ.X.ToString(),
                    MultipliedPoint_Y = aQ.Y.ToString(),
                    FinalVerification = verification.ToString(),
                    SignatureMatches = false // Will be updated in ExecuteSignature
                };
            }

            response.Steps.Add(new CalculationStep
            {
                StepNumber = 9,
                Description = StepDescriptions.GetDescription(9, lang),
                TechnicalDescription = $"Bob verification: e_r(H(m), {privateKey}*Q)",
                Result = $"Verification computed",
                Details = new()
                {
                    ["Verification"] = "Computed successfully"
                }
            });

            return verification;
        }

        private IECPoint<PrimeFieldElement> HashToPointWithTracing(
            IEllipticCurve<PrimeFieldElement> curve, string message, Step3Details? trace)
        {
            var baseField = curve.A.Field;
            var p = baseField.Characteristic;

            // Encode message
            var enc = Encoding.GetEncoding(1255);
            var bytes = enc.GetBytes(message);

            if (trace != null)
            {
                trace.MessageBytesHex = BitConverter.ToString(bytes).Replace("-", " ");
            }

            BigInteger x0 = 0;
            foreach (var b in bytes)
            {
                x0 = (x0 * 256 + b) % p;
            }

            if (trace != null)
            {
                trace.EncodedInteger = x0.ToString();
                trace.InitialX0 = x0.ToString();
            }

            BigInteger aInt = curve.A.Value;
            BigInteger bInt = curve.B.Value;
            int trialCount = 0;

            for (BigInteger increment = 0; increment < p; increment++)
            {
                trialCount++;
                var xi = (x0 + increment) % p;
                var zInt = (BigInteger.Pow(xi, 3) + aInt * xi + bInt) % p;
                if (zInt < 0) zInt += p;

                BigInteger yInt = NumberTheoryUtils.SqrtModP(zInt, p);
                bool isResidue = yInt != -1;

                if (trace != null && (increment < 10 || isResidue))
                {
                    trace.Attempts.Add(new HashTrialAttempt
                    {
                        Increment = (int)increment,
                        CandidateX = xi.ToString(),
                        ComputedZ = zInt.ToString(),
                        IsQuadraticResidue = isResidue,
                        SquareRootY = isResidue ? yInt.ToString() : null
                    });
                }

                if (!isResidue) continue;

                var x = baseField.FromInt(xi);
                var y = baseField.FromInt(yInt);
                var tempPoint = curve.CreatePoint(x, y);

                if (trace != null)
                {
                    trace.PointBeforeCofactorClearing = new PointDetails
                    {
                        X = tempPoint.X.ToString(),
                        Y = tempPoint.Y.ToString(),
                        IsInfinity = tempPoint.IsInfinity
                    };
                    trace.CofactorUsed = (curve.GroupOrder / curve.R).ToString();
                }

                var cofactor = curve.GroupOrder / curve.R;
                var result = tempPoint.Multiply(cofactor);

                if (trace != null)
                {
                    trace.TrialsUntilSuccess = trialCount;
                    trace.PointAfterCofactorClearing = new PointDetails
                    {
                        X = result.X.ToString(),
                        Y = result.Y.ToString(),
                        IsInfinity = result.IsInfinity
                    };
                }

                if (!result.IsInfinity)
                {
                    return result;
                }
            }

            throw new InvalidOperationException("Failed to hash message to a curve point.");
        }
    }
}
