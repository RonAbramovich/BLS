# BLS Project

This repository contains implementations for BLS Algorithm over finite fields and elliptic curves. Below is a short summary of the main algorithms used so far and where they are implemented.

## Algorithms

- Prime field arithmetic (class: `PrimeFieldElement`)
  - Addition/Subtraction/Multiplication: modular arithmetic with normalization into the canonical representative in [0, p-1].
  - Multiplicative inverse: extended Euclidean algorithm to compute the inverse modulo the prime characteristic.
  - Exponentiation (`Power(long)`): exponentiation by squaring (binary exponentiation). Supports negative exponents by using multiplicative inverse.

- Elliptic curve point arithmetic (class: `ECPoint<T>`)
  - Point representation: affine coordinates (X, Y) and a special projective value for the point at infinity.
  - Negation: `-P = (X, -Y)` (uses field additive inverse).
  - Addition of distinct points: slope = (y2 - y1) / (x2 - x1); x3 = slope^2 - x1 - x2; y3 = slope*(x1 - x3) - y1.
  - Doubling: slope = (3*x^2 + A) / (2*y); then same formulas for x3 and y3.
  - Scalar multiplication: double-and-add (binary left-to-right) using repeated doubling and conditional addition based on the bits of the scalar.

- Group order enumeration (class: `EllipticCurve<T>`)
  - Direct counting for curves over prime fields: for each x in F_p evaluate z = x^3 + A*x + B. Use Euler's criterion to determine whether z is a quadratic residue:
    - z == 0 -> exactly one y (y = 0)
    - z is quadratic residue -> two y values
    - z is non-residue -> zero y values
  - Euler's criterion used: z^{(p-1)/2} equals 1 for residues, -1 for non-residues. 
  - The algorithm counts points and adds 1 for the point at infinity.
  - This is a direct counting method suitable for prime fields; This is what we need for the BLS Algorithm.

- Factorization of group order (helper in `EllipticCurve<T>` and `ECPoint<T>`)
  - Trial-division factorization: repeatedly factor out 2, then test odd integers up to sqrt(n). Produces a list of `(prime, exponent)` pairs.

- Point order computation (class: `ECPoint<T>`)
    - Let N be the group order and let its prime factorization be N = ∏ p^e.
    - Initialize ord := N.
    - For each prime p with multiplicity e, repeatedly test if (ord / p) * P == Infinity. If yes, set ord := ord / p and repeat; otherwise stop for that prime.
    - After processing all prime powers, ord is the exact order of P.
