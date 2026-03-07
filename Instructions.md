# Final Project: BLS Signature Implementation (Reduced Tate Pairing)

## Objective
Implement the BLS cryptographic signature scheme using the Reduced Tate Pairing.
The implementation must be modular and object-oriented. Define separate classes for algebraic structures and their elements (with operator overloading where appropriate).

---

# System Inputs

- Prime number: p > 3, with p ≡ 3 (mod 4)
- Elliptic curve over F_p:
  E: y^2 = x^3 + A x + B
- Private key: 1 < a < r − 1

---

# Required Components

## 1. Prime Field F_p

Implement:
- Addition
- Subtraction
- Multiplication
- Division (using Extended Euclidean Algorithm)
- Efficient exponentiation

---

## 2. Elliptic Curve Validation

Verify the curve is non-singular:

4A^3 + 27B^2 ≠ 0 (mod p)

---

## 3. Elliptic Curve Group Operations

Implement:
- Point addition
- Point negation
- Scalar multiplication (double-and-add algorithm)
- Computation of group order |E(F_p)|
- Store group order in curve object
- Store point order in point object

Define:
r = largest prime divisor of |E|

---

## 4. Field Extension F_{p^k}

Find an irreducible polynomial:

g(x) ∈ F_p[x]

Of embedding degree k > 1 such that:
r | (p^k − 1)

Use Rabin's irreducibility test:

A polynomial g(x) of degree k is irreducible iff:

1) g(x) divides x^(p^k) − x
2) For every prime divisor d of k:
   gcd(g(x), x^(p^(k/d)) − x) = 1

Implement:
- Polynomial arithmetic
- Polynomial inverse (extended Euclidean algorithm)
- Field arithmetic in F_{p^k}

---

## 5. Miller Function (Reduced Tate Pairing)

Given:
- P ∈ E(F_p) of order r
- Q ∈ E(F_{p^k}) of order r

Implement Miller’s algorithm and compute:

e(P, Q) = f(P, Q)^((p^k − 1)/r)

Ensure:
- Q ≠ 0
- Q is not a pole

---

## 6. Hash-to-Point Function H(m)

Convert a string message to a point in subgroup of order r.

Step 1 — Convert String to Field Element

Treat message as byte array (Windows-1255 ASCII).
Interpret as base-256 integer:

x = ( Σ_{i=0}^{n−1} byte_i · 256^(n−1−i) ) mod p

---

Step 2 — Increment-and-Try Mapping

Loop:

1) Compute:
   z = x^3 + A x + B (mod p)

2) Check quadratic residue using Euler’s criterion:
   z^((p−1)/2) ≡ 1 (mod p)

3) If true (and p ≡ 3 (mod 4)):
   y = z^((p+1)/4) mod p
   Return point (x, y)

4) Otherwise:
   x ← x + 1
   Repeat

---

Step 3 — Clear Cofactor

Project to subgroup of order r:

H(m) = ( |E| / r ) · P_temp

---

## 7. BLS Signature Algorithm

Signature:
σ = a · H(m)

Verification:
e(σ, P) = e(H(m), aP)

Where:
aP = public key

---

## 8. Interactive Interface

Prompt user for:
- Prime p
- Curve coefficients A, B
- Private key a
- Message m

Output:
- Hash point H(m)
- Signature σ
- Pairing values
- Verification result

---

# Example Run

Input:
p = 103
A = 1
B = 0
a = 7
Message = "שלום"

Intermediate:
x ≡ 9 (mod 103)
P_temp = (9, 29)
H(m) = (32, 47)
σ = a · H(m) = (18, 44)

Pairing example:
Q = (8, 47 + 56i)

Verification:
e(σ, P) = e(H(m), aP)

---

# Implementation Requirements

- Use OOP design
- Separate classes for:
  - FiniteField
  - FieldElement
  - Polynomial
  - ExtensionField
  - EllipticCurve
  - ECPoint
- Efficient algorithms (double-and-add, fast exponentiation)
- Store computed orders where applicable