# BLS Digital Signature Implementation

This project implements the **BLS** cryptographic signature scheme using the Reduced Tate Pairing.
BLS signatures are a form of digital signature that supports signature aggregation, making them useful for blockchain applications and distributed systems.
It was used until 2020 in the Ethereum 2.0 specification for its efficient signature aggregation properties.

---

## Overview

The BLS signature scheme is built on elliptic curve cryptography and bilinear pairings. This implementation provides all the mathematical building blocks needed to:
- Generate cryptographic signatures for messages
- Verify signatures using public keys
- Work with elliptic curves over finite fields and their extensions

The project is designed with modularity and object-oriented principles, separating algebraic structures (fields, curves) from their operations.

---

## Project Structure

### Interfaces

The project defines clean abstractions for algebraic structures:

- **`IField`**: Represents a mathematical field with characteristic and element creation
- **`IFieldElement`**: Represents an element in a field with arithmetic operations (addition, subtraction, multiplication, division, exponentiation)
- **`IEllipticCurve`**: Represents an elliptic curve with methods to create points and check validity - over a given field
- **`IECPoint`**: Represents a point on an elliptic curve with point arithmetic operations

### Implementations

#### Field Implementations

1. **`PrimeField` & `PrimeFieldElement`**
   - Implements finite fields F_p for prime p
   - Core arithmetic: addition, subtraction, multiplication, division (using Extended Euclidean Algorithm)
   - Efficient exponentiation using binary exponentiation (square-and-multiply)
   - Modular normalization to canonical representatives in [0, p-1]

2. **`ExtensionField` & `ExtensionFieldElement`**
   - Implements extension fields F_p^k (polynomials modulo an irreducible polynomial)
   - Elements represented as polynomials (coefficient vectors) in the quotient ring F_p[x]/(g(x))
   - Arithmetic operations use polynomial arithmetic modulo the irreducible polynomial
   - See "Extension Field Design Decision" section below for implementation rationale

#### Elliptic Curve Implementations

1. **`EllipticCurve<T>`**
   - Implements elliptic curves E: y² = x³ + Ax + B over any field
   - Validates curve is non-singular: 4A³ + 27B² ≠ 0
   - Computes group order |E(F_p)| using direct counting with Euler's criterion
   - Factorizes group order to find the largest prime divisor r

2. **`ECPoint<T>`**
   - Implements elliptic curve points with affine coordinates (x, y) and point at infinity
   - Point addition using slope formulas
   - Point doubling with efficient tangent calculation
   - Scalar multiplication via double-and-add algorithm
   - Computes point order by testing divisors of the group order

#### Supporting Utilities

1. **`Polynomial` & `PolynomialUtils`**
   - Polynomial arithmetic (addition, multiplication, division with remainder)
   - GCD computation using Euclidean algorithm
   - Evaluation and degree management

2. **`IrreduciblePolynomialFinder`**
   - Finds irreducible polynomials of degree k over F_p
   - Uses Rabin's irreducibility test:
     - g(x) divides x^(p^k) - x
     - For every prime divisor d of k: gcd(g(x), x^(p^(k/d)) - x) = 1
   - Ensures embedding degree k satisfies: r divides (p^k - 1)

3. **`NumberTheoryUtils`**
   - Modular normalization
   - Prime factorization

4. **`HashToPoint`**
   - Converts string messages to elliptic curve points
   - Three-step process:
     1. Convert message to field element (base-256 interpretation)
     2. Increment-and-try mapping to find valid curve point
     3. Clear cofactor to project into subgroup of order r

---

## BLS Signature Algorithm Steps

The BLS signature scheme consists of the following steps:
Input : [message m, Eliptic curve prameter A,B \in F_p, private key a, public key P = aG]

### 1. **Setup**
   - Define elliptic curve E: y² = x³ + Ax + B over F_p
   - Compute group order |E(F_p)| and find largest prime divisor r
   - Find embedding degree k and irreducible polynomial g(x) of degree k over F_p
   - Construct extension field F_p^k

### 2. **Signature Generation**
   - Hash message m to a curve point: H(m) \in E(F_p)
   - Compute signature: σ = a · H(m)

### 3. **Signature Verification**
   - Compute pairing: e(σ, P) = e(H(m), aP)
   - The signature is valid if the pairing equality holds
   - Uses Miller's algorithm to compute the Reduced Tate Pairing:
     - e(P, Q) = f(P, Q)^((p^k - 1)/r)

---

## Extension Field Design Decision

When implementing the extension field F_p[x]/(g(x)) where g(x) is irreducible over F_p of degree k, there were two possible approaches:

### Option A: Polynomial Representation (Chosen)
Elements are represented as polynomials (coefficient vectors) in the quotient ring F_p[x]/(g(x)). Operations include:
- Addition and subtraction: component-wise modulo p
- Multiplication: polynomial multiplication followed by reduction modulo g(x)
- Division: compute multiplicative inverse using the Extended Euclidean Algorithm on polynomials

**Advantages:**
- Direct and mathematically natural representation
- Standard algorithms for all operations
- Straightforward implementation of field arithmetic

**Disadvantages:**
- Requires careful implementation of polynomial arithmetic and reduction modulo the irreducible polynomial

### Option B: Matrix Representation (Not Chosen)
This approach embeds the multiplicative group of F_p^k into GL_k(F_p) (k×k matrices over F_p):
- Start with the root α = π(x) where π: F_p[x] → F_p[x]/(g(x))
- Build basis {1, α, α², ..., α^(k-1)} which generates the extension field as a vector space
- Find a generator g of the multiplicative group
- Represent each element using the generator basis {1, g, g², ..., g^(k-1)}
- Perform multiplication via matrix exponentiation

**Disadvantages (why this was rejected):**
1. **Generator Search Problem**: Finding a generator of the multiplicative group of order p^k - 1 is computationally expensive
2. **Addition/Subtraction Complexity**: Matrix addition/subtraction requires solving discrete logarithm problems - finding which power of g corresponds to the sum or difference of two elements is impractical

**Conclusion:** The polynomial representation (Option A) was chosen for its simplicity, direct correspondence to field theory, and practical implementation feasibility.

---

## Project Components Summary

- **Fields Package** (`Fields/`): Prime fields, extension fields, and polynomial utilities
- **Elliptic Curve Package** (`ElipticCurve/`): Curve and point implementations with group operations
- **Hash-to-Point**: Deterministic message-to-point mapping
- **Tests**: Comprehensive unit tests for all components

For detailed algorithm explanations, see the `docs/` directory.

---

## Next Steps

The following components are planned or in development:
- Miller's algorithm for pairing computation
- Full BLS signature and verification workflow
- Interactive command-line interface for signing and verifying messages
- Performance optimizations for large-scale operations
