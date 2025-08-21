using System.Text.Json.Serialization;

namespace FiwearcinereYohiwarferjalnodi;

[JsonPolymorphic()]
[JsonDerivedType(typeof(Component1))]
[JsonDerivedType(typeof(Component2))]
interface IComponent
{
}