using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MooperekemStalbo
{
    public class GairKetemRairsem
    {
        public long Id { get; set; }

        /// <summary>
        /// 包的名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 包版本
        /// </summary>
        public string Version { get; set; }

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

        /// <summary>
        /// 文件
        /// </summary>
        public long File { get; set; }
    }
}
