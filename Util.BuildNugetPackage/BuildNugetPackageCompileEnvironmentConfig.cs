using System;
using System.Collections.Generic;
using System.Text;

namespace Util.BuildNugetPackage
{
    public class BuildNugetPackageCompileEnvironmentConfig
    {
        public string Msbuild { get; set; } // = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe";

        public string Nuget { get; set; } // = @"C:\Java\jdk1.8.0_20_x64\bin\jarsigner.exe";
    }
}
