using Factos.Abstractions.Dto;
using Factos.RemoteTesters;
using System.Text.Json.Serialization;

namespace Factos.Abstractions;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecutionResponse))]
[JsonSerializable(typeof(IEnumerable<TestNodeDto>))]
internal partial class JsonGenerationContext : JsonSerializerContext
{ }
