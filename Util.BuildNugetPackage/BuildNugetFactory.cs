using System;
using System.IO;

namespace Util.BuildNugetPackage
{
    public class BuildNugetFactory
    {
        #region Nuget 环境配置路径

        string msbuild { get; set; }

        string nuget { get; set; }

        public void SetMsbuild(string path)
        {
            this.msbuild = path;
        }

        public void SetNuget(string path)
        {
            this.nuget = path;
        }

        #endregion

        /// <summary>
        /// 生成 Nuget Package
        /// </summary>
        /// <param name="csprojFilePath">.csproj 文件路径</param>
        /// <param name="version">Nuget Package 版本号</param>
        /// <param name="argsNuspecPath">用户自定义 nuspec 配置文件</param>
        public void CreateNugetPackage(string csprojFilePath, string version, string buildMode, string argsNuspecPath = "")
        {
            string cleanArgs = $"{csprojFilePath} -target:Clean -property:Configuration={buildMode}";
            string rebuildArgs = $"{csprojFilePath} -target:Rebuild -property:Configuration={buildMode}";

            Tuple<string,string> nugetTupleResult = getNugetArgs(csprojFilePath, version, buildMode, argsNuspecPath);
            string nugetArgs = nugetTupleResult.Item1;
            string nugetPackageFilePath = nugetTupleResult.Item2;

            RunProcess(msbuild, cleanArgs);
            System.Diagnostics.Debug.WriteLine($"Clean is done");

            RunProcess(nuget, nugetArgs);
            System.Diagnostics.Debug.WriteLine($"Nuget is done");

            if (System.IO.File.Exists(nugetPackageFilePath) == false)
            {
                throw new Exception($"生成失败。文件不存在：{nugetPackageFilePath}");
            }
        }

        /// <summary>
        /// 获取 Nuget 打包参数
        /// </summary>
        /// <param name="csprojFilePath">.csproj 文件路径</param>
        /// <param name="version">Nuget Package 版本号</param>
        /// <param name="argsNuspecPath">用户自定义 nuspec 配置文件</param>
        /// <returns></returns>
        private Tuple<string, string> getNugetArgs(string csprojFilePath, string version, string buildMode, string argsNuspecPath)
        {
            FileInfo fileInfo_csproj = new System.IO.FileInfo(csprojFilePath);

            string nuspecPath = string.Empty;
            if (string.IsNullOrWhiteSpace(argsNuspecPath) == false)
            {
                nuspecPath = argsNuspecPath;
            }
            else
            {
                nuspecPath = this.GetDefaultNuspecFilePath(csprojFilePath);
            }

            if (System.IO.File.Exists(nuspecPath) == false)
            {
                throw new Exception($"找不到 Nuget 配置文件。 路径:{nuspecPath}");
            }

            // string outputDirectory = System.IO.Path.Combine(csProjFolder, "bin", "NugetPackage");
            string outputDirectory = this.GetOutputDirectory(csprojFilePath);

            // r1 执行参数
            string r1 = $"pack \"{csprojFilePath}\" -BasePath \"{nuspecPath}\" -Version \"{version}\" -OutputDirectory \"{outputDirectory}\" -Build -Properties \"Configuration={buildMode}\"";

            // r2 检测生成文件
            string r2 = System.IO.Path.Combine(outputDirectory, $"{fileInfo_csproj.NameWithoutExtension()}.{version}.nupkg");

            return new Tuple<string, string>(r1, r2);
        }

        /// <summary>
        /// 获取生成目录路径
        /// </summary>
        /// <param name="csprojFilePath"></param>
        /// <returns></returns>
        public string GetOutputDirectory(string csprojFilePath)
        {
            FileInfo fileInfo_csproj = new System.IO.FileInfo(csprojFilePath);
            string csProjFolder = fileInfo_csproj.Directory.FullName;
            string outputDirectory = System.IO.Path.Combine(csProjFolder, "bin", "NugetPackage");
            return outputDirectory;
        }

        /// <summary>
        /// 获取 nuspec 配置文件默认路径
        /// </summary>
        /// <param name="csprojFilePath">.csproj 文件路径</param>
        /// <returns></returns>
        public string GetDefaultNuspecFilePath(string csprojFilePath)
        {
            FileInfo fileInfo_csproj = new System.IO.FileInfo(csprojFilePath);

            string csProjFolder = fileInfo_csproj.Directory.FullName;
            string nuspecPath = System.IO.Path.Combine(csProjFolder, $"{fileInfo_csproj.NameWithoutExtension()}.nuspec");
            return nuspecPath;
        }

        void RunProcess(string filename, string arguments)
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = arguments;
            process.Start();
            process.WaitForExit();
        }
    }
}
