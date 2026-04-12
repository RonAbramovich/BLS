# Finding an Independent Torsion Point

This document describes the algorithm for finding a point Q in E[r] over F_{q^k} that is linearly independent from the base-field generator P, a prerequisite for the non-degenerate Tate pairing used in BLS signature verification.

---

## 1. Why We Need an Independent Torsion Point

### 1.1 The Pairing Inputs

BLS verification requires computing a bilinear pairing e(P, Q) where:
- P is a point in G1 = E(F_q)[r] (base field, order r)
- Q is a point in G2 = E(F_{q^k})[r] (extension field, order r)

For the pairing to be **non-degenerate** (i.e., e(P, Q) != 1), P and Q must span the full r-torsion group E[r]. This requires Q to be **linearly independent** from P — that is, Q cannot be a scalar multiple of P.

### 1.2 The r-Torsion Group Structure

Over F_{q^k}, the full r-torsion group is:
```
E[r] = {R in E(F_{q^k}) : r*R = O}  (isomorphic to Z/r x Z/r)
```

Points in E(F_q)[r] all lie in the "rational eigenspace" of Frobenius. Q must come from the complementary eigenspace.

---

## 2. Algorithm Overview

### 2.1 High-Level Steps

```
FindIndependentTorsionPoint(curve over F_{q^k}, r):
    N_k = #E(F_{q^k})                  // from Frobenius recurrence (see doc 06)
    cofactor = N_k with all r-factors stripped  // coprime to r

    repeat:
        T = random point in E(F_{q^k})
        S = cofactor * T               // project onto r-primary part

        if S = O: retry

        Q = Frobenius(S) - S           // project out rational eigenspace

        if Q = O: retry

        Q = reduce Q into E[r]         // strip higher r-powers

        if Q != O: return Q

    raise error
```

**Implementation**: `TorsionPointFinder.FindIndependentTorsionPoint()` in `ElipticCurve/Implementations/TorsionPointFinder.cs`

---

## 3. Step 1: Cofactor Projection

### 3.1 Purpose

A random point T in E(F_{q^k}) may not lie in E[r] or its multiples. Multiplying by the **cofactor** (N_k with all factors of r stripped) moves T into the r-primary subgroup without landing exactly at O (since the cofactor is coprime to r).

### 3.2 Algorithm

```
ComputeCofactor(N_k, r):
    cofactor = N_k
    while cofactor mod r = 0:
        cofactor = cofactor / r
    return cofactor    // result satisfies gcd(cofactor, r) = 1
```

```
ProjectToRPrimary(T, cofactor):
    S = cofactor * T
    return S
```

### 3.3 Why gcd(cofactor, r) = 1 Matters

Since gcd(cofactor, r) = 1, the map T -> cofactor * T is injective on the r^e-torsion subgroup. Specifically:
- If T has a non-trivial component in E[r^e], then S = cofactor * T also has a non-trivial component in E[r^e].
- The multiplication does not "accidentally" zero out the r-primary part that we need for Q.

### 3.4 Example

Curve with N_k = 36, r = 3:
- Strip r-factors: 36 / 3 = 12, 12 / 3 = 4 (not divisible by 3)
- cofactor = 4
- S = 4 * T for a random T

---

## 4. Step 2: Frobenius Endomorphism

### 4.1 Definition

The **Frobenius endomorphism** pi acts on a point (x, y) in E(F_{q^k}) as:
```
pi(x, y) = (x^q, y^q)
```

where the power q acts coefficient-wise on the polynomial representation of the extension field element.

### 4.2 Eigenspaces

The Frobenius has two eigenspaces on E[r] (when r does not divide q-1):
- **Eigenvalue 1 (rational)**: points fixed by pi, which are exactly E(F_q)[r]
- **Eigenvalue q (non-rational)**: the complementary eigenspace, where Q must live

### 4.3 Projecting Out the Rational Eigenspace

The combination:
```
Q = pi(S) - S
```

annihilates the eigenvalue-1 component and doubles the eigenvalue-q component:
- If S = S_1 + S_q (decomposed by eigenspace), then pi(S) = S_1 + q * S_q
- pi(S) - S = (q - 1) * S_q, which is non-zero when S_q != O and r does not divide (q-1)

**Implementation**: `TorsionPointFinder.ApplyFrobeniusEndomorphism()` computes (x^q, y^q) using `ExtensionFieldElement.Power(q)`.

### 4.4 Example

For F_{5^2} with q = 5, a point S = (x_0 + x_1*u, y_0 + y_1*u) in E(F_{25}):
- pi(S) = (x_0^5 + x_1^5 * u^5, y_0^5 + y_1^5 * u^5)
- Since u^5 is computed mod the irreducible polynomial, this reduces to a different point
- Q = pi(S) - S lies in the non-rational eigenspace

---

## 5. Step 3: E[r] Reduction

### 5.1 Purpose

After the Frobenius step, Q lies in E[r^e] for some e >= 1 but may not yet be in E[r] exactly. We reduce it by repeatedly multiplying by r until r*Q = O.

### 5.2 Algorithm

```
ReduceIntoEr(Q, r):
    rQ = r * Q
    while rQ != O:
        Q = rQ
        rQ = r * Q
    return Q    // now r * Q = O, so Q in E[r]
```

### 5.3 Why This Works

The r-primary subgroup of an abelian group is a chain:
```
E[r^e] ⊃ E[r^{e-1}] ⊃ ... ⊃ E[r] ⊃ {O}
```

Starting from Q in E[r^e], multiplying by r moves down one level. After e-1 multiplications, we reach a point in E[r] \ {O}.

### 5.4 Example

Suppose Q has order r^2 = 9:
- r * Q has order r = 3 (non-zero)
- r * (r * Q) = r^2 * Q = O
- So Q_reduced = r * Q is in E[r]

---

## 6. Random Point Generation over Extension Fields

### 6.1 Method

To generate a random point T in E(F_{q^k}):
1. Choose random coefficients c_0, ..., c_{k-1} in F_q
2. Set x = c_0 + c_1*u + ... + c_{k-1}*u^{k-1}  (an element of F_{q^k})
3. Compute rhs = x^3 + A*x + B  (in F_{q^k})
4. Attempt to compute y = sqrt(rhs) in F_{q^k}
5. If successful, return the point (x, y)

### 6.2 Square Root in Extension Fields (Tonelli-Shanks)

Computing square roots in F_{q^k} is more complex than in F_q. The implementation uses `NumberTheoryUtils.SqrtModExtensionField()`, which applies the Tonelli-Shanks algorithm adapted to extension field arithmetic:
```
SqrtModExtensionField(a, field):
    // Find s, d such that q^k - 1 = 2^s * d with d odd
    // Use random non-residue z in F_{q^k}
    // Apply Tonelli-Shanks iteration with field arithmetic
    return sqrt_a  // or null if no square root exists
```

Roughly half of all extension field elements are quadratic residues, so on average two random x-values need to be tried.

**Implementation**: `TorsionPointFinder.GenerateRandomPoint()` wraps `NumberTheoryUtils.SqrtModExtensionField()`.

---

## 7. Complete Algorithm

```
FindIndependentTorsionPoint(curve, r, maxAttempts=100):
    q = base field characteristic
    k = extension degree
    N_k = curve.GroupOrder                  // #E(F_{q^k})

    assert r divides N_k

    cofactor = N_k
    while cofactor mod r = 0:
        cofactor /= r                       // strip all r-factors

    for attempt = 1 to maxAttempts:
        T = GenerateRandomPoint(curve)      // random point in E(F_{q^k})
        S = cofactor * T                    // project to r-primary part

        if S = O: continue

        piS = Frobenius(S, q)               // (S.x^q, S.y^q)
        Q = piS - S                         // project to non-rational eigenspace

        if Q = O: continue

        // Reduce to exact E[r]
        rQ = r * Q
        while rQ != O:
            Q = rQ
            rQ = r * Q

        if Q != O:
            return Q

    raise "Failed after maxAttempts attempts"
```

**Implementation**: `TorsionPointFinder.FindIndependentTorsionPoint()` in `ElipticCurve/Implementations/TorsionPointFinder.cs`

---

## 8. Success Probability

Each random point T succeeds with probability approximately 1 - 1/r (since only 1/r of r-primary points project to zero under pi(S) - S, and only 1/r of those reduce to O). For cryptographic r (large prime), the probability of failure per attempt is negligible, so 100 attempts provide an astronomically high success rate.

---

## Summary

Finding an independent torsion point Q involves four stages:
1. **Cofactor stripping**: compute cofactor = N_k / r^e so that gcd(cofactor, r) = 1
2. **r-primary projection**: S = cofactor * T pushes a random T into the r-primary subgroup
3. **Frobenius projection**: Q = pi(S) - S kills the rational eigenspace and isolates the non-rational eigenspace
4. **E[r] reduction**: repeated multiplication by r strips any higher r-power torsion

The result Q has order r and spans the second factor of E[r] = Z/r x Z/r, enabling a non-degenerate pairing with the base-field generator P.
