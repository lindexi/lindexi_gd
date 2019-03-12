RSS简易信息聚合(也叫聚合内容)是一种RSS基于XML标准，在互联网上被广泛采用的内容包装和投递协议。RSS(Really Simple Syndication)是一种描述和同步网站内容的格式，是使用最广泛的XML应用。RSS搭建了信息迅速传播的一个技术平台，使得每个人都成为潜在的信息提供者。发布一个RSS文件后，这个RSS Feed中包含的信息就能直接被其他站点调用，而且由于这些数据都是标准的XML格式，所以也能在其他的终端和服务中使用，是一种描述和同步网站内容的格式。RSS可以是以下三个解释的其中一个: Really Simple Syndication;RDF (Resource Description Framework) Site Summary; Rich Site Summary。但其实这三个解释都是指同一种Syndication的技术。

今天在win10.me看到一个rss，不知道是什么东西，打开看到
![这里写图片描述](http://img.blog.csdn.net/20160222151447588)

于是在网上查了RSS，又在微软官网看到https://msdn.microsoft.com/zh-cn/library/windows/apps/mt429379.aspx

林政的书也有说过，不过他是用HttpWebRequest

我的rss是使用SyndicationClient
先创建SyndicationClient

```
            Windows.Web.Syndication.SyndicationClient client = new Windows.Web.Syndication.SyndicationClient();
            Windows.Web.Syndication.SyndicationFeed feed;
```

因为输URL可能是错的，所以微软就用try catch

```
            //uri写在外面，为了在try之外不会说找不到变量
            Uri uri = null;

            //uri字符串
            string uriString = "http://www.win10.me/?feed=rss2";

            try
            {
                uri = new Uri(uriString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
```
网络请求有很多异常，我们放在try 

```
            try
            {
                //模拟http 
                // 如果没有设置可能出错
                client.SetRequestHeader("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                feed = await client.RetrieveFeedAsync(uri);

                foreach (Windows.Web.Syndication.SyndicationItem item in feed.Items)
                {
                    displayCurrentItem(item);
                }

            }
            catch (Exception ex)
            {
                // Handle the exception here.
            }
```

我们写一个函数处理每个SyndicationItem

```
        private void displayCurrentItem(Windows.Web.Syndication.SyndicationItem item)
        {
            string itemTitle = item.Title == null ? "No title" : item.Title.Text;
            string itemLink = item.Links == null ? "No link" : item.Links.FirstOrDefault().ToString();
            string itemContent = item.Content == null ? "No content" : item.Content.Text;
            string itemSummary = item.Summary.Text + "";
            reminder = itemTitle + "\n" + itemLink + "\n" + itemContent+"\n"+itemSummary+"\n";

        }
```

reminder是通知显示，把每个不为空的值放在StringBuilder
![这里写图片描述](http://img.blog.csdn.net/20160222152926727)

看起来很多html，我们可以用WebUtility，Regex来得到文本

我们可以做一个显示标题，然后点击显示内容

建一个类rssstr，这个类存放rss标题和内容

在viewModel 一个列表`ObservableCollection<rssstr>`

界面MainPage

```
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid.RowDefinitions >
            <RowDefinition >

            </RowDefinition>
            <RowDefinition Height="auto"/>
        </Grid.RowDefinitions>
        <ScrollViewer Grid.Row="0"  VerticalScrollBarVisibility="Auto">
            <ListView SelectionChanged="select" ItemsSource="{x:Bind view.rsslist}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition/>
                            </Grid.RowDefinitions>
                            <TextBlock Text="{Binding title}"/>
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
            <!--<TextBlock Grid.Row="0" Text="{x:Bind view.reminder,Mode=OneWay}" TextWrapping="Wrap"/>-->
        </ScrollViewer>
        <Button Grid.Row="1" Margin="10,10,10,10" Content="确定" Click="Button_Click"/>
    </Grid>
```

新建一个页面rss_page

```
    <Page.Resources>
        <Style x:Key="TextBlockStyle1" TargetType="TextBlock">
            <Setter Property="Margin" Value="10,10,10,10"/>
        </Style>
    </Page.Resources>

    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="auto"/>
            <RowDefinition />
            <RowDefinition Height="auto"/>
        </Grid.RowDefinitions>
        <TextBlock Style="{StaticResource TextBlockStyle1}" Grid.Row="0" Text="{x:Bind view.title}"/>
        <TextBlock Style="{StaticResource TextBlockStyle1}" Grid.Row="1" Text="{x:Bind view.summary}"/>
        <Button Grid.Row="2" Content="确定" Click="Button_Click"/>
    </Grid>
```

在列表被点击

```
        private void select(object sender, SelectionChangedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(rss_page), (ViewModel.rssstr)(sender as ListView).SelectedItem);
        }
```

rss_page viewModel使用rssstr

```
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            view = e.Parameter as rssstr;
            base.OnNavigatedTo(e);
        }
```
![这里写图片描述](http://img.blog.csdn.net/20160222153520940)

![这里写图片描述](http://img.blog.csdn.net/20160222153545766)

rss_page不能滚动TextBlock，可以使用ScrollViewer

```
        <ScrollViewer Grid.Row="1">
            <TextBlock Style="{StaticResource TextBlockStyle1}" Grid.Row="1" Text="{x:Bind view.summary}" TextWrapping="Wrap"/>
        </ScrollViewer>
```

源代码
https://github.com/lindexi/lindexi_gd/tree/master/rss

链接：http://pan.baidu.com/s/1sk7v6Zr 密码：dzfa

Http://blog.csdn.net/lindexi_gd

![这里写图片描述](http://img.blog.csdn.net/20160222155811716)
