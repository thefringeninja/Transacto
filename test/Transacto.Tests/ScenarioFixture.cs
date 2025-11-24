using AutoFixture;

namespace Transacto;

partial class ScenarioFixture {
	public ScenarioFixture() {
		this.CustomizeNodaTime();

		this.CustomizeAccountingPeriod();

		this.CustomizeAccountName();

		this.CustomizeAccountNumber();

		this.CustomizeAccount();

		this.CustomizeGeneralLedgerEntryNumberPrefix();

		this.CustomizeMoney();

		this.CustomizeCredit();

		this.CustomizeDebit();
		
		this.CustomizeCashFlowCategory();
	}
}
