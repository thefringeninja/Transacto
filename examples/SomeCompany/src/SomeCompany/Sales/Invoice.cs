using Transacto;
using Transacto.Domain;

namespace SomeCompany.Sales;

public class Invoice(int invoiceNumber, Money sales, Money shipping, Money inventory, Percentage salesTaxRate)
	: IBusinessTransaction {
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("invoice");

	public Percentage SalesTaxRate { get; } = salesTaxRate;
	public Money Inventory { get; } = inventory;
	public Money Shipping { get; } = shipping;
	public Money Sales { get; } = sales;
	public int InvoiceNumber { get; } = invoiceNumber;
	public Money Tax => Sales * SalesTaxRate;
	public Money Total => Sales + Shipping + Tax;
	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; init; }

	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<object> GetTransactionItems() {
		var tax = Sales * SalesTaxRate;
		var total = Sales + Shipping + tax;
		yield return new Debit(new AccountNumber(1100), total);
		yield return new Credit(new AccountNumber(1200), Inventory);
		yield return new Credit(new AccountNumber(2300), tax);
		yield return new Credit(new AccountNumber(4000), Sales);
		yield return new Credit(new AccountNumber(4050), Shipping);
		yield return new Debit(new AccountNumber(5000), Inventory);
	}
}

public class Payment(int paymentNumber, Money total) : IBusinessTransaction {
	public Money Total { get; } = total;
	public int PaymentNumber { get; } = paymentNumber;

	public GeneralLedgerEntrySequenceNumber? SequenceNumber { get; init; }
	public static GeneralLedgerEntryNumberPrefix Prefix { get; } = new("payment");
	public GeneralLedgerEntryNumber? ReferenceNumber =>
		SequenceNumber.HasValue ? new GeneralLedgerEntryNumber(Prefix, SequenceNumber.Value) : null;

	public IEnumerable<object> GetTransactionItems() {
		yield return new Credit(new AccountNumber(1100), Total);
		yield return new Debit(new AccountNumber(1000), Total);
	}
}

public class Sales : IPlugin {
	public string Name { get; } = nameof(Sales);
	public IEnumerable<Type> MessageTypes { get; } = [typeof(Invoice), typeof(Payment)];

	public void ConfigureServices(IServiceCollection services) {
		services.AddBusinessTransaction<Invoice>().AddBusinessTransaction<Payment>();
	}

	public void Configure(IEndpointRouteBuilder builder) {
		builder.MapBusinessTransaction<Invoice>("/invoices");
		builder.MapBusinessTransaction<Payment>("/payments");
	}

	public IEnumerable<LinkRelation> LinkRelations => [];
}
