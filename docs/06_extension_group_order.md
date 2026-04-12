# Extension Field Group Order via Frobenius Trace

This document describes how the implementation computes #E(F_{q^k}), the number of points on an elliptic curve over an extension field, using the Frobenius trace recurrence.

---

## 1. Background and Motivation

### 1.1 Why a Different Method for Extension Fields?

For a curve over a prime field F_q, counting points by enumerating all x-values is feasible (see `02_elliptic_curves.md`, Section 5). Over an extension field F_{q^k}, the field has q^k elements, making direct enumeration impractical for any non-trivial k.

Instead, the implementation uses a closed-form recurrence derived from the Frobenius endomorphism, which reduces the problem to a single prime-field computation followed by k-2 recurrence steps.

---

## 2. The Frobenius Trace

### 2.1 Definition

**Purpose**: Encode the "deviation" of #E(F_q) from the Hasse bound midpoint q+1 in a single integer.

For a curve E over a prime field F_q with #E(F_q) = N_1, the **Frobenius trace** is:
```
t = q + 1 - N_1
```

By Hasse's theorem, |t| <= 2*sqrt(q), so t is small relative to q.

**Implementation**: Computed inside `EllipticCurve<T>.ComputeGroupOrderForExtensionField()` after calling `ComputeGroupOrderForPrimeField()` on the base curve.

**Example** (F_5, curve y^2 = x^3 + 2, A=0, B=2):
- From direct counting: N_1 = #E(F_5) = 6
- t = 5 + 1 - 6 = 0

---

## 3. The Frobenius Trace Recurrence

### 3.1 Sequence Definition

Define a sequence a_n by the linear recurrence:
```
a_0 = 2
a_1 = t
a_n = t * a_{n-1} - q * a_{n-2}   for n >= 2
```

This sequence encodes the trace of the n-th power of the Frobenius endomorphism acting on the Tate module of E.

### 3.2 Algorithm

```
ComputeExtensionGroupOrder(curve over F_{q^k}):
    // Step 1: compute N_1 = #E(F_q) for the base prime-field curve
    N_1 = CountPointsDirectly(curve over F_q)

    // Step 2: compute Frobenius trace
    t = q + 1 - N_1

    // Step 3: run recurrence up to index k
    a_prev = 2      // a_0
    a_curr = t      // a_1

    for n = 2 to k:
        a_next = t * a_curr - q * a_prev
        a_prev = a_curr
        a_curr = a_next

    // a_curr is now a_k
    N_k = q^k + 1 - a_curr
    return N_k
```

**Implementation**: `EllipticCurve<T>.ComputeGroupOrderForExtensionField()` in `ElipticCurve/Implementations/EllipticCurve.cs` (lines 88-132)

### 3.3 Final Formula

```
N_k = #E(F_{q^k}) = q^k + 1 - a_k
```

This is the analogue of Hasse's formula N_1 = q + 1 - t, generalized to degree k.

---

## 4. Worked Example

**Setup**: Curve y^2 = x^3 + 2 over F_5, extension degree k = 2.

**Step 1 - Base group order**:
- Count over F_5: N_1 = 6 (from direct enumeration)

**Step 2 - Frobenius trace**:
- t = 5 + 1 - 6 = 0

**Step 3 - Recurrence for k = 2**:
- a_0 = 2
- a_1 = t = 0
- a_2 = t * a_1 - q * a_0 = 0 * 0 - 5 * 2 = -10

**Step 4 - Extension order**:
- N_2 = 5^2 + 1 - (-10) = 25 + 1 + 10 = 36

So #E(F_{5^2}) = 36.

**Verification check**: 36 = 6 * 6, and 6 = N_1. For curves with t = 0 (called supersingular curves), the formula gives N_k = q^k + 1 + 2*q^{k/2} for even k, which for k=2 yields q^2 + 2q + 1 = (q+1)^2 = 36 when q=5. (pass)

---

## 5. Why This Matters

The extension group order N_k is a prerequisite for:
- **Torsion point finding** (`07_torsion_point.md`): the cofactor is computed as N_k with all r-factors stripped.
- **BLS Step 6**: verifying that r | N_k, which is required for the r-torsion subgroup E[r] to exist over F_{q^k}.
- **Embedding degree**: k is the smallest integer with r | (q^k - 1); finding k such that r | N_k is how this is verified in practice (see `03_irreducible_polynomials.md`).

---

## Summary

Computing #E(F_{q^k}) reduces to three steps:
1. **One prime-field counting** to get N_1 via Euler's criterion
2. **One subtraction** to get the Frobenius trace t = q + 1 - N_1
3. **k-1 recurrence steps** a_n = t * a_{n-1} - q * a_{n-2}, then N_k = q^k + 1 - a_k

The method is efficient for small k (as used in BLS schemes with embedding degree k = 2 or small values), requiring O(k) arithmetic operations on integers of size ~k * log(q) bits.
