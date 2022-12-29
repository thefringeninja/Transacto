namespace Transacto.Plugins.BalanceSheet; 

partial record Decimal {
	public static readonly Decimal Zero = new() { Value = "0" };

	public static Decimal Create(decimal value) => Zero with { DecimalValue = value };
	public decimal DecimalValue {
		get => decimal.TryParse(Value, out var value) ? value : decimal.Zero;
		init => Value = value.ToString();
	}

	public static explicit operator decimal(Decimal @decimal) => @decimal.DecimalValue;
	public static Decimal operator +(decimal a, Decimal b) => Create(b.DecimalValue + a);
	public static Decimal operator +(Decimal a, decimal b) => Create(a.DecimalValue + b);
}
