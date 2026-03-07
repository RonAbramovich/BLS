# Test Suite Documentation

This document describes all the test cases in the BLS signature implementation, including the mathematical calculations and expected results.

---

## 1. Prime Field Tests

### Test Class: `EllipticCurveTests.PrimeFieldElement_BasicArithmeticAndInverse`

**Purpose**: Verify basic arithmetic operations in prime fields.

**Setup**:
- Field: F_7 (prime p = 7)
- Elements: a = 3, b = 5

#### Test: Addition
- Operation: a + b = 3 + 5
- Calculation: 8 mod 7 = 1
- Expected: 1 ✓

#### Test: Multiplication
- Operation: a × b = 3 × 5
- Calculation: 15 mod 7 = 1
- Expected: 1 ✓

#### Test: Additive Inverse
- Operation: -a = -3
- Calculation: -3 mod 7 = 4
- Expected: 4 ✓
- Verification: 3 + 4 = 7 ≡ 0

#### Test: Multiplicative Inverse
- Operation: a⁻^1 where a = 3
- Calculation using Extended Euclidean Algorithm:
  - gcd(3, 7): 7 = 2×3 + 1, so 1 = 7 - 2×3
  - In F_7: 1 = -2×3, so 3⁻^1 = -2 ≡ 5
- Expected: 5 ✓
- Verification: 3 × 5 = 15 ≡ 1

#### Test: Power (Positive)
- Operation: a^2 = 3^2
- Calculation: 9 mod 7 = 2
- Expected: 2 ✓

#### Test: Power (Negative)
- Operation: a⁻^1 via Power(-1)
- Expected: Same as multiplicative inverse = 5 ✓

---

## 2. Elliptic Curve Group Order Tests

### Test Class: `EllipticCurveTests.EllipticCurve_GroupOrder_Factors_R`

**Purpose**: Verify group order computation and factorization.

**Setup**:
- Field: F_5
- Curve: y^2 = x^3 + 2 (A = 0, B = 2)

#### Calculation: Points on Curve

**Quadratic residues in F_5**: {0, 1, 4}

For each x ∈ F_5, compute z = x^3 + 2:

| x | x^3 | z = x^3 + 2 (mod 5) | Is z a square? | Number of points |
|---|----|--------------------|----------------|------------------|
| 0 | 0  | 2                  | No             | 0                |
| 1 | 1  | 3                  | No             | 0                |
| 2 | 8≡3| 10≡0               | Yes (y=0)      | 1: (2,0)         |
| 3 | 27≡2| 29≡4              | Yes (y=±2)     | 2: (3,2), (3,3)  |
| 4 | 64≡4| 66≡1              | Yes (y=±1)     | 2: (4,1), (4,4)  |

**Total points**: 1 (infinity) + 0 + 0 + 1 + 2 + 2 = **6**

#### Test: Group Order
- Expected: 6 ✓

#### Test: Factorization
- 6 = 2 × 3
- Expected factors: (2, 1) and (3, 1) ✓

#### Test: Largest Prime Divisor R
- Primes in factorization: {2, 3}
- R = max{2, 3} = 3
- Expected: 3 ✓

---

## 3. Elliptic Curve Point Arithmetic Tests

### Test Class: `EllipticCurveTests.ECPoint_IsOnCurve_Negate_Double_Multiply_Order`

**Setup**: Same curve as above (y^2 = x^3 + 2 over F_5)

**Test Points**:
- falsePoint = (2, 3)
- p1 = (2, 0)
- p2 = (3, 2)
- p2neg = (3, 3)

#### Test: Point Validation

**Check falsePoint = (2, 3)**:
- LHS: y^2 = 3^2 = 9 ≡ 4
- RHS: x^3 + 2 = 8 + 2 = 10 ≡ 0
- 4 ≠ 0 → Not on curve ✓

**Check p1 = (2, 0)**:
- LHS: 0^2 = 0
- RHS: 8 + 2 = 10 ≡ 0
- 0 = 0 → On curve ✓

**Check p2 = (3, 2)**:
- LHS: 2^2 = 4
- RHS: 27 + 2 = 29 ≡ 4
- 4 = 4 → On curve ✓

#### Test: Negation
- Operation: -p2 where p2 = (3, 2)
- Calculation: (3, -2 mod 5) = (3, 3)
- Expected: p2neg ✓

#### Test: Point Doubling

**Double p2 = (3, 2)**:

Calculate slope:
- λ = (3x^2 + A) / (2y) with A = 0
- Numerator: 3(3^2) = 3(9) = 27 ≡ 2 (mod 5)
- Denominator: 2(2) = 4
- Inverse of 4 in F_5: 4⁻^1 = 4 (since 4×4 = 16 ≡ 1)
- λ = 2 × 4 = 8 ≡ 3 (mod 5)

Calculate coordinates:
- x₃ = λ^2 - 2x = 9 - 6 = 3
- y₃ = λ(x - x₃) - y = 3(3 - 3) - 2 = -2 ≡ 3 (mod 5)
- Result: 2P = (3, 3)

Expected: (3, 3) = p2neg ✓

This shows that 2P = -P, which means 3P = O.

#### Test: Triple Point (Order Verification)
- Operation: 3 × p2
- Calculation: 3P = P + 2P = P + (-P) = O
- Expected: Point at infinity ✓

#### Test: Point Order of p2
- Group order: 6 = 2 × 3
- Testing: 3P = O (verified above)
- Testing: 2P ≠ O (2P = (3,3))
- Order: 3 ✓

#### Test: Point with y = 0

**Point p1 = (2, 0)**:

Doubling formula requires division by 2y = 0:
- When y = 0, tangent is vertical
- Vertical line → intersects at infinity
- 2P = O

Expected: 2P = O ✓

Order:
- 2P = O
- P ≠ O
- Order = 2 ✓

#### Test: Negative Scalar Multiplication
- Operation: (-1) × p2
- Expected: -p2 = (3, 3) ✓

---

## 4. Polynomial and Extension Field Tests

### Test Class: `PolynomialAndFieldTests`

#### Test: Inverse of x in F_2^2

**Setup**:
- Base field: F_2
- Modulus: g(x) = x^2 + x + 1

**Operation**: Find x⁻^1 mod (x^2 + x + 1)

**Calculation using Extended Euclidean Algorithm**:
```
gcd(x, x^2 + x + 1):
  x^2 + x + 1 = x · x + (x + 1)
  x = (x + 1) · 1 + 1
  x + 1 = 1 · (x + 1) + 0

Back-substitution:
  1 = x - (x + 1)
  1 = x - (x^2 + x + 1 - x · x)
  1 = x - x^2 - x - 1 + x^2
  1 = x + 1 (mod 2, since -1 ≡ 1)
  
Wait, let me recalculate in F_2:
  x · (x+1) = x^2 + x ≡ 1 (mod x^2+x+1) in F_2
  Because x^2+x+1 ≡ 0, so x^2+x ≡ -1 ≡ 1
```

Expected: x⁻^1 = x + 1 ✓

Verification:
- x(x + 1) = x^2 + x
- Reduce mod (x^2 + x + 1): x^2 + x ≡ -1 ≡ 1 in F_2 ✓

#### Test: Inverse of x + 1 in F_2^2

**Operation**: (x + 1)⁻^1 mod (x^2 + x + 1)

From previous test: x⁻^1 = x + 1, so (x + 1)⁻^1 = x

Expected: x ✓

#### Test: Inverse of x in F_3^2

**Setup**:
- Base field: F_3
- Modulus: g(x) = x^2 + 1

**Operation**: Find x⁻^1 mod (x^2 + 1)

**Calculation**:
- In F_3: x^2 ≡ -1 (mod x^2 + 1)
- So: x · x = x^2 ≡ -1
- Multiply both sides by -x: -x · x · x = -x · (-1) = x
- So: -x^3 = x, which gives x^3 = -x
- Actually simpler: x · (-x) = -x^2 ≡ -(-1) = 1
- But -x ≡ 2x in F_3 (since -1 ≡ 2)
- Therefore: x⁻^1 = 2x ✓

Expected: 2x ✓

Verification:
- x · (2x) = 2x^2
- Reduce: 2x^2 ≡ 2(-1) = -2 ≡ 1 (mod 3) ✓

#### Test: Zero Has No Inverse
- Expected: InvalidOperationException ✓

---

## 5. Extension Field Tests

### Test Class: `ExtensionFieldTests`

These tests mirror the polynomial tests but use the `ExtensionField` and `ExtensionFieldElement` classes.

All calculations and expected results are identical to the polynomial tests above.

#### Additional Test: Equivalent Polynomials

**Setup**: F_3^2, modulus x^2 + 1

**Test**: Verify that x^2 reduces to constant 2

- Element from x^2: polynomial [0, 0, 1]
- Reduce mod (x^2 + 1): x^2 ≡ -1 ≡ 2 in F_3
- Expected: Equals field element from integer 2 ✓

#### Test: FromInt Addition

**Setup**: F_7^2, modulus x^2 + 1

- a = 3, b = 5 (constant polynomials)
- a + b = 8 ≡ 1 (mod 7)
- Expected: 1 ✓

---

## 6. Irreducible Polynomial Finder Tests

### Test Class: `IrreduciblePolynomialFinderTests.FindIrreduciblePolynomial_F2_Degree2`

**Purpose**: Test automatic search for irreducible polynomials.

**Setup**:
- Base field: F_2
- r = 3 (desired subgroup order)

**Step 1 - Find Embedding Degree**:
- Need k where r | (2^k - 1)
- k=1: 2^1 - 1 = 1, 1 mod 3 ≠ 0
- k=2: 2^2 - 1 = 3, 3 mod 3 = 0 ✓
- Embedding degree: k = 2

**Step 2 - Search for Irreducible Polynomial**:

Candidates (monic degree-2 over F_2):
1. x^2: Clearly reducible
2. x^2 + 1: Factors as (x+1)^2 in F_2
3. x^2 + x: Factors as x(x+1)
4. x^2 + x + 1: Test using Rabin

**Rabin's Test for x^2 + x + 1**:
- Condition 1: x^4 ≡ x (mod x^2+x+1) ✓ (verified in doc 03)
- Condition 2: gcd(x^2+x+1, x^2-x) = 1 ✓ (verified in doc 03)
- Result: Irreducible ✓

Expected: x^2 + x + 1 ✓

---

## 7. Hash-to-Point Tests

### Test Class: `HashToPointTests`

#### Test: Determinism and Subgroup Membership

**Setup**:
- Prime: p = 7
- Curve: y^2 = x^3 + 1 over F_7
- Message: "hello"

**Test 1 - Deterministic**:
- Compute H₁ = HashToCurve("hello")
- Compute H₂ = HashToCurve("hello")
- Expected: H₁ = H₂ ✓

**Test 2 - Not Infinity**:
- Expected: H₁ is not point at infinity ✓

**Test 3 - In r-Subgroup**:
- Compute: r × H₁
- Expected: Point at infinity ✓
- This proves order of H₁ divides r

#### Test: Hebrew Message Support

**Setup**: Same curve
- Message: "שלום" (Hebrew: "hello")

**Tests**:
- Hash completes without error ✓
- Result is not infinity ✓
- r × H = O (in subgroup) ✓

This verifies Windows-1255 encoding works correctly.

#### Test: Prime Requirement

**Setup**:
- Prime: p = 5 (note: 5 ≡ 1 mod 4, not 3)
- Curve: y^2 = x^3 + 1

**Expected**: NotSupportedException ✓

Reason: Square root algorithm requires p ≡ 3 (mod 4).

---

## 8. Test Coverage Summary

### Field Arithmetic
- ✓ Addition, subtraction (prime fields)
- ✓ Multiplication
- ✓ Additive inverse
- ✓ Multiplicative inverse (Extended Euclidean)
- ✓ Exponentiation (positive and negative)

### Polynomial Arithmetic
- ✓ Polynomial inverse modulo irreducible polynomial
- ✓ Multiple base fields (F_2, F_3)
- ✓ Zero inverse throws exception

### Extension Fields
- ✓ Construction from irreducible polynomial
- ✓ Element arithmetic (addition, multiplication, inverse)
- ✓ Reduction modulo irreducible polynomial
- ✓ Equivalence of different representations

### Elliptic Curves
- ✓ Point validation (on curve vs. not on curve)
- ✓ Group order computation via direct counting
- ✓ Factorization of group order
- ✓ Largest prime divisor R
- ✓ Point negation
- ✓ Point addition (general case)
- ✓ Point doubling
- ✓ Scalar multiplication
- ✓ Point order computation
- ✓ Special cases (y=0, vertical tangent)

### Irreducible Polynomials
- ✓ Automatic search for irreducible polynomial
- ✓ Embedding degree computation
- ✓ Rabin's irreducibility test

### Hash-to-Point
- ✓ Deterministic mapping
- ✓ Subgroup membership
- ✓ Unicode/Hebrew support
- ✓ Prime requirement validation

---

## 9. Test Data Reference

### Curve: y^2 = x^3 + 2 over F_5

Complete point enumeration:

| Point      | Verification               | Order |
|------------|----------------------------|-------|
| O (inf)    | Identity                   | 1     |
| (2, 0)     | 0 = 8+2 ≡ 0 ✓             | 2     |
| (3, 2)     | 4 = 27+2 ≡ 4 ✓            | 3     |
| (3, 3)     | 9 = 27+2 ≡ 4, 9≡4 ✓       | 3     |
| (4, 1)     | 1 = 64+2 ≡ 1 ✓            | 6     |
| (4, 4)     | 16 = 64+2 ≡ 1, 16≡1 ✓     | 6     |

Group structure: Cyclic group of order 6 ≅ ℤ/2ℤ × ℤ/3ℤ

---

## Summary

The test suite provides comprehensive coverage of:
- **Core algorithms**: Field arithmetic, polynomial operations, elliptic curve arithmetic
- **Cryptographic components**: Irreducible polynomial finding, hash-to-point mapping
- **Edge cases**: Zero inverses, special points, prime requirements
- **Correctness**: All calculations verified by hand using modular arithmetic

All tests include detailed mathematical verification to ensure the implementation matches theoretical expectations.
