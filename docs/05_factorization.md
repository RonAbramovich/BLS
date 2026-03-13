# Factorization Algorithms

This document describes the factorization methods used in the BLS signature implementation for computing group orders and point orders.

---

## 1. Overview

Factorization is used in several places:
1. **Group order factorization**: To find the largest prime divisor R
2. **Point order computation**: To efficiently compute the order of a specific point
3. **Embedding degree search**: To find prime divisors of candidate degrees

---

## 2. Integer Factorization (Trial Division)

### 2.1 Purpose

Factor a positive integer n into prime factors: n = p1^e1 x p2^e2 x ... x p?^e?

### 2.2 Algorithm

```
Factorize(n):
    factors = []
    if n = 1: return factors
    
    m = n
    
    // Remove all factors of 2
    count = 0
    while m mod 2 = 0:
        count++
        m = m / 2
    if count > 0:
        factors.add((prime: 2, power: count))
    
    // Try odd divisors from 3 to vm
    f = 3
    while f x f = m:
        count = 0
        while m mod f = 0:
            count++
            m = m / f
        if count > 0:
            factors.add((prime: f, power: count))
        f = f + 2  // only odd numbers
    
    // If m > 1, it's a prime factor
    if m > 1:
        factors.add((prime: m, power: 1))
    
    return factors
```

**Implementation**: `NumberTheoryUtils.Factorize(BigInteger n)`

### 2.3 Example

Factoring 360:
- m = 360
- Remove 2s: 360/2 = 180, 180/2 = 90, 90/2 = 45 to (2, 3), m = 45
- Try f=3: 45/3 = 15, 15/3 = 5 to (3, 2), m = 5
- Try f=5: f^2 = 25 > 5, stop loop
- m = 5 > 1 to (5, 1)
- Result: 360 = 2^3 x 3^2 x 5^1

### 2.4 Time Complexity

- **Worst case**: O(vn) when n is prime
- **Average case**: Much faster for composite numbers
- **Suitable for**: Small to medium integers (up to ~10^1^5 for reasonable performance)

---

## 3. Prime Divisor Extraction

### 3.1 Purpose

Get a list of distinct prime divisors (without powers) from an integer.

### 3.2 Algorithm

```
GetPrimeDivisors(n):
    primes = []
    t = n
    
    // Check divisors from 2 to vt
    for p = 2 to sqrt(t) (incrementing p):
        if t mod p = 0:
            primes.add(p)
            // Remove all instances of this prime
            while t mod p = 0:
                t = t / p
    
    // If t > 1, it's a remaining prime divisor
    if t > 1:
        primes.add(t)
    
    return primes
```

**Implementation**: `NumberTheoryUtils.GetPrimeDivisors(int n)`

### 3.3 Difference from Full Factorization

- Only returns distinct primes (no exponents)
- Optimized for when you only need the list of prime factors
- Used in Rabin's irreducibility test

### 3.4 Example

GetPrimeDivisors(60):
- t = 60
- p=2: 60 mod 2 = 0 to add 2, reduce: t = 60/4 = 15
- p=3: 15 mod 3 = 0 to add 3, reduce: t = 15/9... wait, let me recalculate
- p=2: add 2, t = 60 to 30 to 15
- p=3: add 3, t = 15 to 5
- p=5: reaches end of loop (5x5 > 5)
- t=5 > 1 to add 5
- Result: [2, 3, 5]

---

## 4. Largest Prime Divisor (R)

### 4.1 Purpose

Find the largest prime factor of the group order for cryptographic security.

### 4.2 Algorithm

```
FindLargestPrimeDivisor(n):
    factors = Factorize(n)
    R = max{p : (p, e) in factors}
    return R
```

**Implementation**: `EllipticCurve<T>.R` property

### 4.3 Why This Matters

In BLS signatures:
- We need a subgroup of **prime** order for security
- The largest prime divisor R gives us the largest prime-order subgroup
- Working in this subgroup prevents certain attacks

### 4.4 Example

Group order = 420 = 2^2 x 3 x 5 x 7
- Prime factors: 2, 3, 5, 7
- R = max{2, 3, 5, 7} = 7

---

## 5. Using Factorization for Point Order

### 5.1 Background

Given:
- Point P on elliptic curve
- Group order N = |E(F_p)|
- Prime factorization of N

Goal: Find the smallest k such that kP = O

### 5.2 Key Theorem

By Lagrange's theorem, the order of any element divides the group order. So:
```
order(P) | N
```

This means order(P) can be written as a product of prime powers that divide N.

### 5.3 Algorithm

**How It Works**:
- Start with ord = N (maximum possible order)
- For each prime p dividing N, try removing factors of p
- If (ord/p) x P = O, then we can reduce ord by p
- Continue until no more reduction possible

### 5.4 Example

Group order N = 12 = 2^2 x 3
Point P with actual order 6

Prime factorization: [(2, 2), (3, 1)]

**Start**: ord = 12

**Test prime 2**:
- Iteration 1: candidate = 12/2 = 6
  - Check: 6P = O? to Yes (assuming point has order 6)
  - Update: ord = 6
- Iteration 2: candidate = 6/2 = 3
  - Check: 3P = O? to No (point has order 6, so 3P != O)
  - Break

**Test prime 3**:
- Iteration 1: candidate = 6/3 = 2
  - Check: 2P = O? to No
  - Break

**Result**: order(P) = 6

### 5.5 Why This Is Efficient

- Only requires O(log N) scalar multiplications (one per prime power)
- Much faster than testing k = 1, 2, 3, ... until kP = O
- Uses the structure of the group order

**Implementation**: `ECPoint<T>.Order` property

---


