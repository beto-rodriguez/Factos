using System.Text.Json.Serialization;

namespace Factos.Abstractions.Dto;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestMethodIdentifierPropertyDto), "TestMethodIdentifier")]
[JsonDerivedType(typeof(DiscoveredTestNodeStatePropertyDto), "DiscoveredTestNodeState")]
[JsonDerivedType(typeof(SkippedTestNodeStatePropertyDto), "SkippedTestNodeState")]
[JsonDerivedType(typeof(PassedTestNodeStatePropertyDto), "PassedTestNodeState")]
[JsonDerivedType(typeof(FailedTestNodeStatePropertyDto), "FailedTestNodeState")]
[JsonDerivedType(typeof(ErrorTestNodeStatePropertyDto), "ErrorTestNodeState")]
public abstract class PropertyDto
{ }
