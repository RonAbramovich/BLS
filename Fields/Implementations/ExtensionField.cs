using System;
using System.Collections.Generic;
using System.Numerics;
using BLS.Fields.Interfaces;

namespace BLS.Fields.Implementations
{
    public class ExtensionField : IField<ExtensionFieldElement>
    {
        public BigInteger Characteristic { get; }
        public int ExtensionDegree { get; }

        public PrimeField BaseField { get; }
        public Polynomial ModulusPolynomial { get; }

        private List<KeyValuePair<BigInteger,int>>? _multiplicativeOrderFactors;

        public ExtensionField(PrimeField baseField, Polynomial modulusPolynomial, bool enforceIrreducible = true)
        {
            BaseField = baseField ?? throw new ArgumentNullException(nameof(baseField));
            ModulusPolynomial = modulusPolynomial ?? throw new ArgumentNullException(nameof(modulusPolynomial));

            if (ModulusPolynomial.Modulus != baseField.Characteristic)
            {
                throw new ArgumentException("Modulus polynomial coefficients must be over the base field characteristic.", nameof(modulusPolynomial));
            }

            if (ModulusPolynomial.Degree <= 0)
            {
                throw new ArgumentException("Modulus polynomial must have positive degree.", nameof(modulusPolynomial));
            }

            // ensure monic
            if (ModulusPolynomial[ModulusPolynomial.Degree] != 1)
            {
                throw new ArgumentException("Modulus polynomial must be monic.", nameof(modulusPolynomial));
            }

            Characteristic = baseField.Characteristic;
            ExtensionDegree = ModulusPolynomial.Degree;

            if (enforceIrreducible && !PolynomialUtils.IsIrreducible(ModulusPolynomial, BaseField))
            {
                throw new ArgumentException("Provided modulus polynomial is not irreducible over the base field.", nameof(modulusPolynomial));
            }
        }

        public ExtensionFieldElement FromInt(BigInteger value)
        {
            return new ExtensionFieldElement(this, new Polynomial(Characteristic, NumberTheoryUtils.ModNormalize(value, Characteristic)));
        }

        public BigInteger MultiplicativeGroupOrder => BigInteger.Pow(Characteristic, ExtensionDegree) - 1;

        public IReadOnlyList<KeyValuePair<BigInteger,int>> MultiplicativeGroupOrderFactorization
        {
            get
            {
                if (_multiplicativeOrderFactors is null)
                {
                    _multiplicativeOrderFactors = NumberTheoryUtils.Factorize(MultiplicativeGroupOrder);
                }
                return _multiplicativeOrderFactors;
            }
        }

        public ExtensionFieldElement Zero => new ExtensionFieldElement(this, new Polynomial(Characteristic));

        public ExtensionFieldElement One => new ExtensionFieldElement(this, new Polynomial(Characteristic, 1));
    }
}
