# Tate Pairing

This document describes the reduced Tate pairing e_r(P, Q) used in BLS signature verification, covering Miller's algorithm, line function evaluation, and final exponentiation.

---

## 1. What Is a Pairing?

A **bilinear pairing** is a map:
```
e: G1 x G2 -> GT
```

where:
- G1 = E(F_q)[r]  — points of order r on the base-field curve
- G2 = E(F_{q^k})[r]  — points of order r on the extension-field curve, linearly independent from G1
- GT = (F_{q^k})*[r]  — the r-th roots of unity in the multiplicative group of F_{q^k}

**Bilinearity** means:
```
e(aP, bQ) = e(P, Q)^{ab}   for any integers a, b
```

**Non-degeneracy** means:
```
e(P, Q) != 1   when P, Q != O
```

### 1.1 Role in BLS Signatures

In BLS signature verification, both Alice (signer) and Bob (verifier) independently compute Tate pairings:
- **Alice (Step 8)**: e(P, H(m)) — pairing the public key P with the hash point
- **Bob (Step 9)**: e(G, sigma) — pairing the base generator G with the signature

If the signature is valid, these two pairing values are equal, confirming that sigma = a * H(m) for private key a.

---

## 2. The Reduced Tate Pairing

The **reduced Tate pairing** is defined as:
```
e_r(P, Q) = f_{r,P}(Q)^{(q^k - 1)/r}
```

where f_{r,P} is a rational function on the curve (the **Miller function**) constructed so that:
- div(f_{r,P}) = r*(P) - r*(O)  (divisor)
- f_{r,P}(Q) is the value of this function evaluated at Q

The exponent (q^k - 1)/r maps the raw Miller output into the subgroup of r-th roots of unity in F_{q^k}*.

**Implementation**: `TatePairing.Compute()` in `Pairing/Implementations/TatePairing.cs`

---

## 3. Miller's Algorithm

### 3.1 Purpose

Compute f_{r,P}(Q) efficiently by exploiting the binary representation of r in a double-and-add loop, similar in structure to scalar multiplication.

### 3.2 Key Idea

The Miller function satisfies a multiplicative decomposition:
```
f_{a+b, P} = f_{a,P} * f_{b,P} * l_{aP, bP} / v_{(a+b)P}
```

where l_{A,B} is the line through A and B, and v_R is the vertical line at R. In practice the vertical line factors cancel after final exponentiation, so they are omitted from the accumulator.

### 3.3 Algorithm

```
ComputeMillerFunction(P, Q, r):
    f = 1  (in F_{q^k})
    T = P
    bits = binary representation of r (MSB first)

    for i = len(bits)-2 down to 0:     // skip MSB (always 1)
        // Doubling step
        f = f^2 * l_{T,T}(Q)           // tangent line at T, evaluated at Q
        T = 2T

        // Addition step (only when bit is 1)
        if bits[i] = 1:
            f = f * l_{T,P}(Q)         // chord line through T and P, evaluated at Q
            T = T + P

    return f
```

**Implementation**: `MillerAlgorithm.ComputeMillerFunction()` in `Pairing/Implementations/MillerAlgorithm.cs`

### 3.4 Example Trace

For r = 6 = 110_2 (bits [1, 1, 0] MSB first), the loop processes bits[1] and bits[0]:

```
i=1: double  -> f = f^2 * tangent(T=P, Q);  T = 2P
     bit=1   -> f = f * chord(T=2P, P, Q);  T = 3P
i=0: double  -> f = f^2 * tangent(T=3P, Q); T = 6P
     bit=0   -> (no addition step)
```

Result: f = f_{6,P}(Q)

---

## 4. Line Function Evaluation

### 4.1 The Tangent Line (Doubling Step)

The **tangent line** at a point T = (x_T, y_T) on E has slope:
```
lambda = (3 * x_T^2 + A) / (2 * y_T)   (mod q)
```

The tangent line equation is:
```
l(x, y) = y - y_T - lambda * (x - x_T) = 0
```

Evaluating at Q = (x_Q, y_Q) in F_{q^k} (with x_T, y_T, lambda lifted from F_q into F_{q^k}):
```
l_{T,T}(Q) = y_Q - y_T - lambda * (x_Q - x_T)
```

**Special case**: If y_T = 0, the tangent is vertical. The line evaluates to x_Q - x_T.

**Implementation**: `LineFunctionUtils.EvaluateTangentLine()` in `Pairing/Implementations/LineFunctionUtils.cs`

### 4.2 The Chord Line (Addition Step)

The **chord line** through T = (x_T, y_T) and P = (x_P, y_P) has slope:
```
lambda = (y_P - y_T) / (x_P - x_T)   (mod q)
```

Evaluating at Q:
```
l_{T,P}(Q) = y_Q - y_T - lambda * (x_Q - x_T)
```

**Special case**: If x_T = x_P (vertical chord), the line evaluates to x_Q - x_T.

**Implementation**: `LineFunctionUtils.EvaluateChordLine()` in `Pairing/Implementations/LineFunctionUtils.cs`

### 4.3 Lifting Base-Field Values to F_{q^k}

P and T live in E(F_q), so their coordinates are in F_q. Q lives in E(F_{q^k}). To multiply and add them together, the base-field scalars (x_T, y_T, lambda) are **lifted** into F_{q^k} as constant polynomials (degree-0 elements) using `extensionField.FromInt()`.

**Implementation**: `LineFunctionUtils.EvaluateLineFunction()` — shared inner method
```
EvaluateLineFunction(Q, slope, x_T_lifted, y_T_lifted):
    return Q.Y - y_T_lifted - slope * (Q.X - x_T_lifted)
```

---

## 5. Final Exponentiation

### 5.1 Purpose

The raw Miller output f_{r,P}(Q) lies in F_{q^k}* but is not yet in the r-th roots of unity subgroup. Raising to the exponent (q^k - 1)/r maps it into GT:
```
e_r(P, Q) = f_{r,P}(Q)^{(q^k - 1)/r}
```

This exponent is well-defined because r | (q^k - 1) by the definition of embedding degree k.

### 5.2 Algorithm

```
FinalExponentiation(f, extensionField, r):
    q = extensionField.BaseField.Characteristic
    k = extensionField.ExtensionDegree

    exponent = (q^k - 1) / r

    assert (q^k - 1) mod r = 0     // embedding degree property

    return f^exponent               // power in F_{q^k}*
```

**Implementation**: `TatePairing.FinalExponentiation()` in `Pairing/Implementations/TatePairing.cs`

### 5.3 Why It Produces an r-th Root of Unity

Let alpha = f^{(q^k-1)/r}. Then:
```
alpha^r = f^{q^k - 1} = 1   (by Fermat's little theorem in F_{q^k}*)
```

So alpha^r = 1, meaning alpha is an r-th root of unity. The non-degeneracy of the pairing guarantees alpha != 1 when P, Q are independent.

### 5.4 Example

For q = 5, k = 2, r = 3:
- q^k - 1 = 25 - 1 = 24
- exponent = 24 / 3 = 8
- e_r(P, Q) = f^8 in F_{25}*
- Verification: (f^8)^3 = f^{24} = 1 (pass)

---

## 6. Complete Tate Pairing Computation

```
TatePairing.Compute(P, Q, r, baseCurve, extensionField):
    // Step 1: Miller's algorithm
    f = ComputeMillerFunction(P, Q, r)

    // Step 2: Final exponentiation
    e = FinalExponentiation(f, extensionField, r)

    return e
```

**Inputs**:
- P in G1 = E(F_q)[r]  (base-field point, order r)
- Q in G2 = E(F_{q^k})[r]  (extension-field point, order r, independent from P)
- r: the prime order
- baseCurve: E over F_q (provides the A coefficient for tangent slopes)
- extensionField: F_{q^k} (provides One, FromInt, and field arithmetic)

**Output**: e(P, Q) in (F_{q^k}*)[r] — an r-th root of unity

---

## 7. BLS Verification via Pairings

### 7.1 Alice's Computation (Step 8)

Alice computes:
```
val_A = e(P, H(m))
```
where P is her public key and H(m) is the hash of the message.

### 7.2 Bob's Computation (Step 9)

Bob computes:
```
val_B = e(G, sigma)
```
where G is the agreed base generator and sigma = a * H(m) is Alice's signature.

### 7.3 Verification Equation

By bilinearity:
```
val_B = e(G, sigma) = e(G, a * H(m)) = e(G, H(m))^a = e(a * G, H(m)) = e(P, H(m)) = val_A
```

So val_A = val_B if and only if sigma is a valid signature under public key P = a * G.

---

## Summary

The reduced Tate pairing computation has two phases:

1. **Miller's algorithm**: a double-and-add loop over the bits of r, accumulating line function evaluations f = f^2 * tangent (doubling step) and f = f * chord (addition step). Each line is evaluated at the extension-field point Q after lifting base-field coordinates into F_{q^k}.

2. **Final exponentiation**: raise the Miller output to the power (q^k - 1)/r, mapping it from F_{q^k}* into the r-th roots of unity subgroup GT.

Together these produce a bilinear, non-degenerate pairing that enables the BLS equality check e(P, H(m)) = e(G, sigma).
