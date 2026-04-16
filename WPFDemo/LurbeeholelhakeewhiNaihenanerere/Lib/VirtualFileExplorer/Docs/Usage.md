# VirtualFileExplorer 使用说明

## 目标

本文档说明如何在宿主 WPF 项目中快速接入 `VirtualFileExplorer`，以及如何在默认能力之上进行界面定制。

## 基础使用

### 1. 准备文件管理器

最基础的接入方式是构造一个 `VirtualFileManager` 实例，并赋值给 `FileExplorerUserControl.FileManager`。

如果希望直接映射物理目录，可以使用 `PhysicalFileManager`。

```csharp
using VirtualFileExplorer.Core.PhysicalFileManagers;

var fileManager = new PhysicalFileManager(@"C:\DemoFiles", "演示目录");
FileExplorerUserControl.FileManager = fileManager;
```

### 2. 在 XAML 中放入控件

```xaml
<Window x:Class="DemoApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="clr-namespace:VirtualFileExplorer.Views;assembly=VirtualFileExplorer">
    <Grid>
        <views:FileExplorerUserControl x:Name="FileExplorerUserControl" />
    </Grid>
</Window>
```

### 3. 默认能力

控件在默认状态下提供以下能力：

- 面包屑导航
- 返回上级
- 刷新
- 平铺模式与详细模式切换
- 搜索、筛选、排序
- 新建文件夹、重命名、复制、移动、删除
- 文件夹目标选择对话框
- 默认浅色主题样式

## 定制使用

### 1. 替换平铺模式模板

控件支持分别替换文件和文件夹的平铺模板。

```xaml
<Window.Resources>
    <DataTemplate x:Key="CustomFileTileTemplate">
        <Border Style="{StaticResource ExplorerTileCardStyle}">
            <StackPanel>
                <TextBlock FontFamily="Segoe Fluent Icons"
                           FontSize="26"
                           Foreground="{StaticResource ExplorerAccentBrush}"
                           Text="{Binding IconGlyph}" />
                <TextBlock Margin="0,12,0,0"
                           FontWeight="SemiBold"
                           Text="{Binding Name}"
                           TextWrapping="Wrap"
                           TextTrimming="CharacterEllipsis"
                           MaxLines="2" />
                <TextBlock Margin="0,8,0,0"
                           Foreground="{StaticResource ExplorerMutedTextBrush}"
                           Text="{Binding DisplayType}" />
            </StackPanel>
        </Border>
    </DataTemplate>
</Window.Resources>

<views:FileExplorerUserControl x:Name="FileExplorerUserControl"
                               TileFileTemplate="{StaticResource CustomFileTileTemplate}" />
```

如果希望文件夹也使用宿主自己的模板，可以继续设置 `TileFolderTemplate`。

### 2. 替换导航区域

控件默认使用 `BreadcrumbBarControl`。如果宿主希望替换导航区域，可以通过 `NavigationContent` 提供自定义内容。

```xaml
<views:FileExplorerUserControl>
    <views:FileExplorerUserControl.NavigationContent>
        <Border Padding="12" Background="#FFF5F8FD" CornerRadius="12">
            <TextBlock Text="这里可以替换为宿主自己的导航区域" />
        </Border>
    </views:FileExplorerUserControl.NavigationContent>
</views:FileExplorerUserControl>
```

### 3. 替换工具栏区域

如果宿主需要完全接管顶部操作区，可以设置 `ToolBarContent`。

```xaml
<views:FileExplorerUserControl>
    <views:FileExplorerUserControl.ToolBarContent>
        <TextBlock VerticalAlignment="Center" Text="自定义工具栏" />
    </views:FileExplorerUserControl.ToolBarContent>
</views:FileExplorerUserControl>
```

### 4. 替换空状态区域

当当前目录没有可显示项目时，可以替换默认空状态内容。

```xaml
<views:FileExplorerUserControl>
    <views:FileExplorerUserControl.EmptyContent>
        <Border Padding="24" Background="#FFF8FBFF" CornerRadius="16">
            <StackPanel>
                <TextBlock FontSize="18" FontWeight="SemiBold" Text="当前目录为空" />
                <TextBlock Margin="0,8,0,0" Text="请创建文件夹或切换到其他目录。" />
            </StackPanel>
        </Border>
    </views:FileExplorerUserControl.EmptyContent>
</views:FileExplorerUserControl>
```

## 使用建议

### 1. 平铺模板建议

默认平铺项已经固定为圆角等宽高卡片。如果宿主自定义模板，建议继续保持固定宽高，并对名称使用以下策略：

- 开启 `TextWrapping="Wrap"`
- 设置 `MaxLines="2"`
- 设置 `TextTrimming="CharacterEllipsis"`

这样可以避免长名称撑开卡片尺寸。

### 2. 详细模式建议

详细模式基于 `ListView + GridView`，适合需要列宽拖拽、排序感知和多选的场景。若宿主需要自定义项样式，建议不要直接替换掉 `GridViewRowPresenter`，否则容易导致列内容显示异常。

### 3. 物理文件系统使用建议

如果使用 `PhysicalFileManager`：

- 根目录会被限制在传入目录范围内。
- 复制、移动、删除等操作不会越出根目录。
- 示例应用中的演示方式可作为最小接入参考。

## 相关文档

- `Docs/Requirements.md`
- `Docs/Design.md`
- `Docs/CodeReviewAndUpdate.md`
- `Docs/WpfMvvmBestPractices.md`
