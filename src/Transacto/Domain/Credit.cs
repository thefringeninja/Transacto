using System.Numerics;
using System.Text;

namespace Transacto.Domain;

public readonly struct Credit : IEquatable<Credit>,
	IAdditiveIdentity<Credit, Money>,
	IAdditionOperators<Credit, Money, Credit>,
	IAdditionOperators<Credit, decimal, Credit>,
	ISubtractionOperators<Credit, Money, Credit>,
	ISubtractionOperators<Credit, decimal, Credit> {

	public static Money AdditiveIdentity { get; } = Money.AdditiveIdentity;

	public AccountNumber AccountNumber { get; }
	public Money Amount { get; }

	public Credit(AccountNumber accountNumber) : this(accountNumber, Money.Zero) {
	}

	public Credit(AccountNumber accountNumber, Money amount) {
		ArgumentOutOfRangeException.ThrowIfLessThan(amount, Money.Zero);

		Amount = amount;
		AccountNumber = accountNumber;
	}

	public override int GetHashCode() => unchecked((Amount.GetHashCode() * 397) ^ AccountNumber.GetHashCode());
	public bool Equals(Credit other) => Amount.Equals(other.Amount) && AccountNumber.Equals(other.AccountNumber);
	public override bool Equals(object? obj) => obj is Credit other && Equals(other);
	public static bool operator ==(Credit left, Credit right) => left.Equals(right);
	public static bool operator !=(Credit left, Credit right) => !left.Equals(right);

	public static Credit operator +(Credit left, Money right) =>
		new(left.AccountNumber, left.Amount + right);

	public static Credit operator -(Credit left, Money right) =>
		new(left.AccountNumber, left.Amount - right);

	public static Credit operator +(Credit left, decimal right) =>
		new(left.AccountNumber, left.Amount + right);

	public static Credit operator -(Credit left, decimal right) =>
		new(left.AccountNumber, left.Amount - right);

	public override string ToString() {
		var builder = new StringBuilder().Append(AccountNumber).Append('\t').Append('\t');
		if (Amount < Money.Zero) {
			builder.Append('(').Append(Amount).Append(')');
		} else {
			builder.Append(Amount);
		}
		
		return builder.ToString();
	}
}
