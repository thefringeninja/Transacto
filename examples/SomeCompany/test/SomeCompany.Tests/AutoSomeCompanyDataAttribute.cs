using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture.Xunit2;
using Xunit.Sdk;

namespace SomeCompany {
    [DataDiscoverer("AutoFixture.Xunit2.NoPreDiscoveryDataDiscoverer", "AutoFixture.Xunit2")]
    public class AutoSomeCompanyDataAttribute : DataAttribute {
        public int Iterations { get; }

        public AutoSomeCompanyDataAttribute(int iterations = 3) {
            Iterations = iterations;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod) {
            var customAutoData = new CustomAutoData();

            return Enumerable.Range(0, Iterations).SelectMany(_ => customAutoData.GetData(testMethod));
        }

        private class CustomAutoData : AutoDataAttribute {
            public CustomAutoData() : base(() => new ScenarioFixture {

            }) {
            }
        }
    }
}
