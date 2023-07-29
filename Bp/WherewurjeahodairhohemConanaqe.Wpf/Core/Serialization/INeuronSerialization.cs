namespace WherewurjeahodairhohemConanaqe.Wpf.Core.Serialization;

public interface INeuronSerialization
{
    void Serialize(SerializeContext context);
    void Deserialize(DeserializeContext context);
}

public class SerializeContext
{
}

public class DeserializeContext
{
}
