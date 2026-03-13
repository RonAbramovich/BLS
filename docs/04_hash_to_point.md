# Hash-to-Point Algorithm

This document describes the hash-to-point function used in the BLS signature scheme to deterministically map messages to elliptic curve points.

---

## 1. Purpose

In BLS signatures, we need to convert an arbitrary message string into a point on an elliptic curve that lies in a specific subgroup of prime order r.

**Requirements**:
1. **Deterministic**: Same message always maps to same point
2. **Cryptographically sound**: Output should be uniformly distributed and unpredictable
3. **Subgroup membership**: Resulting point must have order r (or divide r)

---

## 2. Three-Step Process

The hash-to-point algorithm consists of three steps:

```
HashToPoint(message, curve):
    Step 1: x0 = MessageToInteger(message, p)
    Step 2: P_temp = IncrementAndTry(x0, curve)
    Step 3: H(m) = ClearCofactor(P_temp, curve)
    return H(m)
```

---

## 3. Step 1: Message to Integer Encoding

### 3.1 Purpose

Convert a string message into an integer in the range [0, p-1].

### 3.2 Algorithm

```
MessageToInteger(message, p):
    // Use Windows-1255 encoding (8-bit character encoding)
    bytes = Encode(message, "Windows-1255")
    
    // Interpret bytes as big-endian base-256 integer
    x0 = 0
    for each byte b in bytes:
        x0 = (x0 x 256 + b) mod p
    
    return x0
```

**Why Windows-1255**: Supports Hebrew characters and provides consistent byte encoding across platforms.

**Why Base-256**: Each byte represents a digit in base 256, creating a deterministic integer representation.

### 3.3 Example

Message: "AB" (ASCII values: 65, 66)
Prime: p = 101

- Initial: x0 = 0
- Process byte 65: x0 = (0 x 256 + 65) mod 101 = 65
- Process byte 66: x0 = (65 x 256 + 66) mod 101 = 16706 mod 101 = 40
- Result: x0 = 40

**Implementation**: `HashToPoint.EncodeMessageToInteger()`

---

## 4. Step 2: Increment-and-Try Mapping

### 4.1 Purpose

Find a valid point on the curve starting from x-coordinate x0.

### 4.2 Algorithm

```
IncrementAndTry(x0, curve):
    for increment = 0 to p-1:
        x = (x0 + increment) mod p
        z = x^3 + Ax + B  (mod p)
        
        if HasSquareRoot(z, p):
            y = SquareRoot(z, p)
            return Point(x, y)
    
    throw "Failed to find point"
```

**Key Idea**: If z is not a quadratic residue (no square root exists), try the next x value.

### 4.3 Quadratic Residue Test (Euler's Criterion)

For odd prime p and z != 0:
```
z is a square ? z^((p-1)/2) = 1 (mod p)
```

### 4.4 Square Root Computation (p = 3 mod 4)

For primes where p = 3 (mod 4), there's a simple formula:
```
If y^2 = z, then y = ± z^((p+1)/4) mod p
```

**Why This Works**:
- y^2 = z
- (z^((p+1)/4))^2 = z^((p+1)/2) = z^((p-1)/2) * z
- By Euler's criterion, z^((p-1)/2) = 1 for quadratic residues
- So (z^((p+1)/4))^2 = 1 * z = z (pass)

**Limitation**: This method only works for p = 3 (mod 4). Other cases require Tonelli-Shanks algorithm.

### 4.5 Complete Square Root Algorithm

```
SquareRootModP(z, p):
    if p mod 4 != 3:
        throw "Only supported for p = 3 (mod 4)"
    
    z = Normalize(z, p)
    if z = 0: return 0
    
    // Check Legendre symbol
    legendre = z^((p-1)/2) mod p
    if legendre != 1:
        return -1  // no square root exists
    
    // Compute square root
    y = z^((p+1)/4) mod p
    return y
```

**Implementation**: `NumberTheoryUtils.SqrtModP()`

### 4.6 Example

Curve: y^2 = x^3 + 2 over F_7 (note: 7 = 3 mod 4 (pass))
Starting: x0 = 1

**Iteration 1**: x = 1
- z = 1^3 + 2 = 3
- Legendre: 3^((7-1)/2) = 3^3 = 27 = 6 = -1 (mod 7)
- Not a square, continue

**Iteration 2**: x = 2
- z = 2^3 + 2 = 10 = 3 (mod 7)
- Legendre: 3^3 = -1 (mod 7)
- Not a square, continue

**Iteration 3**: x = 3
- z = 3^3 + 2 = 29 = 1 (mod 7)
- Legendre: 1^3 = 1 (pass)
- Square root: y = 1^((7+1)/4) = 1^2 = 1
- Point found: (3, 1)

**Implementation**: `HashToPoint.HashToCurve()` (increment loop)

---

## 5. Step 3: Cofactor Clearing

### 5.1 Purpose

Ensure the resulting point lies in the subgroup of prime order r.

### 5.2 Background

The elliptic curve group E(F_p) may have order |E| = h * r where:
- r is a large prime (the cryptographic subgroup order)
- h is the **cofactor** (usually small)

Not all points are in the r-subgroup. We must project P_temp into this subgroup.

### 5.3 Algorithm

```
ClearCofactor(P_temp, curve):
    h = |E| / r  // cofactor
    H_m = h x P_temp
    return H_m
```

**Why This Works**:
- By Lagrange's theorem, the order of H_m divides |E|
- Since |E| x P_temp has order dividing |E|, and we multiply by h = |E|/r
- The result h x P_temp has order dividing r
- Since r is prime, the order is exactly r (unless point is infinity)

### 5.4 Verification

To verify H(m) is in the r-subgroup:
```
r x H(m) = r x (h x P_temp) = |E| x P_temp = O  (pass)
```

### 5.5 Example

Curve with |E| = 6, r = 3:
- Cofactor h = 6/3 = 2
- P_temp = (3, 1) from previous example
- H(m) = 2 x (3, 1) = ...
  - Need to compute 2P on y^2 = x^3 + 2 over F_7
  - ? = (3*3^2 + 0)/(2*1) = 27/2
  - Inverse of 2 mod 7 is 4
  - ? = 27 x 4 = 108 = 3 (mod 7)
  - x3 = 3^2 - 2*3 = 9 - 6 = 3
  - y3 = 3(3 - 3) - 1 = -1 = 6 (mod 7)
  - H(m) = (3, 6)

Verification: 3 x H(m) should be O

**Implementation**: `HashToPoint.ClearCofactor()`

---

## 6. Complete Example

**Setup**:
- Prime: p = 7 (note: 7 = 3 mod 4 (pass))
- Curve: y^2 = x^3 + 1 over F_7
- Message: "hello"

**Step 1 - Encode**:
- "hello" bytes (ASCII): [104, 101, 108, 108, 111]
- x0 = 0
- x0 = (0 x 256 + 104) mod 7 = 104 mod 7 = 6
- x0 = (6 x 256 + 101) mod 7 = 1637 mod 7 = 6
- ... (continuing for all bytes)
- Result: x0 = some value in [0, 6]

**Step 2 - Find Point**:
- Try x = x0, x0+1, x0+2, ... until valid point found
- Compute z = x^3 + 1 for each x
- Check if z is a quadratic residue
- If yes, compute y = z^((7+1)/4) = z^2
- Return P_temp = (x, y)

**Step 3 - Clear Cofactor**:
- Compute h = |E| / r
- H(m) = h x P_temp
- Return H(m)

**Properties**:
- Deterministic: Same "hello" always gives same H(m)
- In r-subgroup: r x H(m) = O
- Secure: Output appears random without knowledge of mapping

---

## 7. Security Considerations

### 7.1 Why Increment-and-Try?

Alternative approaches (like hashing to x-coordinate directly) might introduce bias. The increment-and-try method:
- Tries consecutive x values until one works
- Distributes points relatively uniformly
- Is simple and deterministic

### 7.2 Why Cofactor Clearing?

Without cofactor clearing, the point might:
- Have small order (security vulnerability)
- Not lie in the cryptographic subgroup
- Break the pairing equation in BLS signatures

Multiplying by the cofactor ensures all points have order dividing r (and since r is prime, order exactly r).

---

## 8. Implementation Notes

### 8.1 Encoding Support

The implementation requires Windows-1255 encoding to be registered:
```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

This is done in test startup to support Hebrew and other special characters.

### 8.2 Prime Requirement

The current implementation requires p = 3 (mod 4) for efficient square root computation. For other primes, the Tonelli-Shanks algorithm would be needed.

### 8.3 Error Handling

If no valid point is found after trying all p candidates, an exception is thrown. In practice, this is extremely unlikely (approximately 50% of x values yield valid points).

---

## Summary

The hash-to-point algorithm provides a deterministic, secure method to map arbitrary messages to elliptic curve points in the correct cryptographic subgroup. The three-step process:

1. **Encode** message as integer (base-256 interpretation)
2. **Find** valid curve point using increment-and-try with Euler's criterion
3. **Clear cofactor** to ensure point is in the prime-order subgroup

This construction is essential for BLS signatures, where the signature s = a * H(m) requires hashing the message m to a curve point.

