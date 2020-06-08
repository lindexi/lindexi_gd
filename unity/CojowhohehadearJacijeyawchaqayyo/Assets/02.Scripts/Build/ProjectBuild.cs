using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ProjectBuild : Editor
{
   static void BuildPackage()
   {
        string[] levels = { @"Assets\01.Scenes\MainScene.unity" };
        string path = @"D:\lindexi\bin\output.exe";

        BuildPipeline.BuildPlayer(levels, path, BuildTarget.StandaloneWindows, BuildOptions.None);
   }
}

// "C:\Program Files\Unity\Hub\Editor\2019.3.12f1\Editor\Unity.exe"
// "C:\Program Files\Unity\Editor\Unity.exe" -batchmode   -projectPath "D:\lindexi\unity\CojowhohehadearJacijeyawchaqayyo"  -executeMethod ProjectBuild.BuildPackage -logFile "D:\lindexi\buildlog.txt"

