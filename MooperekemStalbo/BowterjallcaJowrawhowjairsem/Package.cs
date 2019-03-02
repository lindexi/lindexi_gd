namespace MooperekemStalbo
{
    public class Package
    {
        public string Name { get; set; }

        /// <summary>
        /// 包版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; }

        public string File { get; set; }

        /// <summary>
        /// 需要的最低版本
        /// </summary>
        public string RequirementMinVersion { get; set; }

        /// <summary>
        /// 需要的最高版本
        /// </summary>
        public string RequirementMaxVersion { get; set; }

        /// <summary>
        /// 系统
        /// </summary>
        public string System { get; set; }
    }
}