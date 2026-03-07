# Irreducible Polynomials and Extension Field Construction

This document explains how to find irreducible polynomials and construct extension fields for the BLS signature scheme.

---

## 1. Extension Fields Overview

### 1.1 What is an Extension Field?

An **extension field** F_p^k is a field containing F_p with exactly p^k elements. It can be constructed as:
```
F_p^k ≅ F_p[x] / (g(x))
```
where g(x) is an **irreducible polynomial** of degree k over F_p.

**Irreducible Polynomial**: A polynomial that cannot be factored into non-trivial polynomials over F_p (analogous to prime numbers for integers).

---

### 1.2 Why Extension Fields are Needed

In the BLS signature scheme:
1. We work with an elliptic curve E over F_p with prime subgroup order r
2. We need a **pairing** (bilinear map) that requires an extension field F_p^k
3. The extension degree k must satisfy: **r divides (p^k - 1)**

This k is called the **embedding degree** of r with respect to p.

---

## 2. Embedding Degree

### 2.1 Definition

The **embedding degree** k is the smallest positive integer such that:
```
r | (p^k - 1)
```

This ensures that the extension field F_p^k contains the r-th roots of unity, which are required for pairing computations.

### 2.2 Finding the Embedding Degree

**Algorithm**:
```
FindEmbeddingDegree(p, r, maxSearch):
    for k = 1 to maxSearch:
        if (p^k - 1) mod r = 0:
            return k
    
    throw "Embedding degree not found"
```

**Implementation**: `IrreduciblePolynomialFinder.FindEmbeddingDegree()`

**Example**:
- p = 2, r = 3
- Test k=1: 2^1 - 1 = 1, 1 mod 3 = 1 ✗
- Test k=2: 2^2 - 1 = 3, 3 mod 3 = 0 ✓
- Embedding degree k = 2

---

## 3. Rabin's Irreducibility Test

Rabin's test is an efficient algorithm to check if a polynomial g(x) of degree k is irreducible over F_p.

### 3.1 Theorem (Rabin's Criterion)

A polynomial g(x) ∈ F_p[x] of degree k is irreducible if and only if:

1. **Condition 1**: g(x) divides x^(p^k) - x
   
2. **Condition 2**: For every prime divisor q of k:
   ```
   gcd(g(x), x^(p^(k/q)) - x) = 1
   ```
---

### 3.3 Algorithm

```
IsIrreducible(g, p):
    k = degree of g
    if k ≤ 0: return false
    
    // Condition 1: Check if g divides x^(p^k) - x
    x_power = x^(p^k) mod g(x)
    if x_power ≠ x:
        return false
    
    // Condition 2: Check for each prime divisor of k
    primes = GetPrimeDivisors(k)
    for each prime q in primes:
        sub_degree = k / q
        x_power_sub = x^(p^sub_degree) mod g(x)
        common = gcd(g(x), x_power_sub - x)
        if common ≠ 1:
            return false
    
    return true
```

**Implementation**: `PolynomialUtils.IsIrreducible()`

---

### 3.4 Example

**Testing if x^2 + x + 1 is irreducible over F_2:**

Parameters: p = 2, k = 2, g(x) = x^2 + x + 1

**Step 1 - Condition 1**: Check if g(x) divides x^(2^2) - x = x^4 - x

Compute x^4 mod (x^2 + x + 1) over F_2:
- x^2 ≡ -x - 1 ≡ x + 1 (mod g(x)) in F_2
- x^4 = (x^2)^2 ≡ (x + 1)^2 = x^2 + 2x + 1 ≡ x^2 + 1 (in F_2, 2=0)
- x^4 ≡ (x + 1) + 1 = x (mod g(x))

So x^4 - x ≡ 0 (mod g(x)) ✓

**Step 2 - Condition 2**: Find prime divisors of k=2

Prime divisors of 2: {2}

For q = 2:
- sub_degree = 2/2 = 1
- Compute x^(2^1) - x = x^2 - x
- Compute gcd(g(x), x^2 - x):
  - x^2 - x = x(x - 1) = x(x + 1) in F_2
  - g(x) = x^2 + x + 1
  - gcd(x^2 + x + 1, x^2 + x) using Euclidean algorithm:
    - x^2 + x + 1 = 1 · (x^2 + x) + 1
    - gcd = 1 ✓

Both conditions satisfied → **x^2 + x + 1 is irreducible over F_2**

---

## 4. Finding an Irreducible Polynomial

### 4.1 Search Strategy

Given p, r, and desired degree k:
1. Find embedding degree k (smallest k where r | (p^k - 1))
2. Enumerate monic polynomials of degree k over F_p
3. Test each using Rabin's test until one is found

### 4.2 Algorithm

```
FindIrreduciblePolynomial(p, r, maxSearch):
    k = FindEmbeddingDegree(p, r, maxSearch)
    
    // Enumerate monic polynomials: coefficients c_0, c_1, ..., c_(k-1) and leading 1
    maxAttempts = p^k  // number of possible combinations
    
    for candidateValue = 0 to maxAttempts-1:
        // Convert candidateValue to base-p representation
        coeffs = [0] * (k+1)
        remainder = candidateValue
        for i = 0 to k-1:
            coeffs[i] = remainder mod p
            remainder = remainder / p
        coeffs[k] = 1  // monic
        
        poly = Polynomial(coeffs)
        
        if IsIrreducible(poly):
            return poly
    
    throw "No irreducible polynomial found"
```

**Implementation**: `IrreduciblePolynomialFinder.FindIrreduciblePolynomial()`

---

### 4.3 Example

**Find irreducible polynomial for p=2, r=3:**

Embedding degree k = 2 (as computed earlier)

Enumerate monic degree-2 polynomials over F_2:
- Candidates: x^2, x^2+1, x^2+x, x^2+x+1

Test x^2:
- Clearly reducible (missing lower terms)

Test x^2 + 1:
- x^2 + 1 = (x + 1)(x + 1) = (x + 1)^2 in F_2 (since (x+1)^2 = x^2+1 in characteristic 2)
- Reducible ✗

Test x^2 + x:
- x^2 + x = x(x + 1)
- Reducible ✗

Test x^2 + x + 1:
- Use Rabin's test (as shown in previous example)
- Irreducible ✓

**Result**: g(x) = x^2 + x + 1

---

## 5. Extension Field Construction

Once an irreducible polynomial g(x) is found:

### 5.1 Field Definition

The extension field is:
```
F_p^k = F_p[x] / (g(x))
```

Elements are polynomials of degree < k with coefficients in F_p.

### 5.2 Implementation

**Creation**: `ExtensionField(baseField, irreduciblePoly)`
- Validates that the polynomial is indeed irreducible
- Stores the polynomial for all reduction operations

**Element Representation**: `ExtensionFieldElement(field, polynomial)`
- Automatically reduces polynomial modulo g(x)
- Ensures canonical representation (degree < k)

---

### 5.3 Example

Constructing F_2^2:
```
baseField = PrimeField(2)
g = Polynomial([1, 1, 1])  // x^2 + x + 1
extensionField = ExtensionField(baseField, g)
```

Elements of F_2^2:
- 0 (zero polynomial)
- 1 (constant 1)
- x
- x + 1

These 4 elements form the complete field F_4.

**Multiplication example** in F_4:
- x · x = x^2 ≡ x + 1 (mod x^2 + x + 1)
- x · (x + 1) = x^2 + x ≡ (x + 1) + x = 1 (mod x^2 + x + 1)

---

## 6. Prime Factorization (Supporting Algorithm)

To apply Rabin's test, we need prime divisors of the degree k.

### 6.1 Algorithm

```
GetPrimeDivisors(n):
    primes = []
    temp = n
    
    // Check all numbers from 2 to sqrt(n)
    for p = 2 to sqrt(temp):
        if temp mod p = 0:
            primes.add(p)
            while temp mod p = 0:
                temp = temp / p
    
    // If temp > 1, it's a prime divisor
    if temp > 1:
        primes.add(temp)
    
    return primes
```

**Implementation**: `NumberTheoryUtils.GetPrimeDivisors()`

**Example**:
- n = 12
- Factors: 2, 2, 3
- Prime divisors: {2, 3}

---

## Summary

The process of constructing an extension field for BLS signatures involves:

1. **Find embedding degree k**: Smallest k where r | (p^k - 1)
2. **Find irreducible polynomial g(x)**: Use Rabin's test to verify irreducibility
3. **Construct extension field**: F_p^k = F_p[x]/(g(x))

**Rabin's Irreducibility Test** provides an efficient deterministic method to verify that a polynomial cannot be factored, which is essential for the security and correctness of the cryptographic construction.

This approach ensures that the extension field has the correct algebraic properties required for implementing the bilinear pairing in BLS signatures.
