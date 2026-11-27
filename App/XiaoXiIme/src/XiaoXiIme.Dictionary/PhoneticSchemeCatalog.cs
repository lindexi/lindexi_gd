using System.Security.Cryptography;
using System.Text;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>Shared product scheme inventory; a known name alone does not establish an implemented layout or installed package.</summary>
public static class PhoneticSchemeCatalog
{
    private const string XiaoheRules =
        "initials:b=b,p=p,m=m,f=f,d=d,t=t,n=n,l=l,g=g,k=k,h=h,j=j,q=q,x=x,r=r,z=z,c=c,s=s,y=y,w=w,zh=v,ch=i,sh=u\n"
        + "finals:iu=q,ei=w,uan=r,ue=t,ve=t,un=y,uo=o,ie=p,ong=s,iong=s,ai=d,en=f,eng=g,ang=h,an=j,uai=k,ing=k,uang=l,iang=l,ou=z,ua=x,ia=x,iao=n,ao=c,ui=v,in=b,ian=m,a=a,o=o,e=e,i=i,u=u,v=v\n"
        + "zero:a,ai,an,ang,ao,e,ei,en,eng,er,o,ou\n"
        + "zero-rule:single=repeat;pair=literal;long=first+final\n"
        + "normalize:lowerInvariant;u:=v;ü=v;y/w=ordinary-initials";

    /// <summary>Fixed display order, independent of persisted enum numbers. Unverified schemes deliberately have no product identity.</summary>
    public static IReadOnlyList<PhoneticSchemeDefinition> All { get; } = Array.AsReadOnly
    (
        new[]
        {
            new PhoneticSchemeDefinition
            (
                "fullPinyin", "Full", ImeProductInputScheme.FullPinyin,
                "repositoryBaseline", "XiaoXiIme/src/XiaoXiIme.Dictionary/PhoneticInputProjection.cs",
                "repository-full-v1", "full-v1"
            ),
            Observed("sogouDoublePinyin", "Sogou", "搜狗双拼", ObservedPhoneticLayouts.Sogou, true),
            Observed("microsoftDoublePinyin", "Microsoft", "微软双拼", ObservedPhoneticLayouts.Microsoft, true),
            new PhoneticSchemeDefinition
            (
                "naturalCodeDoublePinyin", "NaturalCode", ImeProductInputScheme.NaturalCodeDoublePinyin,
                "recalledCommonProductBaseline", "Docs/natural-code-production.md", "common-natural-code-v1",
                "natural-code-v1",
                NaturalCodePhoneticLayout.Rules,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NaturalCodePhoneticLayout.Rules))),
                NaturalCodePhoneticLayout.Layout, Array.Empty<string>()
            ),
            Observed("abcDoublePinyin", "Abc", "智能 ABC", ObservedPhoneticLayouts.Abc, false),
            Missing("daniuDoublePinyin", "Daniu"),
            Observed("jiajiaDoublePinyin", "Jiajia", "拼音加加", ObservedPhoneticLayouts.Jiajia, false),
            new PhoneticSchemeDefinition
            (
                "xiaoheDoublePinyin", "Double", ImeProductInputScheme.XiaoheDoublePinyin,
                "repositoryBaseline", "XiaoXiIme/src/XiaoXiIme.Dictionary/PhoneticInputProjection.cs",
                "repository-xiaohe-v1", "xiaohe-v1",
                XiaoheRules, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(XiaoheRules))),
                ObservedPhoneticLayouts.Xiaohe(XiaoheRules)
            ),
            Missing("xiaoyueDoublePinyin", "Xiaoyue")
        }
    );

    private static PhoneticSchemeDefinition Observed
        (string id, string resourceKey, string imageTitle, PhoneticLayout layout, bool unnamedIng)
    {
        var rules = "initials:" + string.Join
                                (
                                    ',',
                                    layout.Initials.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select
                                        (pair => $"{pair.Key}={pair.Value}")
                                )
                                + "\nfinals:" + string.Join
                                (
                                    ',',
                                    layout.Finals.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select
                                        (pair => $"{pair.Key}={pair.Value}")
                                );
        rules += "\nzero:" + string.Join
                           (
                               ',',
                               layout.ZeroInitialCodes!.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select
                                   (pair => $"{pair.Key}={pair.Value}")
                           )
                           + "\nnormalize:lowerInvariant;u:=v;ü=v;nl-ue=ve;jqxy-v=u;y/w=ordinary-initials;missing-ve=ue"
                           + (unnamedIng ? "\ning-key:common-product-baseline-semicolon" : string.Empty);
        var product = resourceKey switch
        {
            "Sogou" => ImeProductInputScheme.SogouDoublePinyin,
            "Microsoft" => ImeProductInputScheme.MicrosoftDoublePinyin,
            "Abc" => ImeProductInputScheme.AbcDoublePinyin,
            "Jiajia" => ImeProductInputScheme.JiajiaDoublePinyin,
            _ => throw new InvalidOperationException()
        };
        return new
        (
            id, resourceKey, product, "userProvidedLocalImage+commonProductBaseline", $"Docs/输入法方案表/{imageTitle}.png",
            null, "production-layout-v2",
            rules, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rules))), layout, Array.Empty<string>()
        );
    }

    private static PhoneticSchemeDefinition Missing(string id, string resourceKey) =>
        new(id, resourceKey, null, "unverified", string.Empty, string.Empty, string.Empty);
}