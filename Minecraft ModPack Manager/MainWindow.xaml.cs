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

class MinecraftPathGetter {
    public bool status;
    public string path;
    public string message;
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

            string[] instancesPathCut = new string[instances.Length];

            int index = 0;
            InstanceSelector.Visibility = Visibility.Visible;
            foreach (string instance in instances) {
                instancesPathCut[index] = instance.Split("\\").Last();
                index++;
            }

            InstanceSelector.ItemsSource = instancesPathCut;
            return "Ok";
            // return string.Join(",", instances);
        }

        private void InstanceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pathes.Text = (string)InstanceSelector.SelectedValue;
        }
    }



}
