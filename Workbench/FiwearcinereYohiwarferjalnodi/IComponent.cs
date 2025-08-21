using System.Text.Json.Serialization;

namespace FiwearcinereYohiwarferjalnodi;

[JsonPolymorphic()]
[JsonDerivedType(typeof(Component1), typeDiscriminator: "c1")]
[JsonDerivedType(typeof(Component2), typeDiscriminator: "c2")]
interface IComponent
{
}