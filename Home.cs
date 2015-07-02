using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Java_Version_Selector
{
    public partial class Home : Form
    {

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr pszPath);

        List<string> JavaPath = new List<string>();
        List<string> JavaVersions;

        public Home()
        {
            InitializeComponent();
            LoadJavaVersions();
        }

        private void LoadJavaVersions()
        {
            MyName.Links.Add(0, MyName.Text.Length, "http://www.navjotsinghminhas.com");
            JavaVersions = GetJavaInstallationPath(out JavaPath);

            if (JavaVersions != null)
            {
                VersionSelector.DataSource = JavaVersions;
            }
            else
            {
                VersionSelector.Enabled = false;
                button1.Enabled = false;
                Websites.Enabled = false;
            }

            //Reading Sites
            try
            {
                Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
                FileInfo saveWebsites = new FileInfo(Path.Combine(GetKnownFolderPath(localLowId), "Sun\\Java\\Deployment\\security\\exception.sites"));
                StreamReader reader = saveWebsites.OpenText();
                Websites.Text = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception) { }
        }

        private List<string> GetJavaInstallationPath(out List<string> JavaPath)
        {
            JavaPath = new List<string>();
            List<string> JavaVersions = new List<string>();

            //string environmentPath = Environment.GetEnvironmentVariable("JAVA_HOME");
            //if (!string.IsNullOrEmpty(environmentPath))
            //{
            //    return environmentPath;
            //}

            string javaKey = string.Empty;

            try
            {
                javaKey = "SOFTWARE\\Javasoft\\Java Runtime Environment\\";
                using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                {
                    string[] rkSubKey = rk.GetSubKeyNames();

                    foreach (string s in rkSubKey)
                    {
                        string[] temp = s.Split('.');
                        if (temp.Length == 3)
                        {
                            using (Microsoft.Win32.RegistryKey rks = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey + s + "\\"))
                            {
                                JavaVersions.Add(s);
                                JavaPath.Add(rks.GetValue("JavaHome").ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            if (Environment.Is64BitOperatingSystem)
            {
                try
                {
                    javaKey = "SOFTWARE\\Wow6432Node\\Javasoft\\Java Runtime Environment\\";

                    using (Microsoft.Win32.RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey))
                    {
                        string[] rkSubKey = rk.GetSubKeyNames();

                        foreach (string s in rkSubKey)
                        {
                            string[] temp = s.Split('.');
                            if (temp.Length == 3)
                            {
                                using (Microsoft.Win32.RegistryKey rks = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(javaKey + s + "\\"))
                                {
                                    if (!JavaVersions.Contains(s))
                                    {
                                        JavaVersions.Add(s);
                                        JavaPath.Add(rks.GetValue("JavaHome").ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception) { }
            }

            if (JavaVersions.Count == 0)
            {
                MessageBox.Show("We thing Java is not installed on your system.", "No java detected !", MessageBoxButtons.OK);
                return null;
            }

            return JavaVersions;
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");

            //Reading Deployment Properties for write operation
            FileInfo openDeploymentProperties = new FileInfo(Path.Combine(GetKnownFolderPath(localLowId), "Sun\\Java\\Deployment\\deployment.properties"));
            try
            {
                StreamReader reader = openDeploymentProperties.OpenText();
                string DeploymentProperties = reader.ReadToEnd();
                reader.Close();
                var DeploymentPropertiesStrings = DeploymentProperties.Split(new String[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                //Saving Deployment Properties
                SaveDeploymentProperties(DeploymentPropertiesStrings);
            }
            catch(Exception)
            {
                MessageBox.Show("Goto -> Control Panel -> Java. \n(Just open and close) \n\nIf this still doesn't works, contact the developer.", "Action needed !", MessageBoxButtons.OK);
                return;
            }

            //Saving websites
            FileInfo saveWebsites = new FileInfo(Path.Combine(GetKnownFolderPath(localLowId), "Sun\\Java\\Deployment\\security\\exception.sites"));
            StreamWriter writer2 = saveWebsites.CreateText();
            string[] temp = Websites.Text.Split('\n');
            List<string> WebsiteList = new List<string>();
            foreach (string s in temp)
            {
                if (s != "")
                {
                    WebsiteList.Add(s);
                }
            }
            if (WebsiteList.Count != 0)
            {
                for (int i = 0; i < WebsiteList.Count; i++)
                {
                    string[] temp1 = WebsiteList[i].Split('\r');
                    WebsiteList[i] = temp1[0];
                }
                WebsiteList = WebsiteList.Distinct().ToList<string>();
                List<string> WebsiteNames = new List<string>();
                foreach (string s in WebsiteList)
                {
                    Uri uriResult;
                    bool result = Uri.TryCreate(s, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                    if (result)
                        WebsiteNames.Add(s);
                }

                for (int i = 0; i < WebsiteNames.Count; i++)
                {
                    if (i == (WebsiteNames.Count - 1))
                    {
                        writer2.Write(WebsiteNames[i]);
                    }
                    else
                    {
                        writer2.Write(WebsiteNames[i] + "\r\n");
                    }
                }
            }
            writer2.Close();

            MessageBox.Show("Done, Current version is "+ JavaVersions[VersionSelector.SelectedIndex], "RESTART YOUR FIREFOX !!!!", MessageBoxButtons.OK);
        }

        /// <summary>
        /// Saving modified Deployment Properties
        /// </summary>
        /// <param name="DeploymentPropertiesStrings">
        /// Strings read from Deployment Properties
        /// </param>
        void SaveDeploymentProperties(String[] DeploymentPropertiesStrings)
        {
            Guid localLowId = new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16");
            FileInfo saveProperties = new FileInfo(Path.Combine(GetKnownFolderPath(localLowId), "Sun\\Java\\Deployment\\deployment.properties"));
            StreamWriter writer1 = saveProperties.CreateText();
            string[] temptemp = JavaVersions[VersionSelector.SelectedIndex].Split('.');
            string[] tempPath = JavaPath[VersionSelector.SelectedIndex].Split(':');
            string path = tempPath[1].Replace("\\", "\\\\");
            string CurrentVersion = "0";
            foreach (string s in DeploymentPropertiesStrings)
            {
                if (s.Contains(JavaVersions[VersionSelector.SelectedIndex]))
                {
                    var t = s.Split('.');
                    CurrentVersion = t[3];
                }
            }
            for (int i = 0; i < DeploymentPropertiesStrings.Count(); i++)
            {
                if (DeploymentPropertiesStrings[i].Contains("deployment.javaws.jre." + CurrentVersion + ".enabled"))
                {
                    DeploymentPropertiesStrings[i] = "deployment.javaws.jre." + CurrentVersion + ".enabled=true";
                }
                else if (DeploymentPropertiesStrings[i].Contains("enabled"))
                {
                    var t = DeploymentPropertiesStrings[i].Split('=');
                    DeploymentPropertiesStrings[i] = t[0] + "=false";
                }
            }

            string DeploymentProperties = string.Empty;
            foreach (string s in DeploymentPropertiesStrings)
            {
                DeploymentProperties += s + "\r\n";
            }
            writer1.Write(DeploymentProperties);
            writer1.Close();
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }



        private void MyName_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }
        
        string GetKnownFolderPath(Guid knownFolderId)
        {
            IntPtr pszPath = IntPtr.Zero;
            try
            {
                int hr = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out pszPath);
                if (hr >= 0)
                    return Marshal.PtrToStringAuto(pszPath);
                throw Marshal.GetExceptionForHR(hr);
            }
            finally
            {
                if (pszPath != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pszPath);
            }
        }
    }
}
