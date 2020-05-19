using AutoFixture;

namespace Transacto {
    internal class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            this.CustomizePeriodIdentifier();

            this.CustomizeAccountName();

            this.CustomizeAccountNumber();

            this.CustomizeMoney();

            this.CustomizeCredits();

            this.CustomizeDebits();
        }
    }
}
