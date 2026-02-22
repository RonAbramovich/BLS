# Elliptic Curve Calculations (Executable notes for tests)

This document contains the full modular arithmetic and elliptic curve calculations used by the unit tests in `Tests/EllipticCurveTests.cs`. 
It documents the PrimeField computations and the EC computations for the example curve over F5 (A = 0, B = 2).

---

Field: F5 (p = 5)

Curve 
```
y^2 = x^3 + A*x + B
```
With A = 0, B = 2 this becomes `y^2 = x^3 + 2` over F5.

### Squares in F5 (quadratic residues)
Compute squares modulo 5:
- 0^2 = 0
- 1^2 = 1
- 2^2 = 4
- 3^2 = 9 ≡ 4
- 4^2 = 16 ≡ 1

Quadratic residues set: `{0, 1, 4}`. Non-residues: `{2, 3}`.

### Enumerate x values and RHS = x^3 + 2 (mod 5)
- x = 0: RHS = 0 + 2 = 2 → non-residue → 0 y-solutions
- x = 1: RHS = 1 + 2 = 3 → non-residue → 0 y-solutions
- x = 2: RHS = 8 + 2 = 10 ≡ 0 → y^2 = 0 → one solution y = 0 → point `(2,0)`
- x = 3: RHS = 27 + 2 = 29 ≡ 4 → y^2 = 4 → two solutions `y = 2` and `y = 3` → points `(3,2)`, `(3,3)`
- x = 4: RHS = 64 + 2 = 66 ≡ 1 → y^2 = 1 → two solutions `y = 1` and `y = 4` → points `(4,1)`, `(4,4)`

Total rational points (including point at infinity):
- Points: O, (2,0), (3,2), (3,3), (4,1), (4,4)
- Count = 6 → `GroupOrder == 6` in tests.

Factorization: `6 = 2 * 3` → factors include `(2,1)` and `(3,1)` → largest prime divisor `R = 3`.

---

## 3) Elliptic-curve group arithmetic (explicit numeric steps)

Curve: p = 5, A = 0, B = 2. Points used in tests:
- `p1 = (2, 0)`
- `p2 = (3, 2)`
- `p2neg = (3, 3)` (this should equal `-p2`)
- `falsePoint = (2,3)` (not on curve)

### Negation
For P = (x, y) over Fp: `-P = (x, -y mod p)`.
- For `p2 = (3,2)`: `-p2 = (3, -2 mod 5) = (3, 3)` → matches `p2neg`.

### Doubling (when y != 0)
Formulas (short Weierstrass):
- λ = (3*x1^2 + A) / (2*y1)  (field division)
- x3 = λ^2 - 2*x1
- y3 = λ*(x1 - x3) - y1

Double `p2 = (3,2)`:
- x1 = 3, y1 = 2, A = 0
- x1^2 = 9 ≡ 4 (mod 5)
- 3*x1^2 = 3 * 4 = 12 ≡ 2
- numerator = 2 + 0 = 2
- denominator = 2*y1 = 4
- inverse of 4 mod 5 = 4 (since 4*4 = 16 ≡ 1)
- λ = 2 * 4 = 8 ≡ 3 (mod 5)
- λ^2 = 9 ≡ 4
- x3 = 4 - 3 - 3 = 4 - 6 = -2 ≡ 3 (mod 5)
- y3 = 3*(3 - 3) - 2 = 0 - 2 = -2 ≡ 3 (mod 5)
- So `2*p2 = (3,3)` → equals `p2neg`.

### Addition P + Q when x1 == x2
- If `x1 == x2` and `y1 == -y2` then P + Q = O (point at infinity).
- For `p2` and `2*p2` we have `p2 + 2*p2 = O` so `3*p2 = O` → order of `p2` divides 3. Since p2 != O, order = 3.

### Point with y = 0
- If `y = 0` then doubling returns O (vertical tangent), so order of `(2,0)` is 2.

### Multiply by negative scalar
- `Multiply(-1)` returns `Negate()`; for `p2` this returns `(3,3)`.

---

## 4) Group order algorithm rationale (used in code)

- The code enumerates all `x ∈ Fp` and computes `rhs = x^3 + A*x + B`.
- If `rhs == 0` → one solution `y = 0`.
- Otherwise use Euler's criterion: compute `rhs^{(p-1)/2}`.
  - If `rhs^{(p-1)/2} == 1` → quadratic residue → two `y` values.
  - If `rhs^{(p-1)/2} != 1` (equals `-1`) → non-residue → zero `y` values.
- Total points = sum of solutions for all x + 1 (point at infinity).

This is correct for curves over prime fields Fp (extension degree 1). The code throws for other extension degrees.

---

## 5) Factorization and largest prime divisor

- The code factors `n` by trial division: remove factors of 2, then check odd `i` up to `sqrt(n)`.
- The largest prime divisor `R` is taken as the maximum prime in the factor list.

---

## 6) Smoothness (non-singularity) check

- For short Weierstrass curves `y^2 = x^3 + A*x + B` the discriminant (up to a non-zero scalar) is `Δ = -16*(4*A^3 + 27*B^2)`.
- Over any field the curve is non-singular iff `4*A^3 + 27*B^2 != 0` in that field.
- The implementation validates `4*A^3 + 27*B^2 != 0` inside the constructor and throws an `ArgumentException` if the curve is singular.

---

If you want this file moved, renamed, or expanded into a PDF / richer doc with step-by-step modular arithmetic tables, I can add that as well and commit it to the repository.
