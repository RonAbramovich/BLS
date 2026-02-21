namespace BLS.Fields.Interfaces
{
    public interface IFieldElement<TSelf> where TSelf : IFieldElement<TSelf> // TSelf is concrete type - for avoiding boxing and enabling static dispatch
    {
        TSelf Add(TSelf other);
        TSelf Sub(TSelf other);
        TSelf Multiply(TSelf other);
        TSelf AdditiveInverse();
        TSelf MultiplicativeInverse();                // multiplicative inverse (error if zero)
        TSelf Power(long exponent);

        bool IsZero { get; }
        bool Equals(TSelf other);
    }
}