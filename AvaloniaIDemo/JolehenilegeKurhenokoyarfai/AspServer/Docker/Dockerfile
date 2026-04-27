FROM ubuntu:24.04

USER root

# 更换使用阿里的源，速度比较快 
COPY sources.list /etc/apt/sources.list

RUN apt-get update \
    && apt-get install -y telnet net-tools lsof tcpdump strace iotop sysstat vim wget unzip tzdata gettext curl libarchive-tools \
    && cp -r -f /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo "Asia/Shanghai" > /etc/timezone \
    && apt-get clean \
    && apt-get autoclean

# 设置语言。上面的 Asia 设置为了设置时区
RUN apt-get install -y language-pack-zh-hans \
    && localedef -c -f UTF-8 -i zh_CN zh_CN.utf8

# 安装依赖
RUN apt-get install -y libgdiplus zlib1g apt-transport-https

# 安装核心运行时
# See https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install
RUN apt-get install -y aspnetcore-runtime-10.0

# 如后续无管理员权限运行，使用 80 端口将提示：
# System.Net.Sockets.SocketException (13): Permission denied https://github.com/dotnet/aspnetcore/issues/4699
ENV ASPNETCORE_URLS=http://+:8080 \
# Enable detection of running in a container
DOTNET_RUNNING_IN_CONTAINER=true

# 清理 apt 缓存
RUN apt-get clean

# 以下为渲染相关的内容

# 安装虚拟 X11 环境
RUN apt-get install -y xvfb
ENV DISPLAY=:0

# 安装字体
RUN apt install -y fonts-ipafont
RUN apt install -y fonts-noto fonts-ubuntu fonts-roboto
RUN fc-cache -fv

# 多媒体渲染工具
# 安装 Inkscape 可以带齐足够的包，于是就不用写太多了。大家也可以取 Inkscape 的依赖去安装
RUN apt install -y inkscape

# docker build -t ubuntu-2404-render:01 .