using AutoFixture;

namespace Transacto {
    internal class ScenarioFixture : Fixture {
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
        }
    }
}
