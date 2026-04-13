# BLS Digital Signature Implementation
Ron Abramovich & Eyal Chai Ezra

**Repository:** [github.com/RonAbramovich/BLS](https://github.com/RonAbramovich/BLS)

This project implements a **complete BLS cryptographic signature scheme** with full pairing-based cryptography support.
BLS signatures are a form of digital signature that supports signature aggregation, making them useful for blockchain applications, distributed systems, and zero-knowledge proofs.

**Tech Stack:** C# / .NET 10 | xUnit | ASP.NET Core Web API

**Status: Complete implementation with verified bilinear pairings**

---

## Overview

The BLS signature scheme is built on elliptic curve cryptography and **bilinear pairings**. This implementation provides:

- Complete Tate pairing with Miller's algorithm and final exponentiation
- Verified bilinearity properties: e(aP, Q) = e(P, Q)^a and e(P, bQ) = e(P, Q)^b
- Cryptographic signature generation and verification
- Elliptic curves over finite fields and extension fields
- Point of prescribed order finding (torsion points)
- All mathematical building blocks for pairing-based cryptography

### What You Can Build With This

- BLS Signatures (signature aggregation for blockchains)
- Identity-Based Encryption (IBE)
- Zero-Knowledge Proofs (zkSNARKs)
- Verifiable Random Functions (VRFs)
- Attribute-Based Encryption (ABE)

---

## Project Structure

### Interfaces

The project defines clean abstractions for algebraic structures:

- **IField**: Represents a mathematical field with characteristic and element creation
- **IFieldElement**: Represents an element in a field with arithmetic operations
- **IEllipticCurve**: Represents an elliptic curve over a given field
- **IECPoint**: Represents a point on an elliptic curve with point arithmetic

### Implementations

#### Field Implementations

**PrimeField & PrimeFieldElement**

- Finite fields F_p for prime p
- Arithmetic: +, -, x, / (using Extended Euclidean Algorithm)
- Binary exponentiation for efficiency
- Modular normalization to [0, p-1]

**ExtensionField & ExtensionFieldElement**

- Extension fields F_p^k (polynomials modulo irreducible polynomial)
- Elements as polynomials in quotient ring F_p[x]/(g(x))
- Full polynomial arithmetic with modular reduction
- Critical for pairing computations

#### Elliptic Curve Implementations

**EllipticCurve\<T\>**

- Curves E: y^2 = x^3 + Ax + B over any field
- Non-singularity validation: 4A^3 + 27B^2 != 0
- Group order computation:
  - Base field: Direct counting with Euler's criterion
  - Extension field: Frobenius trace recurrence
- Factorization to find largest prime divisor r

**ECPoint\<T\>**

- Affine coordinates (x, y) + point at infinity
- Point addition and doubling with slope formulas
- Scalar multiplication via double-and-add
- Order computation by testing divisors

**TorsionPointFinder**

- Finds Q in E[r] linearly independent from base field
- Uses Frobenius endomorphism pi and cofactor multiplication
- Algorithm: Q = pi(S) - S where S = (N_k/r^2) * T
- Essential for pairing-based cryptography

#### Pairing Implementations

**LineFunctionUtils**

- Evaluates tangent lines: l_{T,T}(Q) for point doubling
- Evaluates chord lines: l_{T,S}(Q) for point addition
- Handles vertical lines and special cases
- Shares slope calculations with EllipticCurveUtils

**MillerAlgorithm**

- Core pairing computation: f_{r,P}(Q)
- Double-and-add loop processing binary expansion of r
- Line function accumulation
- Foundation for Tate pairing

**TatePairing**

- Complete reduced Tate pairing: e(P, Q) = f_{r,P}(Q)^((q^k-1)/r)
- Final exponentiation for bilinearity
- Verified properties:
  - Bilinearity: e(aP, Q) = e(P, Q)^a
  - Bilinearity: e(P, bQ) = e(P, Q)^b
  - Non-degeneracy: e(P, Q) != 0
  - Subgroup: e(P, Q)^r = 1

#### Supporting Utilities

**Polynomial & PolynomialUtils**

- Full polynomial arithmetic (+, -, x, / with remainder)
- Euclidean algorithm for GCD
- Evaluation and degree management

**IrreduciblePolynomialFinder**

- Finds irreducible polynomials of degree k over F_p
- Rabin's irreducibility test
- Ensures embedding degree k: r | (p^k - 1)

**NumberTheoryUtils**

- Modular arithmetic (inverse, division)
- Prime factorization
- Binary bit extraction
- Square root modulo p

**EllipticCurveUtils**

- Centralized slope calculations (tangent & chord)
- Shared by point operations and Miller's algorithm
- Eliminates code duplication

**HashToPoint**

- Deterministic message to curve point mapping
- Increment-and-try method
- Cofactor clearing for r-torsion subgroup

---

## BLS Signature Algorithm

**Input:** Message m, curve E(F_p), private key a, public key P = aG

### Phase 1: Setup

1. Define elliptic curve E: y^2 = x^3 + Ax + B over F_p
2. Compute group order |E(F_p)| and largest prime divisor r
3. Find embedding degree k such that r | (p^k - 1)
4. Find irreducible polynomial g(x) of degree k over F_p
5. Construct extension field F_p^k and curve E(F_p^k)

### Phase 2: Signature Generation

1. Hash message: H(m) in E(F_p)
2. Compute signature: sigma = a * H(m)

### Phase 3: Signature Verification

1. Parse: public key P_pub = aG, signature sigma
2. Compute pairings:
   - e1 = e(sigma, G) = e(aH(m), G)
   - e2 = e(H(m), P_pub) = e(H(m), aG)
3. Verify: e1 = e2 (bilinearity ensures equality)

### Tate Pairing Computation

```
e(P, Q) = MillerAlgorithm(P, Q, r) ^ ((p^k - 1) / r)
          [Miller function]         [Final exponentiation]
```

**Miller's Algorithm:**

1. Initialize f = 1, T = P
2. For each bit of r (MSB to LSB):
   - Double: f = f^2 * l_{T,T}(Q), T = 2T
   - Add (if bit=1): f = f * l_{T,P}(Q), T = T + P
3. Return f

**Final Exponentiation:**

- Raises f to power (p^k - 1)/r
- Ensures bilinearity and non-degeneracy
- Maps to correct target group GT

---

## Test Coverage

**50+ Comprehensive Tests:**

- Field arithmetic (prime & extension fields)
- Polynomial operations
- Elliptic curve operations
- Point order finding
- Hash-to-point mapping
- Torsion point finding
- Line function evaluations
- Miller's algorithm
- Complete Tate pairing with bilinearity verification

**Integration Tests:**

- Full pairing pipeline from field setup to bilinear verification
- Group order calculation validated by brute force (N_k = 1815)
- Multiple embedding degrees and field sizes

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

This approach embeds the multiplicative group of F_p^k into GL_k(F_p) (k x k matrices over F_p):

- Start with the root alpha = pi(x) where pi: F_p[x] to F_p[x]/(g(x))
- Build basis {1, alpha, alpha^2, ..., alpha^(k-1)} which generates the extension field as a vector space
- Find a generator g of the multiplicative group
- Represent each element using the generator basis {1, g, g^2, ..., g^(k-1)}
- Perform multiplication via matrix exponentiation

**Disadvantages (why this was rejected):**

1. **Generator Search Problem**: Finding a generator of the multiplicative group of order p^k - 1 is computationally expensive
2. **Addition/Subtraction Complexity**: Matrix addition/subtraction requires solving discrete logarithm problems - finding which power of g corresponds to the sum or difference of two elements is impractical

**Conclusion:** The polynomial representation (Option A) was chosen for its simplicity, direct correspondence to field theory, and practical implementation feasibility.

---

## Project Components Summary

- **Fields Package** (Fields/): Prime fields, extension fields, polynomial utilities, and embedding degree calculation
- **Elliptic Curve Package** (ElipticCurve/): Curve and point implementations with group operations
- **Pairing Package** (Pairing/): Miller's algorithm, line functions, and Tate pairing
- **Hash-to-Point** (ElipticCurve/Implementations/HashToPoint.cs): Deterministic message-to-point mapping
- **Web API** (WebAPI/): ASP.NET Core API with interactive frontend for BLS signature demos
- **Tests** (Tests/): Comprehensive xUnit test suite for all components

---

## Documentation

Detailed algorithm documentation is available in the docs/ directory:

1. **Field Arithmetic** (docs/01_field_arithmetic.md): Prime fields, polynomial arithmetic, extension fields, and the Extended Euclidean Algorithm
2. **Elliptic Curves** (docs/02_elliptic_curves.md): Point operations, group order computation, and scalar multiplication
3. **Irreducible Polynomials** (docs/03_irreducible_polynomials.md): Rabin's irreducibility test, embedding degree, and extension field construction
4. **Hash-to-Point** (docs/04_hash_to_point.md): Deterministic message-to-point mapping with increment-and-try and cofactor clearing
5. **Factorization** (docs/05_factorization.md): Trial division, prime factorization, and point order computation
6. **Test Suite** (docs/06_test_suite.md): Complete test coverage with detailed calculations and expected results
7. **Extension Field Group Order** (docs/06_extension_group_order.md): Computing group order via Frobenius trace recurrence
8. **Torsion Points** (docs/07_torsion_point.md): Finding an independent torsion point for pairing computation
9. **Tate Pairing** (docs/08_tate_pairing.md): Miller's algorithm, final exponentiation, and bilinear pairings
10. **Reference Calculations** (docs/elliptic_curve_calculations.md): Worked elliptic curve examples

Each document includes:

- Algorithm descriptions with pseudocode
- Step-by-step examples with complete calculations
- Implementation references to the codebase
- Time complexity analysis where relevant

---

## Web API & Interactive UI

The project includes an ASP.NET Core Web API (WebAPI/) with a browser-based frontend for interactive BLS signature demonstrations:

- **Single Signature** — Enter curve parameters (p, A, B), a private key, and a message to walk through the full BLS signing and verification pipeline step by step
- **Aggregated Signatures** — Multiple participants each sign the same message; the system aggregates signatures and verifies the result with a single pairing check
- **Bilingual UI** — Available in both Hebrew and English
- **Swagger** — API documentation at `/swagger` for programmatic access

---

## Getting Started

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
# Build the library
dotnet build

# Run the test suite
dotnet test

# Launch the Web API (serves UI at https://localhost:5001)
dotnet run --project WebAPI
```

---

## License

This project is provided for educational and research purposes.