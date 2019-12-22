using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class FriendsModel : PageModel
    {
        public void OnGet()
        {
            var str = @"
## [dotnet职业技术学院](https://dotnet-campus.github.io/)

 - [walterlv https://walterlv.com](https://walterlv.com ) 

   微软最具价值专家

 - [泰琪 https://gandalfliang.github.io/](https://gandalfliang.github.io/)

   对d3d有很深的研究

 - [晒太阳的猫 http://jasongrass.gitee.io/ ](http://jasongrass.gitee.io/ ) 

   WPF开发大神，现在转到[博客园](https://www.cnblogs.com/jasongrass/)

 - [黄腾霄 ](https://huangtengxiao.gitee.io/ )    

   头像大神，wpf大神，专业研究 WCF 相关

 - [唐宋元明清2188 - 博客园](https://www.cnblogs.com/kybs0 )
 
   余冬 前 IBM 工程师 

 - [niuyanjie's blog 一个菜鸟的成长历程](http://niuyanjie.gitee.io/blog/ ) 

   WPF开发大神

 - [Nero\`s Blog](http://erdao123.gitee.io/nero/ )  

   有很多单元测试博客，现在他主要在[简书](https://www.jianshu.com/u/e51c5543bb4b )

 - [Miss_Bread的博客 - CSDN博客](http://blog.csdn.net/miss_bread )

   我们组的女装程序员  

 - [付金祥](https://fujinxiang.gitee.io/ )
 
   驸马爷，一个做前端的wpf大神

 - [wolfleave ](https://wolfleave.github.io/ )

   传说的幸斌师兄

 - [Iron_Ye](https://blog.csdn.net/iron_ye )

   WPF 大佬，专业做动画和 3D 是 dotnet 职业技术学院的校长

 - [lsj](https://blog.sdlsj.net/ )

 - [胡承老司机](https://huchengv5.github.io/ )

 - [天龙](https://getandplay.github.io/ )

## 朋友

 - [王文平](https://tristawwp.github.io/ ) 不告诉你这是漂亮的小姐姐

 - [东邪独孤 - 博客园](http://www.cnblogs.com/tcjiaan/ ) 
   
   老周，国内开发 WPF 或 UWP 的小伙伴相信都认识，他的 《Windows 10 应用开发实战》 是 UWP 国内入门书籍最好的。我很多博客都是参考老周的。

 - [淹死的鱼](https://zhuanlan.zhihu.com/c_191585960 )

   他有国内唯一的 win2d 专栏，这是他的[github](https://github.com/ysdy44)

   布卡漫画 UWP 是他的作品

 - [李继龙 https://kljzndx.github.io/My-Blog/](https://kljzndx.github.io/My-Blog/)

   传说中的开心开发者

 - [陈浩翔 http://chenhaoxiang.cn/](http://chenhaoxiang.cn/ )

 - [Nullptr](http://blog.nullptr.top/ )  

 - [杨宇杰 https://okcthouder.github.io/](https://okcthouder.github.io/)

 - [编程新手的小站 http://17i648w554.iask.in/wordpress/](http://17i648w554.iask.in/wordpress/)

 - [极简天气UWP开发者博客 http://fionlen.azurewebsites.net/](http://fionlen.azurewebsites.net/)

 - [张高兴 http://www.cnblogs.com/zhanggaoxing/default.html](http://www.cnblogs.com/zhanggaoxing/default.html?page=1)

   UWP IOT 大神

 - [邵猛](https://www.cnblogs.com/shaomeng )  

   国内Windows应用开发方向的微软最具价值专家

 - [云乡](https://blog.richasy.cn/ ) 

   云之幻

 - [追梦园 http://www.zmy123.cn](http://www.zmy123.cn)

   里面有很多uwp的博客

 - [wblearn https://wblearn.github.io/](https://wblearn.github.io/)

 - [吾勇士 http://wuyongshi.top/pages/1479085886341.html](http://wuyongshi.top/pages/1479085886341.html)

 - [frendguo's blog  Talk is cheap, show me the code!!!](http://frendguo.top/ )

 - [Leonn https://liyuans.com](https://liyuans.com)

 - [(/≧▽≦/)咦！又好了！ Dreamwings – 继续踏上旅途，在没有你的春天……](https://www.dreamwings.cn/ )

   千千，一个可爱的蓝孩子

 - [老李拉面馆](http://wicrosoft.ml/ ) UWP 大神

 - [YOYOFx](http://blog.microservice4.net/  ) 

   Asp大神，研究.net core

 - [XNA(MonoGame)游戏开发 – 用C#开发跨平台游戏](https://www.xnadevelop.com/ )

   大王，不解释他是谁

 - [法的空间 - 博客园](http://www.cnblogs.com/FaDeKongJian )

   东方财富 UWP 开发者

 - [迪莫的小站](http://www.dimojang.com/ )  这是我见过可能最厉害的高中生

 - [五斤](https://www.chairyfish.com/ )

 - [小文's blog - 小文的个人博客](https://www.qcgzxw.cn/ )

 - [Moe](https://sunnycase.moe/ ) 

   WPF 大神
  
 - [火火](https://blog.ultrabluefire.cn/ ) UWP 大神

 - [LiesAuer](http://www.liesauer.net/blog/ )  

 - [陈计节的博客](https://blog.jijiechen.com/post/ ) 
 
   dotnet core 大神 

 - [一起帮](http://17bang.ren/ )  

   一对一远程互助平台，有什么问题可以在这里问

 - [码友网 最新.NET Core开发资讯源](https://codedefault.com/sn/latest )

   这是 dotnet 开发人员的头条

 - [我是卧底](https://www.songshizhao.com/blog/blogPage/507.html )  

 - [Mutuduxf's blog](https://www.mutuduxf.com/ )

 - [吴波](http://blog.vichamp.com/) Powershell 方向的 MVP 是一位技术+管理的大佬

 - [【xtgmd168的专栏】wpf3dgis - CSDN博客](https://blog.csdn.net/xtgmd168?tdsourcetag=s_pctim_aiomsg )

 - [Ulysses' Brain Holes](http://114.215.126.213/ )

 - [樊健汉](http://www.ikende.com/ ) .NET高性能开发

 - [逆流水Team 布墨](http://blog.niliushui.com/?tdsourcetag=s_pctim_aiomsg )

 - [validvoid](http://www.cnblogs.com/validvoid/ ) http://www.cnblogs.com/validvoid/ 

   大量Win2D博客

 - [纳边](https://github.com/HandyOrg/HandyControl) 

   超好用的 WPF 控件 [HandyControl](https://github.com/HandyOrg/HandyControl ) 的作者，也是 [HandyControl 群](https://jq.qq.com/?_wv=1027&k=5puiv55) 714704041 的群主

 - [三台](https://www.cnblogs.com/3Tai ) 这是他的 [github](https://github.com/a44281071)

 - [王政道](https://github.com/ZhengDaoWang)

 - 玩命夜狼 

   是在做 [The complete WPF tutorial](https://wpf-tutorial.com/zh/1/%E5%85%B3%E4%BA%8Ewpf/%E4%BB%80%E4%B9%88%E6%98%AFwpf/ ) 文档翻译的大佬，这部分文章很适合新手入门

 - [文轩](https://www.itoolsoft.org/ ) 微软 MVP 专业广告我软

 - [痕迹 - 博客园](https://www.cnblogs.com/zh7791 )

 - [dino.c](http://www.cnblogs.com/dino623/ ) 写了很多动画

 - [毛利的技术小站](https://mourinaruto.github.io/?file=Home ) C++ 大神研究 windows 原理

 - [.NET骚操作 - 博客园](https://www.cnblogs.com/sdflysha/ )

 - [Dotnet9 一个热衷于互联网分享精神的程序员网站](https://dotnet9.com/ )

## 国内博客

 - [【WinRT】国内外 Windows 应用商店应用开发者博客收集 - h82258652 - 博客园](http://www.cnblogs.com/h82258652/p/4909957.html)

 - [六兆煮橙不会写代码 - CSDN博客](http://blog.csdn.net/lzl1918 )

 - [怪咖先森的博客 - 博客频道 - CSDN.NET](http://blog.csdn.net/u011033906?viewmode=contents)

 - [☜ 我追求的天空 ☞┅┅┅┅┅﹣·☆ - CSDN博客](http://blog.csdn.net/xuzhongxuan )

 - [C_Li_的博客 - CSDN博客](http://blog.csdn.net/github_36704374?viewmode=contents )

 - [hystar - 博客园](http://www.cnblogs.com/lsxqw2004 )
 
 - [周永恒 - 博客园](http://www.cnblogs.com/Zhouyongh )

 - [MS-UAP - 博客园](http://www.cnblogs.com/ms-uap/ )

   微软Windows 工程院的团队博客 邹老师的团队，最近邹老师写了 《构建之法》 这本书介绍了产品和开发，适合各个小伙伴看

 - [MSP_甄心cherish](http://blog.csdn.net/zmq570235977 ) http://blog.csdn.net/zmq570235977 

   开始就是看甄心大神的博客。

 - [Fantasiax](http://blog.csdn.net/fantasiax ) http://blog.csdn.net/fantasiax 

   我的wpf就是在他博客学到的

 - [linzheng](http://www.cnblogs.com/linzheng/ ) http://www.cnblogs.com/linzheng/  

   我的wpf就是在他博客学到的

 - [码农很忙](http://www.sum16.com ) http://www.sum16.com 

 - [王陈染](http://www.wangchenran.com/ ) http://www.wangchenran.com/ 

   最早看到他发的UWP文章

 - zhxilin http://www.cnblogs.com/zhxilin/ 

   礼物说 App 作者。

 - [nomasp](http://blog.csdn.net/nomasp/ ) http://blog.csdn.net/nomasp/ 

   柯于旺大神，现在小米，没有继续做UWP，但他写了CSDN第一个UWP专栏

 - [叔叔的博客](http://www.cnblogs.com/manupstairs/ ) http://www.cnblogs.com/manupstairs/

 - http://blog.higan.me/ msp，二次元 

   写了很多瀑布流博客 

 - [msp的昌伟哥哥 ](http://www.cnblogs.com/mantgh )

   刚进微软的大神，他有好多HoloLens文章

 - [姜晔的技术专栏 - CSDN博客](http://blog.csdn.net/ioio_jy?viewmode=contents )

 - [E不小心 - 博客园](http://www.cnblogs.com/gaoshang212 )

 - [叛逆者](https://www.zhihu.com/people/minmin.gong/activities)

   Microsoft Senior SDE（微软高级软件工程师），KlayGE开源游戏引擎创始人

 - [durow - 博客园](http://www.cnblogs.com/durow/ )

 - [浅蓝 - 博客园](http://www.cnblogs.com/qianblue )

 - [youngytj - 博客园](http://www.cnblogs.com/youngytj )

 - [时嬴政 - 博客园](http://www.cnblogs.com/shiyingzheng )

 - [UWPBOX WIN10开发类容分享](http://uwpbox.com/ )   C++ uwp 大神

 - [bomo - 博客园](http://www.cnblogs.com/bomo )

 - [LH806732的专栏 - CSDN博客](http://blog.csdn.net/LH806732 )

 - [Berumotto - 博客园](http://www.cnblogs.com/KudouShinichi/ )

 - [Edi Wang](http://edi.wang/ ) 国内 .NET 的大神，有很多的文章，有 WPF 、 UWP 还有其他的 linux ……

 - [尽管扯淡](http://jameszhan.github.io/ ) 写了很多数学

 - [怪咖先森的博客 - CSDN博客](http://blog.csdn.net/u011033906 )

 - [丁校长 - 博客园](http://www.cnblogs.com/dingdaheng )

 - [珂珂的个人博客 - 一个程序猿的个人网站](http://kecq.com/)

 - [月江流](https://www.yuque.com/yuejiangliu/dotnet )

 - [vscode使用笔记 - 木杉的博客](http://mushanshitiancai.github.io/2017/01/07/tools/vscode%E4%BD%BF%E7%94%A8%E7%AC%94%E8%AE%B0/)

## 国外博客

 - [Josh Smith](https://joshsmithonwpf.wordpress.com/ )
 - [Dr. WPF](http://drwpf.com/blog/ )
 - http://xamlnative.com/
 - [jamescroft](http://jamescroft.co.uk)  我们在做WinUx，他是微软的大神
 - [Sam Beauvois Home page](http://www.sambeauvois.be/ )
 - [Dany Khalife](http://www.dkhalife.com/ )
 - [Diederik Krols  Short description of the blog](https://blogs.u2u.be/diederik )
 - [Frank's World of Data Science – Data Science for Developers](http://www.franksworld.com/ )
 - [Romasz.net](http://www.romasz.net/ ) wpf 的大神
 - [Jon Skeet's coding blog](https://codeblog.jonskeet.uk/ )
 - [CodeS-SourceS - CCM](http://codes-sources.commentcamarche.net/ )
 - [Igor Kulman](https://blog.kulman.sk/ )
 - http://juniperphoton.net/
 - [justinxinliu](https://stackoverflow.com/users/231837/justin-xl) 堆栈网25k大神
 - [Ben Cull's Blog](http://benjii.me/ ) asp 大神
 - [Kenny Kerr ](https://kennykerr.ca/ ) C++ WINRT 加拿大微软团队
 - [Sébastien Lachance](http://www.dotnetapp.com/?p=438 )
 - [Sonnemans](https://reflectionit.nl/blog ) trainer, speaker, developer, mentor   Microsoft MVP  C#, XAML, Windows (UWP), WPF, Blend, ASP.NET, SQL 
 - Alan Mendelevič   <https://blog.ailon.org/>
 - Andrei Marukovich  <http://lunarfrog.com/blog>
 - Angelo Luis  <http://xamarinbr.azurewebsites.net/>
 - Bart Lannoeye   <http://www.bartlannoeye.com/blog>
 - Brian Lagunas   <https://www.brianlagunas.com>
 - Brian Noyes  <http://briannoyes.net/>
 - Bruno Sonnino   <http://blogs.msmvps.com/bsonnino/>
 - Caio Proiete <http://caioproiete.net/en>
 - Christian Nagel <https://csharp.christiannagel.com/>
 - Christopher Maneu  <http://deezer.io>
 - Christopher Maneu  <http://log.maneu.net>
 - Dachi Gogotchuri   <https://geekytheory.com/>
 - Dachi Gogotchuri   <http://modernuidoc.com/>
 - Dan Ciprian Ardelean  <http://sviluppomobile.blogpost.com>
 - Daniel Vaughan  <http://danielvaughan.org>
 - David J. Kelley <http://www.Transhumanity.net/>
 - Diederik Krols  <http://Xamlbrewer.wordpress.com>
 - Dwight Goins <http://dgoins.wordpress.com>
 - Engin Polat  <http://www.enginpolat.com>
 - Giancarlo Lelli <http://www.aspitalia.com/ricerca/super.aspx?action=author&key=Giancarlo+Lelli>
 - Glenn Versweyveld  <http://depblog.weblogs.us/>
 - Ignat Andrei <http://msprogrammer.serviciipeweb.ro/>
 - Ivan Toledo  <http://www.birdie.cl/blog/>
 - James Croft  <http://www.jamescroft.co.uk/blog>
 - Jan Hannemann   <http://janhannemann.wordpress.com>
 - Javier Suárez Ruiz <https://javiersuarezruiz.wordpress.com>
 - Jeremy Likness  <http://csharperimage.jeremylikness.com/>
 - Jesse Liberty   <http://Jesseliberty.com>
 - Jessica Engström   <https://www.catoholic.se>
 - Jimmy Engström  <https://www.apeoholic.se>
 - Jonathan ANTOINE   <http://jonathanantoine.com>
 - Joost van Schaik   <http://dotnetbyexample.blogspot.com>
 - Lance McCarthy  <https://winplatform.wordpress.com>
 - Laurent   <http://blog.galasoft.ch>
 - Magnus Montin   <http://blog.magnusmontin.net>
 - Marco Minerva   <http://marcominerva.wordpress.com>
 - Mark Schramm <http://blog.markbschramm.com>
 - Matt Lacey   <http://mrlacey.com/>
 - Matteo Tumiati  <http://aspitalia.com/>
 - Mikael Koskinen <http://mikaelkoskinen.net/>
 - Morten Nielsen  <https://www.sharpgis.net>
 - Nick Prühs   <http://npruehs.de>
 - Nick Randolph   <http://nicksnettravels.builttoroam.com>
 - Nico Vermeir <http://www.spikie.be>
 - Nigel Sampson   <http://compiledexperience.com>
 - Olivier Dahan   <http://www.e-naxos.com/blog>
 - Oren Novotny <https://oren.codes>
 - Patrick Getzmann   <https://patrickgetzmann.wordpress.com/>
 - Peter Foot   <http://peterfoot.net>
 - Rene Schulte <http://blog.rene-schulte.info>
 - Rob Irving   <https://www.robwirving.com>
 - Rodrigo Díaz Concha   <http://rdiazconcha.com>
 - Rudy Huyn <http://rudyhuyn.com>
 - Russel Fustino  <http://russtoolshed.net/blog/>
 - Santiago Porras Rodríguez   <http://geeks.ms/santypr>
 - Sascha Wolter   <http://wolter.biz>
 - Scott Lovegrove <https://www.metronuggets.com>
 - Sébastien Lachance <http://dotnetapp.com>
 - Senthil Kumar   <http://DeveloperPublish.com>
 - Shawn Kendrot   <http://visuallylocated.com/>
 - Subramanyam Balaraju  <http://bsubramanyamraju.blogspot.com>
 - Tamás Deme   <http://shoreparty.org>
 - Thomas Claudius <https://huber.com>
 - <http://www.thomasclaudiushuber.com/blog>
 - Tom Walker   <http://tomwalker.codes>
 - Tony Champion   <http://tonychampion.net/blog>
 - Velvárt, András <http://vbandi.net>
 - Vicente Gerardo Guzman Lucio   <https://vicenteguzman.mx/>
 - Mark Arteaga    <https://www.redbitdev.com/blog>
 - Alex Sorokoletov   <http://sorokoletov.com>
 - Mark MacDonnell <https://www.markmacdonnell.ca>
 - [XAML Brewer, by Diederik Krols](https://xamlbrewer.wordpress.com/ ) 

## 老师

- [玉兰老师](http://blog.sina.com.cn/yulan2014 ) 现在主要是微信公众号 m17771713041 欢迎关注";

            HtmlContent = new MarkupString(Markdig.Markdown.ToHtml(str));
        }

        public MarkupString HtmlContent { get; private set; }

    }
}