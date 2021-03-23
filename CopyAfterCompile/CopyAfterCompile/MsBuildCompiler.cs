using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.Configurations;
using dotnetCampus.DotNETBuild;
using dotnetCampus.DotNETBuild.Utils;

namespace CopyAfterCompile
{
    /// <summary>
    /// 编译器
    /// </summary>
    internal class MsBuildCompiler : Compiler, ICompiler
    {
        /// <inheritdoc />
        public MsBuildCompiler(IAppConfigurator appConfigurator) : base(appConfigurator)
        {
            
        }

        /// <inheritdoc />
        public override void Compile()
        {
            WriteLog($"开始编译");
            Nuget.Restore();
            MsBuild.Build();
        }
    }
}