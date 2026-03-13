# Elliptic Curve Arithmetic

This document describes the elliptic curve operations implemented for the BLS signature scheme.

---

## 1. Elliptic Curve Definition

An elliptic curve over a field F is defined by the **Weierstrass equation**:
```
E: y^2 = x^3 + Ax + B
```
where A, B ? F and the curve is non-singular (smooth).

### 1.1 Non-Singularity Check

A curve is **non-singular** if it has no cusps or self-intersections. For Weierstrass form, this is equivalent to:
```
? = 4A^3 + 27B^2 != 0
```

**Implementation**: `EllipticCurve<T>.ValidateSmoothCurve()`

**Example**:
- Curve y^2 = x^3 + 2 over F_5 (A=0, B=2):
  - ? = 4(0)^3 + 27(2)^2 = 108 = 3 (mod 5) != 0 (pass)
  - Curve is non-singular

**Why This Matters**: Singular curves don't form groups, so arithmetic operations would be undefined.

---

## 2. Points on Elliptic Curves

A point on an elliptic curve is either:
1. **Affine point**: (x, y) satisfying the curve equation
2. **Point at infinity**: O (the identity element of the group)

**Implementation**: `ECPoint<T>`
- Affine points stored with coordinates `X`, `Y`
- Infinity represented by flag `IsInfinity = true`

---

## 3. Group Structure

The set of points on an elliptic curve E(F) forms an **abelian group** under a special addition operation:
- **Identity element**: Point at infinity O
- **Inverse**: For P = (x, y), the inverse is -P = (x, -y)
- **Addition**: Defined by geometric chord-and-tangent method

---

## 4. Point Arithmetic Operations

### 4.1 Point Negation

For a point P = (x, y):
```
-P = (x, -y)
```

where -y is computed in the underlying field.

**Special Cases**:
- -O = O (infinity is its own inverse)

**Implementation**: `ECPoint<T>.Negate()`

**Example** (F_5, curve y^2 = x^3 + 2):
- P = (3, 2)
- -P = (3, -2 mod 5) = (3, 3)
- Verification: 3^2 = 9 = 4 and 3^3 + 2 = 29 = 4 (pass)

---

### 4.2 Point Addition (Distinct Points)

**Case**: Adding two different points P1 = (x1, y1) and P2 = (x2, y2) where x1 != x2.

**Geometric Interpretation**: Draw a line through P1 and P2. This line intersects the curve at a third point. The sum P1 + P2 is the reflection of this third point across the x-axis.

**Algorithm**:
```
Add(P1, P2):
    if P1 = O: return P2
    if P2 = O: return P1
    if x1 = x2:
        if y1 = -y2: return O  // vertical line
        else: return Double(P1) // same point
    
    // Compute slope ?
    ? = (y2 - y1) / (x2 - x1)  // field division
    
    // Compute result coordinates
    x3 = ?^2 - x1 - x2
    y3 = ?(x1 - x3) - y1
    
    return (x3, y3)
```

**Implementation**: `ECPoint<T>.Add()`

**Example** (F_5, curve y^2 = x^3 + 2):
Adding P1 = (2, 0) and P2 = (3, 2):
- ? = (2 - 0) / (3 - 2) = 2 / 1 = 2
- x3 = 2^2 - 2 - 3 = 4 - 5 = -1 = 4 (mod 5)
- y3 = 2(2 - 4) - 0 = -4 = 1 (mod 5)
- Result: (4, 1)

---

### 4.3 Point Doubling

**Case**: Adding a point to itself: 2P = P + P.

**Geometric Interpretation**: Draw the tangent line at P. This line intersects the curve at another point. The result is the reflection of that point.

**Algorithm**:
```
Double(P):
    if P = O: return O
    if y = 0: return O  // vertical tangent
    
    // Compute slope of tangent line
    ? = (3x^2 + A) / (2y)  // field division
    
    // Compute result coordinates
    x3 = ?^2 - 2x
    y3 = ?(x - x3) - y
    
    return (x3, y3)
```

**Implementation**: `ECPoint<T>.Double()`

**Derivation of Slope**: The tangent slope is dy/dx from implicit differentiation of y^2 = x^3 + Ax + B:
- 2y dy/dx = 3x^2 + A
- dy/dx = (3x^2 + A) / (2y)

**Example** (F_5, curve y^2 = x^3 + 2, A=0):
Doubling P = (3, 2):
- x^2 = 9 = 4, so 3x^2 = 12 = 2 (mod 5)
- Numerator: 3x^2 + A = 2 + 0 = 2
- Denominator: 2y = 4
- Inverse of 4 in F_5 is 4 (since 4 x 4 = 16 = 1)
- ? = 2 x 4 = 8 = 3 (mod 5)
- x3 = 3^2 - 2(3) = 9 - 6 = 3
- y3 = 3(3 - 3) - 2 = -2 = 3 (mod 5)
- Result: 2P = (3, 3) = -P (pass)

**Special Case**: If y = 0, the tangent is vertical, so 2P = O. This means P has order 2.

---

### 4.4 Scalar Multiplication

**Purpose**: Compute nP = P + P + ... + P (n times) efficiently.

**Algorithm**: Double-and-Add (Binary Method)
```
Multiply(P, n):
    if n = 0: return O
    if n < 0: return Multiply(-P, -n)
    
    result = O
    addend = P
    
    while n > 0:
        if n is odd:
            result = result + addend
        addend = Double(addend)
        n = n / 2  // integer division
    
    return result
```

**Implementation**: `ECPoint<T>.Multiply(BigInteger scalar)`

**Time Complexity**: O(log n) point operations

**Example** (F_5, P = (3, 2)):
Computing 3P:
- Binary representation of 3: 112
- Steps:
  - n=3 (odd): result = (3,2), addend = 2P = (3,3), n = 1
  - n=1 (odd): result = (3,2) + (3,3) = O, n = 0
- Result: 3P = O (pass)

**Negative Scalars**:
- (-n)P = n(-P)
- Example: (-1)P = -P

---

## 5. Group Order Computation

**Group Order**: The number of points on the curve, denoted |E(F)|.

### 5.1 Direct Counting for Prime Fields (Extension Degree 1)

For curves over F_p, enumerate all x-coordinates and count solutions to y^2 = x^3 + Ax + B.

**Algorithm**:
```
ComputeGroupOrder(E over F_p):
    count = 1  // start with point at infinity
    
    for x = 0 to p-1:
        z = x^3 + Ax + B  (mod p)
        
        if z = 0:
            count += 1  // exactly one solution y = 0
        else:
            // Use Euler's criterion to check if z is a square
            legendre = z^((p-1)/2) mod p
            if legendre = 1:
                count += 2  // two distinct square roots
            // if legendre = -1, z is not a square, add 0
    
    return count
```

**Implementation**: `EllipticCurve<T>.GroupOrder` property

**Euler's Criterion**: For odd prime p and non-zero z ? F_p:
- z^((p-1)/2) = 1 (mod p) ? z is a quadratic residue (has square roots)
- z^((p-1)/2) = -1 (mod p) ? z is a non-residue (no square roots)

**Example** (F_5, y^2 = x^3 + 2):
- x=0: z=2, 2^2=4!=1 to non-residue to 0 points
- x=1: z=3, 3^2=4!=1 to non-residue to 0 points
- x=2: z=10=0 to one point (2, 0)
- x=3: z=29=4, 4^2=1 to residue to two points (3, 2) and (3, 3)
- x=4: z=66=1, 1^2=1 to residue to two points (4, 1) and (4, 4)
- Total: 1 + 0 + 0 + 1 + 2 + 2 = 6 points

**Limitation**: This method only works for prime fields. For extension fields (degree > 1), more sophisticated algorithms (like Schoof's algorithm) are needed.

---

## 6. Point Order Computation

**Point Order**: The smallest positive integer n such that nP = O.

### 6.1 Algorithm Using Group Order Factorization

**Key Theorem**: The order of any point divides the group order (by Lagrange's theorem).

**Algorithm**:
```
ComputePointOrder(P):
    if P = O: return 1
    
    N = group order
    factors = prime factorization of N
    ord = N
    
    // For each prime power p^e dividing N
    for each (p, e) in factors:
        // Try to reduce ord by dividing by p
        for i = 0 to e-1:
            candidate = ord / p
            if candidate x P = O:
                ord = candidate
            else:
                break  // cannot reduce further by this prime
    
    return ord
```

**Implementation**: `ECPoint<T>.Order` property

**Example** (F_5, curve with group order 6 = 2 x 3):
For P = (3, 2):
- Start: ord = 6
- Test factor 2: (6/2) x P = 3P = O (pass) to ord = 3
- Test factor 3: (3/3) x P = 1P != O ? to ord stays 3
- Result: Order of P is 3

For P = (2, 0):
- Start: ord = 6
- Test factor 2: (6/2) x P = 3P != O (actually it equals O since 2P = O) to ord = 3
- Wait, let me recalculate: 2P = O for this point
- Test factor 2: (6/2) x P = 3P = O to ord = 3
- Test factor 3: (3/3) x P = P != O to ord stays 3
- Actually, since 2P = O, we need: ord = 6, test 6/2=3: 3P=P+2P=P+O=P!=O, stays 6
- Test 6/3=2: 2P=O (pass) to ord = 2
- Result: Order of (2,0) is 2 (pass)

---

## 7. Largest Prime Divisor (R)

**Definition**: R is the largest prime factor of the group order.

**Purpose**: In BLS signatures, we work in the subgroup of prime order R for cryptographic security.

**Algorithm**:
```
ComputeR():
    factors = prime factorization of |E(F)|
    R = max{p : (p, e) in factors}
    return R
```

**Implementation**: `EllipticCurve<T>.R` property

**Example**:
- Group order = 6 = 2^1 x 3^1
- R = max{2, 3} = 3

---

## Summary

Elliptic curve arithmetic provides:
- **Group operations**: Addition, doubling, scalar multiplication
- **Group structure**: Computing group order and prime subgroups
- **Point properties**: Order computation for individual points

All operations are implemented generically over any field (prime or extension), though group order counting is currently optimized for prime fields only.

The implementation follows standard elliptic curve arithmetic as described in cryptography textbooks, with optimizations like double-and-add for efficient scalar multiplication.

