button有很多和wpf一样，可以看《深入浅出WPF》
button可以设置属性，使用资源
资源可以写在页面

```
    <Page.Resources>
        
    </Page.Resources>
```

所有按钮使用同样式

```
    <Page.Resources>
        <Style TargetType="Button">
            
        </Style>
    </Page.Resources>
```

按钮的属性用`<Setter Property="属性" Value="值"/>`

按钮的背景

```
    <Page.Resources>
        <Style TargetType="Button">
            <Setter Property="Background" Value="White"/>
        </Style>
    </Page.Resources>
```

指定一个样式，key

```
    <Page.Resources>
        <Style TargetType="Button">
            <Setter Property="Background" Value="White"/>
            <Setter Property="Width" Value="100"/>
            <Setter Property="Height" Value="100"/>
        </Style>
        <Style  x:Key="button" TargetType="Button">
            <Setter Property="Background" Value="White"/>
            <Setter Property="Width" Value="50"/>
            <Setter Property="Height" Value="50"/>
        </Style>
    </Page.Resources>
```

```
         <Button Content="默认"/>
         <Button Style="{StaticResource button}" Content="确定"/>
```

![这里写图片描述](image/20151211154753136.jpg)

移动到button显示文字

在装机必备移动到搜狐显示搜狐
参考：http://blog.csdn.net/lindexi_gd/article/details/50166161

```
                        <Button Click="souhu_Click" ToolTipService.ToolTip="搜狐视频" Padding="0" >
                            <Button.Content>
                                <Grid>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="auto"/>
                                        <RowDefinition Height="auto"/>
                                    </Grid.RowDefinitions>
                                    <Image Source="ms-appx:///Assets/搜狐.png" Grid.Row="0" ScrollViewer.VerticalScrollBarVisibility="Disabled" />
                                    <TextBlock Text="搜狐视频" Grid.Row="1" HorizontalAlignment="Center" />
                                </Grid>
                            </Button.Content>
                        </Button>
```

![这里写图片描述](image/20151211161126290.jpg)

显示图片

```
                        <Button Click="souhu_Click" Padding="0" >
                            <Button.Content>
                                <Grid>
                                    <Grid.RowDefinitions>
                                        <RowDefinition Height="auto"/>
                                        <RowDefinition Height="auto"/>
                                    </Grid.RowDefinitions>
                                    <Image Source="ms-appx:///Assets/搜狐.png" Grid.Row="0" ScrollViewer.VerticalScrollBarVisibility="Disabled" />
                                    <TextBlock Text="搜狐视频" Grid.Row="1" HorizontalAlignment="Center" />
                                </Grid>
                            </Button.Content>
                            <ToolTipService.ToolTip>
                                <Image Height="50" Width="50" Source="ms-appx:///Assets/搜狐.png"/>
                            </ToolTipService.ToolTip>
                        </Button>
```

