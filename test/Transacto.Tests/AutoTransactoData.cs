using System.Collections.Generic;
using System.Reflection;
using AutoFixture;
using AutoFixture.Xunit2;
using Xunit.Sdk;

namespace Transacto {
    [DataDiscoverer("AutoFixture.Xunit2.NoPreDiscoveryDataDiscoverer", "AutoFixture.Xunit2")]
    public class AutoTransactoDataAttribute : DataAttribute {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
            => new CustomAutoData().GetData(testMethod);

        private class CustomAutoData : AutoDataAttribute {
            public CustomAutoData() : base(() => new Fixture().Customize()) {

            }
        }
    }
}
