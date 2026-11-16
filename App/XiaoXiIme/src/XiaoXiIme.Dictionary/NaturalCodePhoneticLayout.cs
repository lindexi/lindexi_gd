namespace XiaoXiIme.Dictionary;

internal static class NaturalCodePhoneticLayout
{
    internal const string Rules =
        "initials:b=b,p=p,m=m,f=f,d=d,t=t,n=n,l=l,g=g,k=k,h=h,j=j,q=q,x=x,r=r,z=z,c=c,s=s,y=y,w=w,zh=v,ch=i,sh=u\n"
        + "finals:a=a,ou=b,iao=c,uang=d,iang=d,e=e,en=f,eng=g,ang=h,i=i,an=j,ao=k,ai=l,ian=m,in=n,o=o,uo=o,un=p,iu=q,uan=r,er=r,ong=s,iong=s,ue=t,ve=t,u=u,ui=v,v=v,ia=w,ua=w,ie=x,uai=y,ing=y,ei=z\n"
        + "zero:a=aa,ai=ai,an=an,ang=ah,ao=ao,e=ee,ei=ei,en=en,eng=eg,er=er,o=oo,ou=ou\n"
        + "normalize:lowerInvariant;u:=v;ü=v;nl-ue=ve;jqxy-v=u;y/w=ordinary-initials\n"
        + "collisions:preserve-all-components;semicolon=unhandled";

    internal static PhoneticLayout Layout { get; } = Create();

    private static PhoneticLayout Create()
    {
        var lines = Rules.Split('\n');
        var initials = ParseKeys(lines[0]["initials:".Length..]);
        var finals = ParseKeys(lines[1]["finals:".Length..]);
        var zero = lines[2]["zero:".Length..].Split(',').ToDictionary
        (
            item => item[..item.IndexOf('=')], item => item[(item.IndexOf('=') + 1)..], StringComparer.Ordinal
        );
        return new(initials, finals, zero);
    }

    private static Dictionary<string, char> ParseKeys(string value) => value.Split(',').ToDictionary
    (
        item => item[..item.IndexOf('=')], item => item[^1], StringComparer.Ordinal
    );
}