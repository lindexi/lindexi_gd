namespace FafurbaliHerekaylarnerecerne;

class helpstringAttribute : Attribute
{
    public helpstringAttribute(string description)
    {
        Description = description;
    }
    public string Description { get; }
}