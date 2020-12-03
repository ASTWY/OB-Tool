using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace OB_Tool
{
    class Program
    {
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        public static string ini_path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\cfg.ini";
        public static string ico_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\astwy\\rofl_ico.png";
        public static string ver = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName).FileVersion;
        public static string up_url = "https://cdn.jsdelivr.net/gh/ASTWY/OB-Tool@master/OB-Tool/OB-Tool/Resources/update.json";

        static void Main(string[] args)
        {
            if (!File.Exists(ini_path))
            {
                //Reg(".rofl", "astwy", Process.GetCurrentProcess().MainModule.FileName);
                IniHelper.ini_creat(ini_path);
                IniHelper.Ini_Write("config", "Checkupdate", "1", ini_path);
                IniHelper.Ini_Write("Locale", "Default", "zh_CN", ini_path);
                IniHelper.Ini_Write("Client", "11.10", "astwyigzy", ini_path);
                IniHelper.Ini_Del_Key("Client", "11.10", ini_path);
                if (File.Exists(ico_path))
                {
                    File.Delete(ico_path);
                }
                FileUtil.ExtractResFile("OB_Tool.Resources.rofl_ico", ico_path);
                Reg(".rofl", ico_path, "astwy", Process.GetCurrentProcess().MainModule.FileName);
            }
            else
            {
                if (IniHelper.Ini_Read("config", "Checkupdate", ini_path) == "")
                {
                    File.Delete(ini_path);
                    IniHelper.ini_creat(ini_path);
                    IniHelper.Ini_Write("config", "Checkupdate", "1", ini_path);
                    IniHelper.Ini_Write("Locale", "Default", "zh_CN", ini_path);
                    IniHelper.Ini_Write("Client", "11.10", "astwyigzy", ini_path);
                    IniHelper.Ini_Del_Key("Client", "11.10", ini_path);
                }
            }

            if (IniHelper.Ini_Read("config", "Checkupdate", ini_path) == "1")
            {
                UpdateInfo update = Check(up_url);
                if (update.version != ver)
                {
                    MessageBox.Show("请再更新后使用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    Environment.Exit(0);
                }
            }

            if (!IsAdministrator())
            {
                MessageBox.Show("请赋予本软件管理员权限后使用", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }



            if (args.Length == 1)
            {
                FileInfo file = new FileInfo(args[0]);
                if (file.Name == "League of Legends.exe")
                {
                    string ver = FileVersionInfo.GetVersionInfo(file.FullName).FileVersion.Substring(0, 8);
                    IniHelper.Ini_Write("Client", ver, file.DirectoryName, ini_path);
                    MessageBox.Show($"{ver}客户端添加成功", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (file.Extension == ".rofl")
                {
                    string ver = GetRoflVersion(file.FullName);
                    if (ver != "")
                    {
                        string exepath = IniHelper.Ini_Read("Client", ver, ini_path);
                        if (exepath != "")
                        {
                            if (FileVersionInfo.GetVersionInfo(exepath + "\\League of Legends.exe").FileVersion.Substring(0, 8) != ver)
                            {
                                IniHelper.Ini_Del_Key("Client", ver, ini_path);
                                IniHelper.Ini_Write("Client", FileVersionInfo.GetVersionInfo(exepath + "\\League of Legends.exe").FileVersion.Substring(0, 8), exepath, ini_path);
                                MessageBox.Show($"无法匹配{ver}客户端，请在添加{ver}客户端后重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            string loca = IniHelper.Ini_Read("LOCALE", "Default", ini_path);
                            string argc = $"\"{file.FullName}\" ";//"-Locale={loca}";
                            if (argc != "")
                            {
                                if (File.Exists(exepath + $"\\DATA\\FINAL\\Champions\\Yasuo.{loca}.wad.client"))
                                {
                                    argc += $"-Locale={loca}";
                                }
                                else
                                {
                                    argc += $"-Locale=en_US";
                                }
                            }
                            else
                            {
                                argc += $"-Locale=en_US";
                            }
                            ProcessStartInfo info = new ProcessStartInfo();
                            info.UseShellExecute = true;
                            info.FileName = exepath + "\\League of Legends.exe";
                            info.WorkingDirectory = exepath;
                            info.Arguments = argc;
                            Process.Start(info);
                        }
                        else
                        {
                            MessageBox.Show($"无法匹配{ver}客户端，请在添加{ver}客户端后重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            //WindowsBuiltInRole可以枚举出很多权限，例如系统用户、User、Guest等等
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static UpdateInfo Check(string url)
        {
            UpdateInfo info = new UpdateInfo();
            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于向Internet资源的请求进行身份验证的网络凭据
                Byte[] pageData = MyWebClient.DownloadData(url); //从指定网站下载数据
                //string pageHtml = Encoding.Default.GetString(pageData);  //如果获取网站页面采用的是GB2312，则使用这句            
                string pageHtml = Encoding.UTF8.GetString(pageData).Replace("\n", ""); //如果获取网站页面采用的是UTF-8，则使用这句
                info = JsonConvert.DeserializeObject<UpdateInfo>(pageHtml);
            }
            catch (WebException webEx) { }
            return info;
        }
        private static void Reg(string strExtension, string ico, string strProject, string strExePath)
        {
            Registry.ClassesRoot.CreateSubKey(strExtension).SetValue("", strProject, RegistryValueKind.String);
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("exefile"))
            {
                key.CreateSubKey(@"Shell\添加到ROFL小工具\Command").SetValue("", strExePath + " \"%1\"", RegistryValueKind.ExpandString);
                key.CreateSubKey(@"Shell\添加到ROFL小工具").SetValue("AppliesTo", "League of Legends", RegistryValueKind.ExpandString);
                key.CreateSubKey(@"Shell\添加到ROFL小工具").SetValue("icon", strExePath, RegistryValueKind.ExpandString);
            }
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(strProject))
            {
                //string strExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                //strExePath = Path.GetDirectoryName(strExePath);
                //strExePath += "\\ROFL_test.exe";
                key.CreateSubKey("").SetValue("", "英雄联盟回放文件", RegistryValueKind.String);
                key.CreateSubKey("DefaultIcon").SetValue("", ico, RegistryValueKind.String);
                key.CreateSubKey(@"Shell\Open\Command").SetValue("", strExePath + " \"%1\"", RegistryValueKind.ExpandString);
                try
                {
                    SHChangeNotify(0x8000000, 0, IntPtr.Zero, IntPtr.Zero);
                    //SHChangeNotify(0x8000000, 0, IntPtr.Zero, IntPtr.Zero);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public static string GetRoflVersion(string roflpath)
        {
            string result = "";
            if (File.Exists(roflpath))
            {
                try
                {
                    StreamReader sr = new StreamReader(roflpath, Encoding.UTF8);
                    for (int i = 0; ; i++)
                    {
                        string a = sr.ReadLine();
                        if (a.Contains("gameLength"))//"\"8.13.235.3406"
                        {
                            string[] b = a.Split(new string[] { "gameVersion" + "\":", "\",\"" }, StringSplitOptions.RemoveEmptyEntries);
                            //Console.WriteLine(b[1].Replace("\"", "").Substring(0, 4));
                            result = b[1].Replace("\"", "").Substring(0, 8);
                            sr.Close();
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            return result;
        }
    }
}
