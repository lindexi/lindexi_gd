# 先在 Docker 文件夹里面进行构建，获取到 ubuntu-2404-render:01 再发布本文件进行后续构建
# 分开两个步骤只是为了方便提升构建速度而已
FROM ubuntu-2404-render:01

# 以下为实际项目运行的配置
COPY ./ ./
RUN chmod +x ./AspServer
ENTRYPOINT ["./AspServer"]

# 发布命令：
# docker build -t t1 .
# 运行命令：
# docker run -p8080:8080 -it t1