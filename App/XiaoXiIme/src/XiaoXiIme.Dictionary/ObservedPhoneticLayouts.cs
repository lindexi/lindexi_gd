namespace XiaoXiIme.Dictionary;

internal static class ObservedPhoneticLayouts
{
    private const string OrdinaryInitials =
        "b=b p=p m=m f=f d=d t=t n=n l=l g=g k=k h=h j=j q=q x=x r=r z=z c=c s=s y=y w=w ";

    internal static PhoneticLayout Sogou { get; } = Create
    (
        "zh=v ch=i sh=u",
        "a=a ou=b iao=c iang=d uang=d e=e en=f eng=g ang=h i=i an=j ao=k ai=l ian=m in=n o=o uo=o un=p iu=q uan=r er=r ong=s iong=s ve=t ue=t u=u ui=v ia=w ua=w ie=x uai=y v=y ei=z"
    );

    internal static PhoneticLayout Microsoft { get; } = Create
    (
        "zh=v ch=i sh=u",
        "a=a ou=b iao=c iang=d uang=d e=e en=f eng=g ang=h i=i an=j ao=k ai=l ian=m in=n o=o uo=o un=p iu=q uan=r er=r ong=s iong=s ue=t u=u ve=v ui=v ia=w ua=w ie=x uai=y v=y ei=z"
    );

    internal static PhoneticLayout Abc { get; } = Create
    (
        "zh=a ch=e sh=v",
        "a=a ou=b in=c uai=c ia=d ua=d e=e en=f eng=g ang=h i=i an=j ao=k ai=l ue=m ui=m un=n o=o uo=o uan=p ei=q iu=r er=r ong=s iong=s iang=t uang=t u=u ve=v v=v ian=w ie=x ing=y iao=z"
    );

    internal static PhoneticLayout Jiajia { get; } = Create
    (
        "zh=v ch=u sh=i",
        "a=a ia=b ua=b uan=c ao=d e=e an=f ang=g iang=h uang=h i=i ian=j iao=k in=l ie=m iu=n o=o uo=o ou=p ing=q er=q en=r ai=s eng=t u=u ui=v v=v ei=w ue=x uai=x ong=y iong=y un=z"
    );

    internal static PhoneticLayout Xiaohe(string rules)
    {
        var lines = rules.Split('\n');
        var initials = Parse(lines[0]["initials:".Length..].Replace(',', ' '));
        var finals = Parse(lines[1]["finals:".Length..].Replace(',', ' '));
        var observed = new Dictionary<string, char>(finals, StringComparer.Ordinal) { ["van"] = 'r', ["vn"] = 'y' };
        var zero = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["a"] = "aa", ["ai"] = "ai", ["an"] = "an", ["ang"] = "ah", ["ao"] = "ao",
            ["e"] = "ee", ["ei"] = "ei", ["en"] = "en", ["eng"] = "eg", ["er"] = "er", ["o"] = "oo", ["ou"] = "ou"
        };
        return new(initials, finals, zero, observed);
    }

    private static PhoneticLayout Create(string initials, string finals)
    {
        var mappings = Parse(finals);
        if (initials == "zh=v ch=i sh=u")
        {
            mappings.Add("ing", ';');
        }

        var zero = "a ai an ang ao e ei en eng er o ou".Split(' ').ToDictionary
        (
            syllable => syllable,
            syllable => string.Concat(initials == "zh=v ch=u sh=i" ? syllable[0] : 'o', mappings[syllable]),
            StringComparer.Ordinal
        );
        return new(Parse(OrdinaryInitials + initials), mappings, zero);
    }

    private static Dictionary<string, char> Parse(string mappings) => mappings.Split(' ')
        .ToDictionary(item => item[..item.IndexOf('=')], item => item[^1], StringComparer.Ordinal);
}