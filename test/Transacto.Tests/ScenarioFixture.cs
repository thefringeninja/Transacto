using AutoFixture;

namespace Transacto {
    internal class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            this.CustomizePeriodIdentifier();

            this.CustomizeAccountName();

            this.CustomizeAccountNumber();

            this.CustomizeAccountType();

            this.CustomizeGeneralLedgerEntryNumber();

            this.CustomizeMoney();

            this.CustomizeCredit();

            this.CustomizeDebit();
        }
    }
}
