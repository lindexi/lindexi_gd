FROM ubuntu:24.04

USER root

RUN apt-get update \
    && apt-get install -y telnet net-tools lsof tcpdump strace iotop sysstat vim wget unzip tzdata gettext curl libarchive-tools \
    && cp -r -f /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo "Asia/Shanghai" > /etc/timezone \
    && apt-get clean \
    && apt-get autoclean

RUN apt-get install -y language-pack-zh-hans \
    && localedef -c -f UTF-8 -i zh_CN zh_CN.utf8

ENV LC_ALL zh_CN.utf8
ENV PS1 "\W # "

# 按照我的做法，以上代码就做了一个 base 的包，这里为了方便，就写到一个 dockerfile 文件里

# 安装依赖
RUN apt-get update && \
    apt-get install -y libgdiplus zlib1g apt-transport-https

# 安装核心运行时
# See https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install
RUN apt-get install -y aspnetcore-runtime-10.0

# Configure web servers to bind to port 8080 when present
# 无管理员权限运行 80 提示 System.Net.Sockets.SocketException (13): Permission denied https://github.com/dotnet/aspnetcore/issues/4699
ENV ASPNETCORE_URLS=http://+:8080 \
# Enable detection of running in a container
DOTNET_RUNNING_IN_CONTAINER=true

# 清理 apt 缓存
RUN apt-get clean

# 以上部分我也会作为一个独立的包，这就是带了 ASP.NET Core 运行环境的包。以下部分是在基础上添加了渲染的支持

# 安装虚拟 X11 环境
RUN apt-get install -y xvfb
ENV DISPLAY=:0

# 安装字体
RUN apt install -y fonts-ipafont
RUN apt install -y fonts-noto fonts-ubuntu fonts-roboto
RUN fc-cache -fv
# 以上方式安装的字体还是缺失很多的，最好还是从 Windows 机器上，将 C:\windows\fonts\ 文件夹里面的 ttf 和 ttc 字体文件全拷贝进去，以及从网上找到更多字体拷贝进去

# 多媒体渲染工具
RUN apt-get install -y libgdiplus
# 安装 Inkscape 可以带齐足够的包，于是就不用写太多了
RUN apt install -y inkscape

# 清理 apt 缓存
RUN apt-get clean



