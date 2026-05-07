using JawjeleceeYairlubelhearrene.Models;

namespace JawjeleceeYairlubelhearrene.Services;

internal static partial class SpeakerCatalogService
{
    public static IReadOnlyList<SpeakerOption> LoadSpeakerOptions()
    {
        IReadOnlyList<SpeakerOption> speakerList =
        [
            new SpeakerOption("Vivi 2.0", "zh_female_vv_uranus_bigtts", "中文、日文、印尼、墨西哥西班牙语", "通用场景"),
            new SpeakerOption("小何 2.0", "zh_female_xiaohe_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("云舟 2.0", "zh_male_m191_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("小天 2.0", "zh_male_taocheng_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("刘飞 2.0", "zh_male_liufei_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("魅力苏菲 2.0", "zh_female_sophie_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("清新女声 2.0", "zh_female_qingxinnvsheng_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("甜美小源 2.0", "zh_female_tianmeixiaoyuan_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("甜美桃子 2.0", "zh_female_tianmeitaozi_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("爽快思思 2.0", "zh_female_shuangkuaisisi_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("邻家女孩 2.0", "zh_female_linjianvhai_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("少年梓辛/Brayan 2.0", "zh_male_shaonianzixin_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("魅力女友 2.0", "zh_female_meilinvyou_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("温柔妈妈 2.0", "zh_female_wenroumama_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("解说小明 2.0", "zh_male_jieshuoxiaoming_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("TVB女声 2.0", "zh_female_tvbnv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("译制片男 2.0", "zh_male_yizhipiannan_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("俏皮女声 2.0", "zh_female_qiaopinv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("邻家男孩 2.0", "zh_male_linjiananhai_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("儒雅青年 2.0", "zh_male_ruyaqingnian_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("温暖阿虎/Alvin 2.0", "zh_male_wennuanahu_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("奶气萌娃 2.0", "zh_male_naiqimengwa_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("婆婆 2.0", "zh_female_popo_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("高冷御姐 2.0", "zh_female_gaolengyujie_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("傲娇霸总 2.0", "zh_male_aojiaobazong_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("反卷青年 2.0", "zh_male_fanjuanqingnian_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("温柔淑女 2.0", "zh_female_wenroushunv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("活力小哥 2.0", "zh_male_huolixiaoge_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("萌丫头/Cutey 2.0", "zh_female_mengyatou_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("贴心女声/Candy 2.0", "zh_female_tiexinnvsheng_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("鸡汤妹妹/Hope 2.0", "zh_female_jitangmei_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("磁性解说男声/Morgan 2.0", "zh_male_cixingjieshuonan_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("亮嗓萌仔 2.0", "zh_male_liangsangmengzai_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("开朗姐姐 2.0", "zh_female_kailangjiejie_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("高冷沉稳 2.0", "zh_male_gaolengchenwen_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("深夜播客 2.0", "zh_male_shenyeboke_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("娇喘女声 2.0", "zh_female_jiaochuannv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("开朗弟弟 2.0", "zh_male_kailangdidi_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("谄媚女声 2.0", "zh_female_chanmeinv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("亲切女声 2.0", "zh_female_qinqienv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("快乐小东 2.0", "zh_male_kuailexiaodong_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("开朗学长 2.0", "zh_male_kailangxuezhang_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("悠悠君子 2.0", "zh_male_youyoujunzi_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("文静毛毛 2.0", "zh_female_wenjingmaomao_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("知性女声 2.0", "zh_female_zhixingnv_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("清爽男大 2.0", "zh_male_qingshuangnanda_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("渊博小叔 2.0", "zh_male_yuanboxiaoshu_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("阳光青年 2.0", "zh_male_yangguangqingnian_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("清澈梓梓 2.0", "zh_female_qingchezizi_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("甜美悦悦 2.0", "zh_female_tianmeiyueyue_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("心灵鸡汤 2.0", "zh_female_xinlingjitang_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("温柔小哥 2.0", "zh_male_wenrouxiaoge_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("柔美女友 2.0", "zh_female_roumeinvyou_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("东方浩然 2.0", "zh_male_dongfanghaoran_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("温柔小雅 2.0", "zh_female_wenrouxiaoya_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("天才童声 2.0", "zh_male_tiancaitongsheng_uranus_bigtts", "中文", "通用场景"),
new SpeakerOption("广告解说 2.0", "zh_male_guanggaojieshuo_uranus_bigtts", "中文", "通用场景"),

        ];

        return speakerList;
    }
}
