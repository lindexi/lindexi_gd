using System;
using System.IO;
using System.Linq;
using OpenMcdf;
using SetStartupProjects;

namespace HairhechallchujurKairbilairlem
{
    class Program
    {
        static void Main(string[] args)
        {
            var suo = new FileInfo(@"..\..\..\.vs\HairhechallchujurKairbilairlem\v16\.suo");

            var sln = new FileInfo(@"..\..\..\HairhechallchujurKairbilairlem.sln");

            var projectList = SetStartupProjects.SolutionProjectExtractor.GetAllProjectFiles(sln.FullName).ToList();
            var startProjectFinder = new StartProjectFinder();
            var startProjectList = startProjectFinder.GetStartProjects(sln.FullName).ToList();

            using (var fileStream = suo.Open(FileMode.Open))
            {
                using CompoundFile compoundFile = new CompoundFile(fileStream, CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
            }
           
        }
    }
}
