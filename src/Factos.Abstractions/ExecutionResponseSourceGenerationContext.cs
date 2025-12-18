using Factos.Abstractions.Dto;
using System.Text.Json.Serialization;

namespace Factos.Abstractions;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TestNodeDto))]
[JsonSerializable(typeof(List<TestNodeDto>))]
[JsonSerializable(typeof(IEnumerable<TestNodeDto>))]
internal partial class JsonGenerationContext : JsonSerializerContext
{ }
