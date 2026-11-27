using System.Collections.Frozen;

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

    // The component projection is intentionally permissive; only these syllables reserve a double key.
    private const string Syllables = """
        ba bai ban bang bao bei ben beng bi bian biao bie bin bing bo bu biang
        pa pai pan pang pao pei pen peng pi pian piao pie pin ping po pou pu
        ma mai man mang mao me mei men meng mi mian miao mie min ming miu mo mou mu
        fa fan fang fei fen feng fo fou fu
        da dai dan dang dao de dei den deng di dia dian diao die ding diu dong dou du duan dui dun duo
        ta tai tan tang tao te tei teng ti tian tiao tie ting tong tou tu tuan tui tun tuo
        na nai nan nang nao ne nei nen neng ni nian niang niao nie nin ning niu nong nou nu nuan nuo nv nve
        la lai lan lang lao le lei leng li lia lian liang liao lie lin ling liu lo long lou lu luan lun luo lv lve
        ga gai gan gang gao ge gei gen geng gong gou gu gua guai guan guang gui gun guo
        ka kai kan kang kao ke kei ken keng kong kou ku kua kuai kuan kuang kui kun kuo
        ha hai han hang hao he hei hen heng hong hou hu hua huai huan huang hui hun huo
        ji jia jian jiang jiao jie jin jing jiong jiu ju juan jue jun
        qi qia qian qiang qiao qie qin qing qiong qiu qu quan que qun
        xi xia xian xiang xiao xie xin xing xiong xiu xu xuan xue xun
        zha zhai zhan zhang zhao zhe zhei zhen zheng zhi zhong zhou zhu zhua zhuai zhuan zhuang zhui zhun zhuo
        cha chai chan chang chao che chen cheng chi chong chou chu chua chuai chuan chuang chui chun chuo
        sha shai shan shang shao she shei shen sheng shi shou shu shua shuai shuan shuang shui shun shuo
        ran rang rao re ren reng ri rong rou ru rua ruan rui run ruo
        za zai zan zang zao ze zei zen zeng zi zong zou zu zuan zui zun zuo
        ca cai can cang cao ce cen ceng ci cong cou cu cuan cui cun cuo
        sa sai san sang sao se sen seng si song sou su suan sui sun suo
        ya yan yang yao ye yi yin ying yo yong you yu yuan yue yun
        wa wai wan wang wei wen weng wo wu
        """;

    internal static FrozenSet<char> InitialKeys { get; } = Layout.Initials.Values.ToFrozenSet();

    internal static FrozenSet<string> SyllableCodes { get; } = Layout.ZeroInitialCodes!.Values
        .Concat(Syllables.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(syllable =>
        {
            var initialLength = syllable[1] == 'h' ? 2 : 1;
            return string.Concat(Layout.Initials[syllable[..initialLength]], Layout.Finals[syllable[initialLength..]]);
        })).ToFrozenSet(StringComparer.Ordinal);

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