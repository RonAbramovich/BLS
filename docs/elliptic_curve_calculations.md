# Elliptic Curve Calculations - Reference Example

> **Note**: This document is a legacy reference file. For comprehensive algorithm documentation, see:
> - [Field Arithmetic](01_field_arithmetic.md)
> - [Elliptic Curves](02_elliptic_curves.md)
> - [Test Suite](06_test_suite.md)

This document contains the detailed step-by-step calculations for the primary test case used in `Tests/EllipticCurveTests.cs`.

---

## Test Curve: y^2 = x^3 + 2 over F_5

**Field**: F_5 (p = 5)  
**Curve Parameters**: A = 0, B = 2

The Weierstrass equation becomes:
```
y^2 = x^3 + 2
```

### Squares in F5 (quadratic residues)
Compute squares modulo 5:
- 0^2 = 0
- 1^2 = 1
- 2^2 = 4
- 3^2 = 9 = 4
- 4^2 = 16 = 1

Quadratic residues set: `{0, 1, 4}`. Non-residues: `{2, 3}`.

### Enumerate x values and RHS = x^3 + 2 (mod 5)
- x = 0: RHS = 0 + 2 = 2 to non-residue to 0 y-solutions
- x = 1: RHS = 1 + 2 = 3 to non-residue to 0 y-solutions
- x = 2: RHS = 8 + 2 = 10 = 0 to y^2 = 0 to one solution y = 0 to point `(2,0)`
- x = 3: RHS = 27 + 2 = 29 = 4 to y^2 = 4 to two solutions `y = 2` and `y = 3` to points `(3,2)`, `(3,3)`
- x = 4: RHS = 64 + 2 = 66 = 1 to y^2 = 1 to two solutions `y = 1` and `y = 4` to points `(4,1)`, `(4,4)`

Total rational points (including point at infinity):
- Points: O, (2,0), (3,2), (3,3), (4,1), (4,4)
- Count = 6 to `GroupOrder == 6` in tests.

Factorization: `6 = 2 * 3` to factors include `(2,1)` and `(3,1)` to largest prime divisor `R = 3`.

---

## 3) Elliptic-curve group arithmetic (explicit numeric steps)

Curve: p = 5, A = 0, B = 2. Points used in tests:
- `p1 = (2, 0)`
- `p2 = (3, 2)`
- `p2neg = (3, 3)` (this should equal `-p2`)
- `falsePoint = (2,3)` (not on curve)

### Negation
For P = (x, y) over Fp: `-P = (x, -y mod p)`.
- For `p2 = (3,2)`: `-p2 = (3, -2 mod 5) = (3, 3)` to matches `p2neg`.

### Doubling (when y != 0)
Formulas (short Weierstrass):
- ? = (3*x1^2 + A) / (2*y1)  (field division)
- x3 = ?^2 - 2*x1
- y3 = ?*(x1 - x3) - y1

Double `p2 = (3,2)`:
- x1 = 3, y1 = 2, A = 0
- x1^2 = 9 = 4 (mod 5)
- 3*x1^2 = 3 * 4 = 12 = 2
- numerator = 2 + 0 = 2
- denominator = 2*y1 = 4
- inverse of 4 mod 5 = 4 (since 4*4 = 16 = 1)
- ? = 2 * 4 = 8 = 3 (mod 5)
- ?^2 = 9 = 4
- x3 = 4 - 3 - 3 = 4 - 6 = -2 = 3 (mod 5)
- y3 = 3*(3 - 3) - 2 = 0 - 2 = -2 = 3 (mod 5)
- So `2*p2 = (3,3)` to equals `p2neg`.

### Addition P + Q when x1 == x2
- If `x1 == x2` and `y1 == -y2` then P + Q = O (point at infinity).
- For `p2` and `2*p2` we have `p2 + 2*p2 = O` so `3*p2 = O` to order of `p2` divides 3. Since p2 != O, order = 3.

### Point with y = 0
- If `y = 0` then doubling returns O (vertical tangent), so order of `(2,0)` is 2.

### Multiply by negative scalar
- `Multiply(-1)` returns `Negate()`; for `p2` this returns `(3,3)`.

---

## 4) Group order algorithm rationale (used in code)

- The code enumerates all `x ? Fp` and computes `rhs = x^3 + A*x + B`.
- If `rhs == 0` to one solution `y = 0`.
- Otherwise use Euler's criterion: compute `rhs^{(p-1)/2}`.
  - If `rhs^{(p-1)/2} == 1` to quadratic residue to two `y` values.
  - If `rhs^{(p-1)/2} != 1` (equals `-1`) to non-residue to zero `y` values.
- Total points = sum of solutions for all x + 1 (point at infinity).

This is correct for curves over prime fields Fp (extension degree 1). The code throws for other extension degrees.

---

## 5) Factorization and largest prime divisor

- The code factors `n` by trial division: remove factors of 2, then check odd `i` up to `sqrt(n)`.
- The largest prime divisor `R` is taken as the maximum prime in the factor list.

---

## 6) Smoothness (non-singularity) check

- For short Weierstrass curves `y^2 = x^3 + A*x + B` the discriminant (up to a non-zero scalar) is `? = -16*(4*A^3 + 27*B^2)`.
- Over any field the curve is non-singular iff `4*A^3 + 27*B^2 != 0` in that field.
- The implementation validates `4*A^3 + 27*B^2 != 0` inside the constructor and throws an `ArgumentException` if the curve is singular.

---

If you want this file moved, renamed, or expanded into a PDF / richer doc with step-by-step modular arithmetic tables, I can add that as well and commit it to the repository.

