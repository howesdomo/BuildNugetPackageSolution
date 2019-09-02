using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFBuildNugetPackage
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Util.BuildNugetPackage.BuildNugetFactory mBuildNugetPackageFactory { get; set; }

        System.ComponentModel.BackgroundWorker mBgWorker { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.Title = $"{this.Title} - V {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            this.mBuildNugetPackageFactory = new Util.BuildNugetPackage.BuildNugetFactory();

            initEvent();
            initData();
        }

        private void initEvent()
        {
            btnBuild.Click += btnBuild_Click;

            ucsfCSharpProject.SuccessEventHandler += new EventHandler<EventArgs>(ucsfCSharpProject_OnSuccess);
        }

        string mConfigJsonPath = System.IO.Path.Combine(Environment.CurrentDirectory, "BuildNugetPackageCompileEnvironmentConfig.json");

        private void initData()
        {
            #region 创建 cbBuildMode 数据

            List<string> buildModeList = new List<string>();
            buildModeList.Add("Debug");
            buildModeList.Add("Release");

            cbBuildMode.ItemsSource = buildModeList;
            cbBuildMode.SelectedItem = buildModeList[1];

            #endregion

            if (System.IO.File.Exists(mConfigJsonPath) == false)
            {
                return;
            }

            string json = System.IO.File.ReadAllText(mConfigJsonPath);

            try
            {
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Util.BuildNugetPackage.BuildNugetPackageCompileEnvironmentConfig>(json);

                ucsfMsbuild.SetFileName(config.Msbuild);
                ucsfNuget.SetFileName(config.Nuget);

                checkCompileEnvironment();
            }
            catch (Exception ex)
            {
                ucConsole.Add(new Util.Model.ConsoleData($"读取配置发生异常\r\n配置文件路径:[{mConfigJsonPath}]\r\n{ex.Message}", Util.Model.ConsoleMsgType.ERROR));
            }
        }

        private void saveConfig()
        {
            var config = new Util.BuildNugetPackage.BuildNugetPackageCompileEnvironmentConfig()
            {
                Msbuild = ucsfMsbuild.FileName,
                Nuget = ucsfNuget.FileName
            };

            string json = this.SerializeObjectWithFormatted(config);
            System.IO.File.WriteAllText(mConfigJsonPath, json);
        }

        public string SerializeObjectWithFormatted(object o)
        {
            System.IO.StringWriter textWriter = new System.IO.StringWriter();
            Newtonsoft.Json.JsonTextWriter jsonWriter = new Newtonsoft.Json.JsonTextWriter(textWriter)
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Serialize(jsonWriter, o);
            return textWriter.ToString();
        }

        private Tuple<bool, string> checkCompileEnvironment()
        {
            if (ucsfMsbuild.IsSuccess == false)
            {
                return new Tuple<bool, string>(false, "请选择MsBuild");
            }

            if (ucsfNuget.IsSuccess == false)
            {
                return new Tuple<bool, string>(false, "请选择Jarsigner");
            }

            this.buildApkFactorySetConfig();
            this.saveConfig();

            return new Tuple<bool, string>(true, string.Empty);
        }

        private void buildApkFactorySetConfig()
        {
            mBuildNugetPackageFactory.SetMsbuild(ucsfMsbuild.FileName);
            mBuildNugetPackageFactory.SetNuget(ucsfNuget.FileName);
        }

        private void ucsfCSharpProject_OnSuccess(object sender, EventArgs args)
        {
            string filePath = mBuildNugetPackageFactory.GetDefaultNuspecFilePath(this.ucsfCSharpProject.FileName);
            ucsfNuspec.SetFileName(filePath);
        }

        #region 实际

        private void btnBuild_Click(object sender, RoutedEventArgs e)
        {
            if (mBgWorker != null && mBgWorker.IsBusy == true)
            {
                return;
            }

            Tuple<bool, string> check1 = checkCompileEnvironment();
            if (check1.Item1 == false)
            {
                ucConsole.Add(new Util.Model.ConsoleData(check1.Item2, Util.Model.ConsoleMsgType.WARNING));
                return;
            }

            if (ucsfCSharpProject.IsSuccess == false)
            {
                ucConsole.Add(new Util.Model.ConsoleData("请选择CSharp项目csproj文件", Util.Model.ConsoleMsgType.WARNING));
                return;
            }

            try
            {
                ucConsole.Add(new Util.Model.ConsoleData("开始生成。请稍候...", Util.Model.ConsoleMsgType.DEFAULT));
                ucConsole.Add(new Util.Model.ConsoleData($"CreateNugetPackage({ucsfCSharpProject.FileName})", Util.Model.ConsoleMsgType.DEBUG));

                mBgWorker = new System.ComponentModel.BackgroundWorker();
                mBgWorker.DoWork += (bgSender, bgArgs) =>
                {
                    object[] objArr = bgArgs.Argument as object[];
                    mBuildNugetPackageFactory.CreateNugetPackage
                    (
                        csprojFilePath: objArr[0] as string,
                        version: objArr[1] as string,
                        buildMode: objArr[2] as string,
                        argsNuspecPath: objArr[3] as string
                    );

                    bgArgs.Result = mBuildNugetPackageFactory.GetOutputDirectory(objArr[0] as string);
                };

                mBgWorker.RunWorkerCompleted += (bgSender, bgArgs) =>
                {
                    ucWait.IsBusy = false;
                    if (bgArgs.Error != null)
                    {
                        ucConsole.Add(new Util.Model.ConsoleData(bgArgs.Error.Message, Util.Model.ConsoleMsgType.ERROR));
                    }
                    else
                    {
                        ucConsole.Add(new Util.Model.ConsoleData("创建 NugetPackage 成功.", Util.Model.ConsoleMsgType.INFO));

                        var openFolderPath = bgArgs.Result as string;
                        System.Diagnostics.Process.Start(openFolderPath); // 用资源管理器打开
                    }
                };

                ucWait.IsBusy = true;
                mBgWorker.RunWorkerAsync
                (
                    new object[]
                    {
                        ucsfCSharpProject.FileName,
                        txtVersionName.Text,
                        cbBuildMode.SelectedItem.ToString(),
                        ucsfNuspec.FileName
                    }
                 );
            }
            catch (Exception ex)
            {
                ucConsole.Add(new Util.Model.ConsoleData(ex.Message, Util.Model.ConsoleMsgType.ERROR));
            }
        }


        #endregion
    }
}
