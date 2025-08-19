using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Analyzers
{
    [Generator]
    public class ProtoPartialGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var protoFilesProvider = context.CompilationProvider
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select((pair, cancellationToken) =>
                {
                    var (compilation, optionsProvider) = pair;
                    return ProtobufHelpers.DiscoverProtoFiles(compilation, optionsProvider).ToImmutableArray();
                });

            context.RegisterSourceOutput(protoFilesProvider, (sourceProductionContext, protoFiles) =>
            {
                foreach (var (filePath, content) in protoFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);

                    List<string> baseTypes = [];

                    if (fileName.EndsWith("Event") && fileName != "Event")
                    {
                        baseTypes.Add("Paramore.Brighter.IEvent");
                    }
                    else if (fileName.EndsWith("Command") && fileName != "Command")
                    {
                        baseTypes.Add("Paramore.Brighter.ICommand");
                    }
                    else if (fileName.EndsWith("Request") && fileName != "Request")
                    {
                        baseTypes.AddRange(["Paramore.Brighter.ICommand", "Paramore.Brighter.ICall"]);
                    }
                    else if (fileName.EndsWith("Response") && fileName != "Response")
                    {
                        baseTypes.AddRange(["Paramore.Brighter.ICommand", "Paramore.Brighter.IResponse", "Shared.IReply"]);
                    }
                    else
                    {
                        continue;
                    }

                    var namespaceName = ProtobufHelpers.GetNamespace(content);

                    if (string.IsNullOrEmpty(namespaceName))
                    {
                        continue;
                    }

                    var sourceCode = GeneratePartialClass(fileName, baseTypes, namespaceName, filePath);

                    sourceProductionContext.AddSource($"{fileName}.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
                }

            });
        }

        private string GeneratePartialClass(string className, List<string> baseTypes, string namespaceName, string protoFilePath)
        {
            var relativePath = Path.GetFileName(protoFilePath);

            string requestProperties = @"
        /// <summary>
        /// Correlates this command with a previous command or event.
        /// </summary>
        /// <value>The <see cref=""Id""/> that correlates this command with a previous command or event.</value>
        [JsonConverter(typeof(IdConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(NIdConverter))]
        [JsonSchema(JsonObjectType.String)]
        public Id? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The <see cref=""Id""/> that uniquely identifies this event instance.</value>
        [NotNull]
        [JsonConverter(typeof(IdConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(NIdConverter))]
        [JsonSchema(JsonObjectType.String)]
        public Id Id { get; set; } = Paramore.Brighter.Id.Random();";

            string eventProperties = requestProperties;

            string commandProperties = requestProperties;

            string callProperties = @"
        /// <summary>
        /// Gets the address of the queue to reply to - usually private to the sender.
        /// </summary>
        /// <value>A <see cref=""ReplyAddress""/> specifying where the response should be sent.</value>
        public ReplyAddress ReplyAddress { get; set; } = new ReplyAddress("""","""");";

            string responseProperties = @"
        /// <summary>
        /// Gets the channel that we should reply to the sender on.
        /// </summary>
        /// <value>A <see cref=""ReplyAddress""/> specifying where this reply should be sent.</value>
        public ReplyAddress SendersAddress { get; set; } = new ReplyAddress("""","""");";

            string properties = string.Empty;

            foreach (var baseType in baseTypes)
            {
                var props = baseType switch
                {
                    "Paramore.Brighter.IEvent" => eventProperties,
                    "Paramore.Brighter.ICommand" => commandProperties,
                    "Paramore.Brighter.ICall" => callProperties,
                    "Paramore.Brighter.IResponse" => responseProperties,
                    _ => ""
                };

                properties = properties + Environment.NewLine + props;
            }

            return $@"// <auto-generated />
// Generated from: {relativePath}

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

using NJsonSchema;
using NJsonSchema.Annotations;

using Paramore.Brighter;
using Paramore.Brighter.JsonConverters;
using Paramore.Brighter.NJsonConverters;

namespace {namespaceName}
{{
    /// <summary>
    /// Auto-generated partial class for {className} based on {relativePath}
    /// </summary>
    public partial class {className} : {string.Join(", ", baseTypes)}
    {{
        {properties}
    }}
}}";
        }
    }
}
