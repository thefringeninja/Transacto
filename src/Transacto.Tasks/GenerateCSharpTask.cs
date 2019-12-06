using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Json.Schema;
using Microsoft.Json.Schema.ToDotNet;

namespace Transacto.Tasks {
    public class GenerateCSharpTask : Task {
        [Required] public ITaskItem[] SchemaFiles { get; set; }
        [Required] public string RootNamespace { get; set; }
        [Required] public string MSBuildProjectDirectory { get; set; }
        [Required] public string IntermediateOutputPath { get; set; }

        public override bool Execute() {
            var success = true;

            foreach (var schemaFile in SchemaFiles) {
                try {
                    var inputFile = schemaFile.ToString();
                    var schema = SchemaReader.ReadSchema(File.ReadAllText(inputFile), inputFile);
                    var rootClassName = inputFile.Split(Path.DirectorySeparatorChar).Last().Split('.')[0];
                    var namespaceName = $"{RootNamespace}.{inputFile.Remove(0, MSBuildProjectDirectory.Length + 1)}";
                    namespaceName = namespaceName.Substring(0,
                        namespaceName.Length - (rootClassName.Length + ".schema.json".Length + 1));

                    var settings = new DataModelGeneratorSettings {
                        OutputDirectory = IntermediateOutputPath + Path.DirectorySeparatorChar + namespaceName,
                        ForceOverwrite = true,
                        NamespaceName = namespaceName,
                        RootClassName = rootClassName,
                        SchemaName = rootClassName
                    };

                    new DataModelGenerator(settings).Generate(schema);
                } catch (Exception ex) {
                    success = false;
                    Log.LogWarning($"Class generation for schema {schemaFile} failed: {ex.Message}");
                }
            }

            return success;
        }
    }
}
