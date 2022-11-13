using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static System.Net.WebRequestMethods;
using System.Threading;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;

class MinecraftPathGetter {
    public bool status;
    public string path;
    public string message;
}

static class InstancesPath {
    public static string path;
    public static string modPath;
}

static class Mods {
    public static string[] profileMods;
    public static string[] installedMods;
}

namespace Minecraft_ModPack_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            downloadMods();
            MinecraftPathGetter path = CheckForCurseForge();
            if (path.status == false)
            {
                ErrorMessage.Foreground = Brushes.Red;
                ErrorMessage.Text = path.message;
            } else {
                ErrorMessage.Foreground = Brushes.Green;
                ErrorMessage.Text = "Path found ! Path: " + path.path;

                string profilePath = SelectMinecraftProfile(path.path + "\\Instances");
                Pathes.Text = profilePath;
                InstancesPath.path = path.path + "\\Instances";
            }


        }

        private MinecraftPathGetter CheckForCurseForge() {

            RegistryKey CurseForgePath = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Overwolf\\CurseForge");

            if (CurseForgePath == null) {
                MinecraftPathGetter returnValue = new MinecraftPathGetter();

                returnValue.status = false;
                returnValue.path = null;
                returnValue.message = "Missing Overwolf or CurseForge installation or registry value";
                return returnValue;

            } else {
                string value = (string)CurseForgePath.GetValue("minecraft_root");

                if (value == null) {
                    MinecraftPathGetter returnValue = new MinecraftPathGetter();
                    returnValue.status = false;
                    returnValue.path = null;
                    returnValue.message = "Missing minecraft CurseForge installation";
                    return returnValue;

                } else {
                    MinecraftPathGetter returnValue = new MinecraftPathGetter();

                    returnValue.status = true;
                    returnValue.path = value;
                    returnValue.message = null;
                    return returnValue;
                }
            }

        }

        private string SelectMinecraftProfile(string CurseForgePath) {
            string[] instances = Directory.GetDirectories(CurseForgePath);
            if (instances.Length == 0) {
                return "no instances installed";
            }

            InstanceSelector.Visibility = Visibility.Visible;
            InstanceSelector.ItemsSource = SplitAndGetLastvalue(instances);
            return "Profiles found.";
        }

        private void InstanceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pathes.Text = "Instance selected: " + (string)InstanceSelector.SelectedValue;
            string workingDirectory = Environment.CurrentDirectory;

            string[] modFolderFiles = Directory.GetFiles(InstancesPath.path + "\\" + (string)InstanceSelector.SelectedValue + "\\mods");
            string[] modAssets = Directory.GetFiles(workingDirectory + "\\modList");

            Mods.installedMods = SplitAndGetLastvalue(modAssets);
            Mods.profileMods = SplitAndGetLastvalue(modFolderFiles);

            bool toUpdate = Enumerable.SequenceEqual(Mods.installedMods, Mods.profileMods);


            DifferencesStatus.Visibility = Visibility.Visible;
            if (toUpdate)
            {
                DifferencesStatus.Foreground = Brushes.Green;
                DifferencesStatus.Text = "Everything is up to date !";
                ExitButton.Visibility = Visibility.Visible;
                ExitButton.IsEnabled = true;
            }
            else {
                InstancesPath.modPath = InstancesPath.path + "\\" + (string)InstanceSelector.SelectedValue + "\\mods";
                DifferencesStatus.Foreground = Brushes.OrangeRed;
                DifferencesStatus.Text = "New mods are available, would you like to update your profile ?";
                UpdateButton.Visibility = Visibility.Visible;
                UpdateButton.IsEnabled = true;
            }
        }

        private string[] SplitAndGetLastvalue(string[] stringList) {
            int index = 0;
            string[] stringCut = new string[stringList.Length];

            foreach (string str in stringList)
            {
                stringCut[index] = str.Split("\\").Last();
                index++;
            }

            return stringCut;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo dirProfile = new DirectoryInfo(InstancesPath.modPath);
            string workingDirectory = Environment.CurrentDirectory;
            DirectoryInfo dirInstalled = new DirectoryInfo(workingDirectory + "\\modList");


            int maximum = dirProfile.GetFiles().Length + dirInstalled.GetFiles().Length;

            DifferencesStatus.Foreground = Brushes.OrangeRed;
            DifferencesStatus.Text = "Updating mods ...";
            Progression.Visibility = Visibility.Visible;
            Progression.Maximum = maximum;
            Progression.Value = 0;
            int counter = 0;

            foreach (FileInfo file in dirProfile.GetFiles())
            {
                file.Delete();
                counter++;
                Progression.Value = counter;
                //Thread.Sleep(100);
            }

            string[] filesToInstall = Directory.GetFiles(workingDirectory + "\\modList");
            foreach (string s in filesToInstall)
            {
                // Use static Path methods to extract only the file name from the path.
                string fileName = System.IO.Path.GetFileName(s);
                string destFile = System.IO.Path.Combine(InstancesPath.modPath, fileName);
                System.IO.File.Copy(s, destFile, true);
                counter++;
                Progression.Value = counter;
                //Thread.Sleep(100);
            }

            DifferencesStatus.Foreground = Brushes.Green;
            DifferencesStatus.Text = "Update finish ! You can close the window by pressing exit button.";
            ExitButton.Visibility = Visibility.Visible;
            ExitButton.IsEnabled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            Environment.Exit(1);
        }

        private async void downloadMods() {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("MyApplication", "1"));
            var repo = "ImShockwaves/Minecraft-ModPack-Manager";
            var contentsUrl = $"https://api.github.com/repos/ImShockwaves/Minecraft-ModPack-Manager/contents/Minecraft%20ModPack%20Manager/modList?ref=master";
            var contentsJson = await httpClient.GetStringAsync(contentsUrl);
            var contents = (JArray)Newtonsoft.Json.JsonConvert.DeserializeObject(contentsJson);

            string workingDirectory = Environment.CurrentDirectory;
            if (!Directory.Exists(workingDirectory + "\\modList"))
            {
                Directory.CreateDirectory(workingDirectory + "\\modList");
            }
            DirectoryInfo dirInstalled = new DirectoryInfo(workingDirectory + "\\modList");
            foreach (FileInfo file in dirInstalled.GetFiles())
            {
                file.Delete();
            }

            foreach (var file in contents)
            {
                var fileType = (string)file["type"];
                if (fileType == "dir")
                {
                    var directoryContentsUrl = (string)file["url"];
                    // use this URL to list the contents of the folder
                    Console.WriteLine($"DIR: {directoryContentsUrl}");
                }
                else if (fileType == "file")
                {
                    var downloadUrl = (string)file["download_url"];
                    // use this URL to download the contents of the file
                    using (var client = new WebClient())
                    {
                        client.DownloadFile(downloadUrl, workingDirectory + "\\modList\\" + (string)file["name"]);
                    }
                }
                    //Console.WriteLine($"DOWNLOAD: {downloadUrl}");
            }
        }

    }



}
