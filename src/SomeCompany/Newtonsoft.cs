using System;

namespace Newtonsoft.Json {
    public class JsonProperty : Attribute {
        public string Name { get; set; }
        public Required Required { get; set; }

        public JsonProperty(string name) {
            Name = name;
        }
    }

    public enum Required {
        Always
    }
}
