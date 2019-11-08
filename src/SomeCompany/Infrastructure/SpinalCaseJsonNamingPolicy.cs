using System.Text.Json;
using System.Text.RegularExpressions;

namespace SomeCompany.Infrastructure {
    public class SpinalCaseJsonNamingPolicy : JsonNamingPolicy {
        public override string ConvertName(string name) => Regex.Replace(
                Regex.Replace(Regex.Replace(name, @"([A-Z]+)([A-Z][a-z])", "$1-$2"), @"([a-z\d])([A-Z])", "$1-$2"),
                @"[-\s]", "-")
            .ToLower();
    }
}
