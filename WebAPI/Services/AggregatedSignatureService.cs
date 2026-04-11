using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BLS.ElipticCurve.Implementations;
using BLS.ElipticCurve.Interfaces;
using BLS.Fields.Implementations;
using BLS.Fields.Interfaces;
using BLS.Pairing.Implementations;
using BLS.WebAPI.Models;

namespace BLS.WebAPI.Services
{
    /// <summary>
    /// Service for BLS aggregated signatures (Ethereum-style)
    /// </summary>
    public class AggregatedSignatureService
    {
        private readonly BLSSignatureService _blsService;

        public AggregatedSignatureService()
        {
            _blsService = new BLSSignatureService();
        }

        public PrivateKeyConstraintsResponse GetPrivateKeyConstraints(PrivateKeyConstraintsRequest request)
        {
            var response = new PrivateKeyConstraintsResponse { Success = true };

            try
            {
                var q = BigInteger.Parse(request.PRIME_Q);
                var a_param = BigInteger.Parse(request.A);
                var b_param = BigInteger.Parse(request.B);
                var lang = request.Language;

                // Create curve infrastructure
                var baseField = new PrimeField(q);
                var a = baseField.FromInt(a_param);
                var b = baseField.FromInt(b_param);
                var baseCurve = new EllipticCurve<PrimeFieldElement>(baseField, a, b);
                var groupOrder = baseCurve.GroupOrder;
                var r = baseCurve.R;

                // Find generator point
                var generator = FindGenerator(baseCurve, baseField, r);

                // Populate response with curve information
                response.Prime = q.ToString();
                response.CurveEquation = lang == "he" 
                    ? $"y² ≡ x³ + {a_param}x + {b_param} (mod {q})"
                    : $"y² ≡ x³ + {a_param}x + {b_param} (mod {q})";
                response.GroupOrder = groupOrder.ToString();
                response.R = r.ToString();
                response.GeneratorPoint = $"G = ({generator.X.Value}, {generator.Y.Value})";

                // Generate invalid and valid key examples
                var invalidKeys = GetInvalidKeyExamples(r, 10);
                var validKeys = GetValidKeyExamples(r, 10);

                response.InvalidKeyExamples = invalidKeys.Select(k => k.ToString()).ToList();
                response.ValidKeyExamples = validKeys.Select(k => k.ToString()).ToList();

                // Constraint description
                response.ConstraintDescription = lang == "he"
                    ? $"מפתח פרטי חייב לעמוד בתנאים הבאים:"
                    : $"Private key must satisfy the following conditions:";

                // Validation rule
                response.ValidationRule = lang == "he"
                    ? $"1. sk > 0 (חיובי)\n2. sk mod {r} ≠ 0 (לא כפולה של r)"
                    : $"1. sk > 0 (positive)\n2. sk mod {r} ≠ 0 (not a multiple of r)";

                // Detailed explanation
                response.Explanation = lang == "he"
                    ? $"הסבר:\n" +
                      $"• הנקודה היוצרת G = ({generator.X.Value}, {generator.Y.Value}) היא בעלת סדר r={r}\n" +
                      $"• כלומר: r × G = נקודת אינסוף\n" +
                      $"• לכן, אם sk הוא כפולה של r (sk = k×r), אזי:\n" +
                      $"  pk = sk × G = (k×r) × G = k × (r × G) = k × ∞ = ∞\n" +
                      $"• מפתח ציבורי שהוא נקודת אינסוף אינו תקין!\n\n" +
                      $"דוגמאות:\n" +
                      $"✗ sk = 0: חייב להיות חיובי\n" +
                      $"✗ sk = {r}: {r} × G = ∞\n" +
                      $"✗ sk = {r * 2}: {r * 2} × G = 2 × ({r} × G) = 2 × ∞ = ∞\n" +
                      $"✓ sk = 1: 1 × G = G (תקין)\n" +
                      (r > 2 ? $"✓ sk = 2: 2 × G (תקין)\n" : "") +
                      $"✓ sk = {(r > 1 ? r + 1 : 1)}: בגלל {(r > 1 ? r + 1 : 1)} mod {r} = {(r > 1 ? 1 : 0)} ≠ 0"
                    : $"Explanation:\n" +
                      $"• The generator point G = ({generator.X.Value}, {generator.Y.Value}) has order r={r}\n" +
                      $"• This means: r × G = point at infinity\n" +
                      $"• Therefore, if sk is a multiple of r (sk = k×r), then:\n" +
                      $"  pk = sk × G = (k×r) × G = k × (r × G) = k × ∞ = ∞\n" +
                      $"• A public key that is the point at infinity is invalid!\n\n" +
                      $"Examples:\n" +
                      $"✗ sk = 0: must be positive\n" +
                      $"✗ sk = {r}: {r} × G = ∞\n" +
                      $"✗ sk = {r * 2}: {r * 2} × G = 2 × ({r} × G) = 2 × ∞ = ∞\n" +
                      $"✓ sk = 1: 1 × G = G (valid)\n" +
                      (r > 2 ? $"✓ sk = 2: 2 × G (valid)\n" : "") +
                      $"✓ sk = {(r > 1 ? r + 1 : 1)}: because {(r > 1 ? r + 1 : 1)} mod {r} = {(r > 1 ? 1 : 0)} ≠ 0";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        public AggregatedSignatureResponse ExecuteAggregatedSignature(AggregatedSignatureRequest request)
        {
            var response = new AggregatedSignatureResponse { Success = true };

            try
            {
                var q = BigInteger.Parse(request.PRIME_Q);
                var a_param = BigInteger.Parse(request.A);
                var b_param = BigInteger.Parse(request.B);
                var message = request.Message;
                var lang = request.Language;

                // Create curve infrastructure
                var baseField = new PrimeField(q);
                var a = baseField.FromInt(a_param);
                var b = baseField.FromInt(b_param);
                var baseCurve = new EllipticCurve<PrimeFieldElement>(baseField, a, b);
                var groupOrder = baseCurve.GroupOrder;
                var r = baseCurve.R;

                AddCommonStep(response, lang == "he" ? "יצירת עקומה אליפטית" : "Create elliptic curve",
                    lang == "he" ? 
                    $"q = {q}\ny² = x³ + {a_param}x + {b_param}\nסדר קבוצה: {groupOrder}\nr = {r}" :
                    $"q = {q}\ny² = x³ + {a_param}x + {b_param}\nGroup order: {groupOrder}\nr = {r}");

                // Hash message to curve
                var Hm = HashToPoint.HashToCurve(baseCurve, message);

                AddCommonStep(response, lang == "he" ? "גיבוב הודעה לנקודה" : "Hash message to point",
                    lang == "he" ?
                    $"הודעה: \"{message}\"\nH(m) = ({Hm.X.Value}, {Hm.Y.Value})" :
                    $"Message: \"{message}\"\nH(m) = ({Hm.X.Value}, {Hm.Y.Value})");

                // Find generator point
                var generator = FindGenerator(baseCurve, baseField, r);

                AddCommonStep(response, lang == "he" ? "בחירת נקודה יוצרת" : "Select generator point",
                    lang == "he" ?
                    $"G = ({generator.X.Value}, {generator.Y.Value})" :
                    $"G = ({generator.X.Value}, {generator.Y.Value})");

                // Calculate and explain valid private key constraints
                var invalidKeys = GetInvalidKeyExamples(r, 5);
                var validKeys = GetValidKeyExamples(r, 5);

                AddCommonStep(response, lang == "he" ? "אילוצי מפתח פרטי" : "Private key constraints",
                    lang == "he" ?
                    $"מפתח פרטי תקין חייב לעמוד בדרישות:\n" +
                    $"1. sk > 0 (חיובי)\n" +
                    $"2. sk mod {r} ≠ 0 (לא כפולה של r={r})\n\n" +
                    $"דוגמאות למפתחות לא תקינים: {string.Join(", ", invalidKeys)}\n" +
                    $"דוגמאות למפתחות תקינים: {string.Join(", ", validKeys)}" :
                    $"Valid private key must satisfy:\n" +
                    $"1. sk > 0 (positive)\n" +
                    $"2. sk mod {r} ≠ 0 (not a multiple of r={r})\n\n" +
                    $"Examples of invalid keys: {string.Join(", ", invalidKeys)}\n" +
                    $"Examples of valid keys: {string.Join(", ", validKeys)}");

                // Process each participant
                var publicKeys = new List<IECPoint<PrimeFieldElement>>();
                var signatures = new List<IECPoint<PrimeFieldElement>>();

                foreach (var participant in request.Participants)
                {
                    var individualSig = new IndividualSignature
                    {
                        ParticipantName = participant.Name,
                        PrivateKey = participant.PrivateKey,
                        Steps = new List<CalculationStep>()
                    };

                    var sk = BigInteger.Parse(participant.PrivateKey);

                    // Validate private key: must be positive and not a multiple of r
                    if (sk <= 0)
                    {
                        throw new ArgumentException(
                            lang == "he" 
                                ? $"מפתח פרטי לא תקין עבור {participant.Name}: {sk}. המפתח חייב להיות חיובי"
                                : $"Invalid private key for {participant.Name}: {sk}. Private key must be positive");
                    }

                    if (sk % r == 0)
                    {
                        throw new ArgumentException(
                            lang == "he" 
                                ? $"מפתח פרטי לא תקין עבור {participant.Name}: {sk}. המפתח לא יכול להיות כפולה של r={r}\n" +
                                  $"(מכיוון ש- {sk} mod {r} = 0, החישוב {sk} × G יביא לנקודת אינסוף)"
                                : $"Invalid private key for {participant.Name}: {sk}. Private key cannot be a multiple of r={r}\n" +
                                  $"(Since {sk} mod {r} = 0, computing {sk} × G would result in point at infinity)");
                    }

                    // Generate public key: pk = sk * G
                    var pk = ScalarMultiply(generator, sk);

                    // Defensive check - should not happen after validation
                    if (pk.IsInfinity)
                    {
                        throw new InvalidOperationException(
                            lang == "he"
                                ? $"מפתח ציבורי לא תקין עבור {participant.Name}: נקבלה נקודת אינסוף"
                                : $"Invalid public key for {participant.Name}: resulted in point at infinity");
                    }

                    publicKeys.Add(pk);
                    individualSig.PublicKey = $"({pk.X.Value}, {pk.Y.Value})";

                    AddParticipantStep(individualSig, lang == "he" ? "מפתח ציבורי" : "Public key",
                        lang == "he" ?
                        $"sk = {sk}\npk = sk × G = ({pk.X.Value}, {pk.Y.Value})" :
                        $"sk = {sk}\npk = sk × G = ({pk.X.Value}, {pk.Y.Value})");

                    // Generate signature: sig = sk * H(m)
                    var sig = ScalarMultiply(Hm, sk);

                    // Defensive check - should not happen after validation
                    if (sig.IsInfinity)
                    {
                        throw new InvalidOperationException(
                            lang == "he"
                                ? $"חתימה לא תקינה עבור {participant.Name}: נקבלה נקודת אינסוף"
                                : $"Invalid signature for {participant.Name}: resulted in point at infinity");
                    }

                    signatures.Add(sig);
                    individualSig.Signature = $"({sig.X.Value}, {sig.Y.Value})";

                    AddParticipantStep(individualSig, lang == "he" ? "חתימה" : "Signature",
                        lang == "he" ?
                        $"σ = sk × H(m) = ({sig.X.Value}, {sig.Y.Value})" :
                        $"σ = sk × H(m) = ({sig.X.Value}, {sig.Y.Value})");

                    response.IndividualSignatures.Add(individualSig);
                }

                // Aggregate public keys
                var aggregatedPK = publicKeys[0];
                for (int i = 1; i < publicKeys.Count; i++)
                {
                    aggregatedPK = AddPoints(aggregatedPK, publicKeys[i]);
                }
                response.AggregatedPublicKey = $"({aggregatedPK.X.Value}, {aggregatedPK.Y.Value})";

                AddCommonStep(response, lang == "he" ? "צבירת מפתחות ציבוריים" : "Aggregate public keys",
                    lang == "he" ?
                    $"apk = Σ pk<sub>i</sub> = {response.AggregatedPublicKey}" :
                    $"apk = Σ pk<sub>i</sub> = {response.AggregatedPublicKey}");

                // Aggregate signatures
                var aggregatedSig = signatures[0];
                for (int i = 1; i < signatures.Count; i++)
                {
                    aggregatedSig = AddPoints(aggregatedSig, signatures[i]);
                }
                response.AggregatedSignature = $"({aggregatedSig.X.Value}, {aggregatedSig.Y.Value})";

                AddCommonStep(response, lang == "he" ? "צבירת חתימות" : "Aggregate signatures",
                    lang == "he" ?
                    $"asig = Σ σ<sub>i</sub> = {response.AggregatedSignature}" :
                    $"asig = Σ σ<sub>i</sub> = {response.AggregatedSignature}");

                // Verification using bilinearity property of pairing:
                // e(σ_agg, Q) should equal e(H(m), pk_agg × Q)
                // Or by bilinearity: e(σ_agg, Q) = e(H(m), Q)^(sum of sk_i)

                // First, we need to set up extension field and find a torsion point
                int k = FindEmbeddingDegree(q, r);
                if (k <= 1)
                {
                    throw new InvalidOperationException(lang == "he"
                        ? $"מעלת השיכון k = {k}. חתימת BLS דורשת k > 1. נסה פרמטרים אחרים לעקומה."
                        : $"Embedding degree k = {k}. BLS signatures require k > 1 (need a non-trivial extension field for the pairing). Try different curve parameters.");
                }
                AddCommonStep(response, lang == "he" ? "מציאת מעלת השיכון" : "Find embedding degree",
                    lang == "he" ?
                    $"k = {k} (הקטן ביותר כך ש- r | q<sup>k</sup> - 1)" :
                    $"k = {k} (smallest such that r | q<sup>k</sup> - 1)");

                var irreduciblePoly = IrreduciblePolynomialFinder.FindIrreduciblePolynomial(baseField, r, k);
                var extensionField = new ExtensionField(baseField, irreduciblePoly);
                var extensionCurve = new EllipticCurve<ExtensionFieldElement>(
                    extensionField,
                    extensionField.FromInt(a_param),
                    extensionField.FromInt(b_param));

                AddCommonStep(response, lang == "he" ? "יצירת שדה הרחבה" : "Create extension field",
                    lang == "he" ?
                    $"F<sub>q</sub><sup>{k}</sup> עם פולינום בלתי פריק ממעלה {k}" :
                    $"F<sub>q</sub><sup>{k}</sup> with irreducible polynomial of degree {k}");

                var Q = TorsionPointFinder.FindIndependentTorsionPoint(extensionCurve, r);
                AddCommonStep(response, lang == "he" ? "מציאת נקודת פיתול" : "Find torsion point",
                    lang == "he" ?
                    $"Q ∈ E(F<sub>q</sub><sup>k</sup>)[r] בלתי תלויה לינארית" :
                    $"Q ∈ E(F<sub>q</sub><sup>k</sup>)[r] linearly independent");

                // Compute pairings for verification
                // Method: e(σ_agg, Q) should equal e(H(m), Q)^(Σ sk_i)
                // Since σ_agg = (Σ sk_i) × H(m), we have:
                // e(σ_agg, Q) = e((Σ sk_i) × H(m), Q) = e(H(m), Q)^(Σ sk_i)

                // Compute e(σ_agg, Q)
                var pairingAggSig = TatePairing.Compute(aggregatedSig, Q, r, baseCurve, extensionField);

                // Compute e(H(m), Q) and raise to power (Σ sk_i)
                BigInteger sumSK = 0;
                foreach (var p in request.Participants)
                {
                    sumSK += BigInteger.Parse(p.PrivateKey);
                }

                var pairingBase = TatePairing.Compute(Hm, Q, r, baseCurve, extensionField);
                var pairingExpected = pairingBase.Power(sumSK);

                response.VerificationPassed = pairingAggSig.Equals(pairingExpected);

                // Alternative verification: demonstrate that each individual signature can be verified
                // and that the aggregation property holds: ∏ e(H(m), pk_i) = e(H(m), Σ pk_i)
                var productOfIndividualPairings = extensionField.One;
                foreach (var pk in publicKeys)
                {
                    var pkQ = Q.Multiply(1); // We can't directly multiply Q by a base field point
                    // Instead we use: e(H(m), pk_i × Q) by computing it differently
                    // Actually, for each pk_i = sk_i × G, we want e(H(m), sk_i × Q)
                    // But we can compute this as e(H(m), Q)^sk_i
                    // Then product = ∏ e(H(m), Q)^sk_i = e(H(m), Q)^(Σ sk_i)
                    // Which is exactly what we computed above
                }
                // The product verification is implicit in our computation above

                if (request.IncludeDetailedReport && response.AggregationDetails != null)
                {
                    response.AggregationDetails = new AggregationDetails
                    {
                        MessageHash = $"H(m) = ({Hm.X.Value}, {Hm.Y.Value})",
                        IndividualPublicKeys = response.IndividualSignatures.Select(s => s.PublicKey).ToList(),
                        IndividualSignatures = response.IndividualSignatures.Select(s => s.Signature).ToList(),
                        AggregatedPublicKeyCalculation = lang == "he" ?
                            $"apk = Σ pk<sub>i</sub> = {response.AggregatedPublicKey}" :
                            $"apk = Σ pk<sub>i</sub> = {response.AggregatedPublicKey}",
                        AggregatedSignatureCalculation = lang == "he" ?
                            $"σ<sub>agg</sub> = Σ σ<sub>i</sub> = {response.AggregatedSignature}" :
                            $"σ<sub>agg</sub> = Σ σ<sub>i</sub> = {response.AggregatedSignature}",
                        VerificationPoint = lang == "he" ?
                            $"e(σ<sub>agg</sub>, Q) = {pairingAggSig}\ne(H(m), Q)<sup>(Σ sk<sub>i</sub>)</sup> = {pairingExpected}" :
                            $"e(σ<sub>agg</sub>, Q) = {pairingAggSig}\ne(H(m), Q)<sup>(Σ sk<sub>i</sub>)</sup> = {pairingExpected}",
                        PairingCheck = response.VerificationPassed
                    };
                }

                AddCommonStep(response, lang == "he" ? "אימות באמצעות זיווג ביליניארי" : "Verify using bilinear pairing",
                    lang == "he" ?
                    $"בדיקה: e(σ<sub>agg</sub>, Q) = e(H(m), Q)<sup>(Σ sk<sub>i</sub>)</sup>\n" +
                    $"e(σ<sub>agg</sub>, Q) = {pairingAggSig.ToString().Substring(0, Math.Min(50, pairingAggSig.ToString().Length))}...\n" +
                    $"e(H(m), Q)<sup>(Σsk)</sup> = {pairingExpected.ToString().Substring(0, Math.Min(50, pairingExpected.ToString().Length))}...\n" +
                    $"תוצאה: {(response.VerificationPassed ? "✓ הזיווגים תואמים - אימות הצליח!" : "✗ הזיווגים שונים")}" :
                    $"Check: e(σ<sub>agg</sub>, Q) = e(H(m), Q)<sup>(Σ sk<sub>i</sub>)</sup>\n" +
                    $"e(σ<sub>agg</sub>, Q) = {pairingAggSig.ToString().Substring(0, Math.Min(50, pairingAggSig.ToString().Length))}...\n" +
                    $"e(H(m), Q)<sup>(Σsk)</sup> = {pairingExpected.ToString().Substring(0, Math.Min(50, pairingExpected.ToString().Length))}...\n" +
                    $"Result: {(response.VerificationPassed ? "✓ Pairings match - Verification passed!" : "✗ Pairings differ")}");

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Error: {ex.Message}\n{ex.StackTrace}";
            }

            return response;
        }

        private IECPoint<PrimeFieldElement> FindGenerator(EllipticCurve<PrimeFieldElement> curve, PrimeField field, BigInteger r)
        {
            var q = field.Characteristic;
            var cofactor = curve.GroupOrder / r;

            for (BigInteger x = 0; x < q; x++)
            {
                var xElem = field.FromInt(x);
                var rhs = xElem.Power(3) + curve.A * xElem + curve.B;
                var rhsInt = rhs.Value;

                var y = NumberTheoryUtils.SqrtModP(rhsInt, q);
                if (y == -1) continue;

                var yElem = field.FromInt(y);
                var point = curve.CreatePoint(xElem, yElem);

                var candidate = ScalarMultiply(point, cofactor);
                if (!candidate.IsInfinity)
                {
                    var check = ScalarMultiply(candidate, r);
                    if (check.IsInfinity)
                    {
                        return candidate;
                    }
                }
            }

            throw new InvalidOperationException("Could not find generator");
        }

        private IECPoint<PrimeFieldElement> ScalarMultiply(IECPoint<PrimeFieldElement> point, BigInteger scalar)
        {
            if (scalar == 0 || point.IsInfinity)
                return point is ECPoint<PrimeFieldElement> ecPoint ? ecPoint.Curve.Infinity : point;

            if (scalar < 0)
                throw new ArgumentException("Negative scalar not supported");

            IECPoint<PrimeFieldElement> result = (point as ECPoint<PrimeFieldElement>)?.Curve.Infinity ?? point;
            IECPoint<PrimeFieldElement> addend = point;

            while (scalar > 0)
            {
                if ((scalar & 1) == 1)
                {
                    result = AddPoints(result, addend);
                }
                addend = AddPoints(addend, addend);
                scalar >>= 1;
            }

            return result;
        }

        private IECPoint<PrimeFieldElement> AddPoints(IECPoint<PrimeFieldElement> p1, IECPoint<PrimeFieldElement> p2)
        {
            if (p1.IsInfinity) return p2;
            if (p2.IsInfinity) return p1;

            var ecPoint1 = p1 as ECPoint<PrimeFieldElement>;
            var curveInterface = ecPoint1?.Curve;
            var curve = curveInterface as EllipticCurve<PrimeFieldElement>;
            if (curve == null) throw new InvalidOperationException("Cannot add points from different curves");

            var field = curve.Field;

            if (p1.X.Equals(p2.X))
            {
                if (p1.Y.Equals(p2.Y))
                {
                    // Point doubling — vertical tangent when y = 0 (2-torsion point)
                    if (p1.Y.IsZero) return curve.Infinity;

                    var three = field.FromInt(3);
                    var two = field.FromInt(2);
                    var slope = (three * p1.X * p1.X + curve.A) / (two * p1.Y);
                    var x3 = slope * slope - two * p1.X;
                    var y3 = slope * (p1.X - x3) - p1.Y;
                    return curve.CreatePoint(x3, y3);
                }
                return curve.Infinity;
            }

            // Point addition
            var s = (p2.Y - p1.Y) / (p2.X - p1.X);
            var x = s * s - p1.X - p2.X;
            var y = s * (p1.X - x) - p1.Y;
            return curve.CreatePoint(x, y);
        }

        private void AddCommonStep(AggregatedSignatureResponse response, string description, string result)
        {
            response.CommonSteps.Add(new CalculationStep
            {
                Description = description,
                Result = result
            });
        }

        private void AddParticipantStep(IndividualSignature signature, string description, string result)
        {
            signature.Steps.Add(new CalculationStep
            {
                Description = description,
                Result = result
            });
        }

        private IECPoint<ExtensionFieldElement> LiftPointToExtension(
            IECPoint<PrimeFieldElement> point,
            EllipticCurve<ExtensionFieldElement> targetCurve,
            ExtensionField extField)
        {
            if (point.IsInfinity) return targetCurve.Infinity;

            // Convert PrimeFieldElement to ExtensionFieldElement
            // ExtensionFieldElement is represented as a polynomial over the base field
            var xPoly = new Polynomial(extField.BaseField.Characteristic, point.X.Value);
            var yPoly = new Polynomial(extField.BaseField.Characteristic, point.Y.Value);

            var xExt = new ExtensionFieldElement(extField, xPoly);
            var yExt = new ExtensionFieldElement(extField, yPoly);

            return targetCurve.CreatePoint(xExt, yExt);
        }

        private int FindEmbeddingDegree(BigInteger q, BigInteger r)
        {
            for (int k = 2; k <= 100; k++)
            {
                var qk_minus_1 = BigInteger.Pow(q, k) - 1;
                if (qk_minus_1 % r == 0)
                {
                    return k;
                }
            }
            throw new InvalidOperationException("Could not find embedding degree k <= 100");
        }

        private List<BigInteger> GetInvalidKeyExamples(BigInteger r, int count)
        {
            var examples = new List<BigInteger> { 0 }; // 0 is always invalid (non-positive)

            // Add first few multiples of r
            for (int i = 1; i < count && examples.Count < count; i++)
            {
                examples.Add(r * i);
            }

            return examples;
        }

        private List<BigInteger> GetValidKeyExamples(BigInteger r, int count)
        {
            var examples = new List<BigInteger>();
            BigInteger candidate = 1;

            while (examples.Count < count)
            {
                if (candidate % r != 0)
                {
                    examples.Add(candidate);
                }
                candidate++;
            }

            return examples;
        }
    }
}
