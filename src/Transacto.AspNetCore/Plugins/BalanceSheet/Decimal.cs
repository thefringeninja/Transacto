namespace Transacto.Plugins.BalanceSheet; 

partial record Decimal {
	public Decimal() {
		Value = "0";
	}

	public decimal DecimalValue {
		get => decimal.TryParse(Value, out var value) ? value : decimal.Zero;
		init => Value = value.ToString();
	}

	public static explicit operator decimal(Decimal @decimal) => @decimal.DecimalValue;
	public static Decimal operator +(decimal a, Decimal b) => new() { DecimalValue = b.DecimalValue + a };
	public static Decimal operator +(Decimal a, decimal b) => new() { DecimalValue = a.DecimalValue + b };
}