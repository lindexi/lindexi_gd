using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

            using (var fileStream = suo.Open(FileMode.Open))
            {
                CompoundFile compoundFile = new CompoundFile(fileStream, CFSUpdateMode.ReadOnly, CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors);
                var cfStream = compoundFile.RootStorage.GetStream("SolutionConfiguration");
                var byteList = cfStream.GetData();
                var encoding = Encoding.GetEncodings()
                    .Single(x => string.Equals(x.Name, "utf-16", StringComparison.OrdinalIgnoreCase));
                var text = encoding.GetEncoding().GetString(byteList);

                const char nul = '\u0000';
                const char dc1 = '\u0011';
                const char etx = '\u0003';
                const char soh = '\u0001';

                var regex = new Regex(@$"{dc1}{nul}MultiStartupProj{nul}={etx}[{nul}{soh}]{nul};4{nul}(.{{38}}).dwStartupOpt");
                var match = regex.Match(text);

                if (match.Success)
                {
                    var guid = Guid.Parse(match.Groups[1].Value);
                    var project = projectList.FirstOrDefault(temp => new Guid(temp.Guid) == guid);

                }

                var startupProjectRegex = new Regex(@$"StartupProject{nul}={'\b'}&{nul}(.{{38}});A");
                var startupProjectMatch = startupProjectRegex.Match(text);
                if (startupProjectMatch.Success)
                {
                    var guid = Guid.Parse(startupProjectMatch.Groups[1].Value);
                    var project = projectList.FirstOrDefault(temp => new Guid(temp.Guid) == guid);
                }
            }

        }
    }
}
