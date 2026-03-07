# Field Arithmetic Algorithms

This document describes the arithmetic operations implemented for prime fields and extension fields in the BLS signature project.

---

## 1. Prime Field Arithmetic (F_p)

A prime field F_p consists of integers modulo a prime p with standard arithmetic operations.

### 1.1 Modular Normalization

**Purpose**: Ensure all field elements are represented in their canonical form [0, p-1].

**Algorithm**:
```
ModNormalize(value, p):
    r = value mod p
    if r < 0:
        r = r + p
    return r
```

**Implementation**: `NumberTheoryUtils.ModNormalize(long value, int modulus)`

**Example**: 
- Input: value = -3, p = 7
- Output: 4 (since -3 ≡ 4 (mod 7))

---

### 1.2 Addition and Subtraction

**Addition**: `(a + b) mod p`
- Compute sum and normalize to [0, p-1]

**Subtraction**: `(a - b) mod p`
- Compute difference and normalize to [0, p-1]

**Additive Inverse**: `-a ≡ p - a (mod p)` for a ≠ 0

**Implementation**: `PrimeFieldElement.Add()`, `PrimeFieldElement.Sub()`, `PrimeFieldElement.AdditiveInverse()`

**Example** (F_7):
- 3 + 5 = 8 ≡ 1 (mod 7)
- 3 - 5 = -2 ≡ 5 (mod 7)
- Additive inverse of 3 is 4 (since 3 + 4 = 7 ≡ 0)

---

### 1.3 Multiplication

**Algorithm**: `(a × b) mod p`
- Compute product and normalize to [0, p-1]

**Implementation**: `PrimeFieldElement.Multiply()`

**Example** (F_7):
- 3 × 5 = 15 ≡ 1 (mod 7)

---

### 1.4 Multiplicative Inverse (Extended Euclidean Algorithm)

**Purpose**: Find the multiplicative inverse of a modulo p (i.e., find x such that a × x ≡ 1 (mod p)).

**Algorithm**: Extended Euclidean Algorithm
```
ExtendedGCD(a, p):
    // Initialize variables
    r_old = p, r_new = a
    t_old = 0, t_new = 1
    
    while r_new ≠ 0:
        quotient = r_old / r_new
        (r_old, r_new) = (r_new, r_old - quotient × r_new)
        (t_old, t_new) = (t_new, t_old - quotient × t_new)
    
    // At termination: r_old = gcd(a, p), t_old = inverse coefficient
    if r_old ≠ 1:
        throw "Inverse does not exist"
    
    return ModNormalize(t_old, p)
```

**Implementation**: `PrimeFieldElement.MultiplicativeInverse()`

**Example** (F_7):
Finding inverse of 3:
- Initial: r = (7, 3), t = (0, 1)
- Step 1: q = 2, r = (3, 1), t = (1, -2)
- Step 2: q = 3, r = (1, 0), t = (-2, 7)
- Result: gcd = 1, inverse = 7 ≡ 5 (mod 7)
- Verification: 3 × 5 = 15 ≡ 1 (mod 7) ✓

---

### 1.5 Exponentiation (Binary Exponentiation)

**Purpose**: Compute a^n (mod p) efficiently.

**Algorithm**: Square-and-Multiply (also called Exponentiation by Squaring)
```
Power(a, n, p):
    if n < 0:
        a = inverse(a)
        n = -n
    
    if n = 0:
        return 1
    
    result = 1
    base = a
    
    while n > 0:
        if n is odd:
            result = (result × base) mod p
        base = (base × base) mod p
        n = n / 2  // integer division
    
    return result
```

**Implementation**: `PrimeFieldElement.Power(long exponent)`

**Time Complexity**: O(log n) multiplications

**Example** (F_7):
Computing 3^5:
- Binary representation of 5: 101₂
- Steps:
  - n=5 (odd): result = 3, base = 9 ≡ 2, n = 2
  - n=2 (even): result = 3, base = 4, n = 1
  - n=1 (odd): result = 12 ≡ 5, base = 16 ≡ 2, n = 0
- Result: 3^5 ≡ 5 (mod 7)

**Negative Exponents**:
- For a^(-n), first compute inverse of a, then raise to power n
- Example: 3^(-1) = inverse(3)^1 = 5 in F_7

---

## 2. Polynomial Arithmetic

Polynomials are used as the building blocks for extension fields. All polynomial operations are performed with coefficients in F_p.

### 2.1 Basic Polynomial Operations

**Addition**: Component-wise addition of coefficients (mod p)
```
(a₀ + a₁x + a₂x^2) + (b₀ + b₁x + b₂x^2) = ((a₀+b₀) mod p) + ((a₁+b₁) mod p)x + ...
```

**Multiplication**: Standard polynomial multiplication followed by coefficient reduction (mod p)
```
(a₀ + a₁x)(b₀ + b₁x) = a₀b₀ + (a₀b₁ + a₁b₀)x + a₁b₁x^2
```

**Implementation**: `Polynomial.Add()`, `Polynomial.Mul()`

---

### 2.2 Polynomial Division with Remainder

**Purpose**: Divide polynomial f(x) by g(x) to get quotient q(x) and remainder r(x) such that:
```
f(x) = q(x) · g(x) + r(x)
```
where degree(r) < degree(g).

**Algorithm**: Long Division
```
Divide(f, g):
    if g is zero:
        throw error
    
    quotient = 0
    remainder = f
    
    while degree(remainder) >= degree(g):
        // Compute leading coefficient ratio
        coeff = leadingCoeff(remainder) / leadingCoeff(g)  // in F_p
        deg_diff = degree(remainder) - degree(g)
        
        // Create monomial term
        term = coeff · x^(deg_diff)
        quotient = quotient + term
        
        // Subtract term × g from remainder
        remainder = remainder - term × g
    
    return (quotient, remainder)
```

**Implementation**: `Polynomial.Div()`, `Polynomial.Mod()`

**Example** (F_5):
Divide f(x) = x^3 + 2x + 1 by g(x) = x^2 + 1:
- Step 1: Leading term x^3/x^2 = x, remainder = x^3 + 2x + 1 - x(x^2 + 1) = 2x + 1
- Step 2: Degree of remainder (1) < degree of g (2), stop
- Result: quotient = x, remainder = 2x + 1

---

### 2.3 Polynomial GCD (Euclidean Algorithm)

**Purpose**: Find the greatest common divisor of two polynomials.

**Algorithm**: Similar to integer GCD
```
GCD(a, b):
    while b ≠ 0:
        remainder = a mod b
        a = b
        b = remainder
    return a
```

**Implementation**: `Polynomial.Gcd()`

**Use Cases**:
- Testing irreducibility (Rabin's test)
- Polynomial inverse computation (Extended Euclidean Algorithm)

---

### 2.4 Polynomial Inverse Modulo (Extended Euclidean Algorithm)

**Purpose**: Find polynomial h(x) such that f(x) · h(x) ≡ 1 (mod g(x)).

**Algorithm**: Extended Euclidean Algorithm for Polynomials
```
ExtendedGCD(f, g):
    r_old = g, r_new = f
    t_old = 0, t_new = 1
    
    while r_new ≠ 0:
        (quotient, _) = Divide(r_old, r_new)
        (r_old, r_new) = (r_new, r_old - quotient × r_new)
        (t_old, t_new) = (t_new, t_old - quotient × t_new)
    
    if r_old is not constant 1:
        throw "Inverse does not exist"
    
    // Normalize so that r_old = 1
    scale = 1 / leadingCoeff(r_old)
    return t_old × scale
```

**Implementation**: `Polynomial.InverseMod()`

**Example** (F_2, modulus x^2 + x + 1):
Finding inverse of x:
- The computation yields inverse = x + 1
- Verification: x(x + 1) = x^2 + x ≡ 1 (mod x^2 + x + 1) in F_2 ✓

---

## 3. Extension Field Arithmetic (F_p^k)

Extension fields are constructed as F_p[x]/(g(x)) where g(x) is an irreducible polynomial of degree k.

### 3.1 Element Representation

Elements are represented as polynomials of degree < k with coefficients in F_p:
```
a = a₀ + a₁x + a₂x^2 + ... + a_(k-1)x^(k-1)
```

### 3.2 Addition and Subtraction

**Algorithm**: Component-wise operations on polynomial coefficients (mod p)

**Implementation**: `ExtensionFieldElement.Add()`, `ExtensionFieldElement.Sub()`

---

### 3.3 Multiplication

**Algorithm**:
1. Multiply polynomials normally
2. Reduce result modulo the irreducible polynomial g(x)

**Implementation**: `ExtensionFieldElement.Multiply()`

**Example** (F_3, modulus g(x) = x^2 + 1):
- Computing x · x:
  - Product: x^2
  - Reduction: x^2 ≡ -1 ≡ 2 (mod x^2 + 1) in F_3
  - Result: 2 (constant polynomial)

---

### 3.4 Multiplicative Inverse

**Algorithm**: Use Extended Euclidean Algorithm for polynomials to find h(x) such that:
```
f(x) · h(x) ≡ 1 (mod g(x))
```

**Implementation**: `ExtensionFieldElement.MultiplicativeInverse()`

**Example** (F_3, modulus x^2 + 1):
- Inverse of x is 2x
- Verification: x · (2x) = 2x^2 ≡ 2(-1) ≡ -2 ≡ 1 (mod 3) ✓

---

## Summary

This implementation provides complete field arithmetic for:
- **Prime Fields F_p**: Using modular arithmetic with Extended Euclidean Algorithm for inverses
- **Polynomial Rings F_p[x]**: With division, GCD, and inverse computations
- **Extension Fields F_p^k**: Represented as quotient rings F_p[x]/(g(x))

All algorithms follow standard number theory and abstract algebra techniques, optimized with binary exponentiation for efficient power computations.
