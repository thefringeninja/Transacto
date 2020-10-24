using AutoFixture;
using Transacto;

namespace SomeCompany {
    internal class ScenarioFixture : Fixture {
        public ScenarioFixture() {
            OmitAutoProperties = false;
            this.CustomizePeriodIdentifier();

            this.CustomizeAccountName();

            this.CustomizeAccountNumber();

            this.CustomizeMoney();
        }
    }
}
