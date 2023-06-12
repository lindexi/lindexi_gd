using dotnetCampus.Configurations;

namespace ChelbemlelnujeGellallweweewere;

class FooConfiguration : Configuration
{
    public string Name
    {
        set => SetValue(value);
        get => GetString();
    }
}