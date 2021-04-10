using SecretChat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.UI.Notifications;
using System.Net.NetworkInformation;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        bool upToDate = false; //ACHTUNG, muss auf false gesetzt werden nur fürs debuggen auf true!!!
        NameWindow userNameInput = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SecretChat";
            string path2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string currentVersion = "1.9";
            int time = 2700;
            bool log;
            string macAndUser = null;
            List<StoredUserEntity> storedUsers = new List<StoredUserEntity>();
            UsersController usersController = new UsersController();
            VersionController versionController = new VersionController();
            base.OnStartup(e);
            try
            {
                Directory.CreateDirectory(path);
                InstanceAlreadyRunning(e);
                CompareVersions(currentVersion, versionController);
                if (File.Exists(path + "\\code.txt"))
                {

                    macAndUser = GetMac() + Environment.UserName;
                    string code = System.IO.File.ReadAllText(path + "\\code.txt");
                    storedUsers = usersController.GetStoredUsers(macAndUser, code);
                    if (storedUsers == null)
                    {
                        userNameInput = new NameWindow(null);
                        userNameInput.Show();
                        return;
                    }
                    bool macStored = false;
                    IsUserStored(storedUsers, ref macStored, macAndUser);
                }
                else
                {
                    userNameInput = new NameWindow(null);
                    userNameInput.ShowDialog();
                }

                if (time == 2700)
                    {
                        log = true;
                    }
                    else
                    {
                        log = false;
                    }
                    var splashScreen = new SplashScreenWindow(log);
                    this.MainWindow = splashScreen;
                    splashScreen.Show();
                    //userNameInput.Close();
                    RemoveProfilePictures(path2, storedUsers);
                    CreateFiles(path, macAndUser, path2);
                    SaveFilterWords(path);
                    Task.Factory.StartNew(() =>
                    {
                        System.Threading.Thread.Sleep(time); //eigentlich 2700
                    this.Dispatcher.Invoke(() =>
                            {
                                var mainWindow = new MainWindow("true");
                                this.MainWindow = mainWindow;
                                splashScreen.Close();
                            //mainWindow.Show();

                        });
                    });
                
            }
            catch (Exception ex)
            {
                if (IsConnectedToInternet() == true)
                {
                    var st = new StackTrace(ex, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    string ErrorInfo = "Unsere Server sind momentan leider nicht verfügbar. Fehler:  " + ex.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                    ModernWpf.MessageBox.Show(ErrorInfo);
                    Environment.Exit(0);

                }
                else
                {
                    ModernWpf.MessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal.");

                }
            }

        }

        public bool IsConnectedToInternet()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204"))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RemoveProfilePictures(string path2, List<StoredUserEntity> storedUsers)
        {
            foreach (var u in storedUsers)
            {
                if (u.storedMac != null)
                {
                    if (u.Picture == null || u.Picture == "x")
                    {
                        if (File.Exists(path2 + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png"))
                        {
                            try
                            {
                                File.Delete(path2 + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png");
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
        }

        private void IsUserStored(List<StoredUserEntity> storedUsers, ref bool macStored, string macAndUser)
        {

            foreach (var mac in storedUsers)
            {
                if (mac.storedMac == macAndUser)
                {
                    macStored = true;
                }
            }
            if (macStored == false)
            {
                userNameInput = new NameWindow(null);
                userNameInput.ShowDialog();
            }

        }

        private static void InstanceAlreadyRunning(StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                Process[] processlist = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                foreach (var p in processlist)
                {
                    if (p.ProcessName == Process.GetCurrentProcess().ProcessName)
                    {
                        if (p.Id != Process.GetCurrentProcess().Id)
                        {
                            Thread.Sleep(30000);
                        }
                    }
                }
            }
        }

        private void CompareVersions(string currentVersion, VersionController versionController)
        {
            try
            {
                var latestVersion = versionController.GetVersion();
                foreach (var v in latestVersion)
                {
                    if (v.VersionNumber.Contains(currentVersion))
                    {
                        upToDate = true;
                    }
                }
            }
            catch(Exception e)
            {
                if (IsConnectedToInternet() == false)
                {
                    ModernWpf.MessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal.");
                    Environment.Exit(0);
                }
                else
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    string ErrorInfo = "Unbekannter Fehler, sende ihn bitte an Armin um ihn zu beheben. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                    Clipboard.SetText(ErrorInfo);
                    ModernWpf.MessageBox.Show("Ein unbekannter Fehler ist aufgetreten, möglicherweise haben wir Teschnische Schwierigkeiten, Du wirst nun zu dem Download weitergeleitet (wenn die Versionsnummern gleich sind dann warte bis zum nächsten Update), da die neuste Version dieses Problem höchstwahrscheinlich beheben wird. Damit dies nicht wieder vorkommt sende den Fehler (er wurde in deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + ". Der Fehler wurde in Deine Zwischenablage gespeichert.");
                    Process.Start("https://github.com/SagMeinenNamen/SecretChat/raw/main/SecretChat.exe");
                    Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/SecretChat.exe");
                    Environment.Exit(0);
                }
            }
            if (upToDate == false)
            {
                ModernWpf.MessageBox.Show("Du brauchst die neuste Version von Secret Chat. Du wirst nun automatisch zum download weitergeleitet. Installiere sie und führe sie aus um Secret Chat wie gewohnt weiter zu verwenden. In Updates sind meistens Fehlerkorrekturen, neue Funktionen oder neue Designs verfügbar!");
                Process.Start("https://github.com/SagMeinenNamen/SecretChat/raw/main/SecretChat.exe");
                Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/SecretChat.exe");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }

        }

        private static void CreateFiles(string path, string macAndUser, string path2)
        {
            if (System.IO.File.Exists(path2 + "\\SecretChat\\ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png"))
            {
                File.Delete(path2 + "\\SecretChat\\ProfilePictures\\" + macAndUser.Replace(":", ".") + ".png");
                System.IO.File.Move(path2 + "\\SecretChat\\ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png", path2 + "\\SecretChat\\ProfilePictures\\" + macAndUser.Replace(":", ".") + ".png");
            }
            if (!File.Exists(path + "\\darkorlight.txt"))
            {
                System.IO.File.WriteAllText(path + "\\darkorlight.txt", "dark");
            }
            if (!File.Exists(path + "\\autostart.txt"))
            {
                File.WriteAllText(path + "\\autostart.txt", "true");
            }
            if (!File.Exists(path + "\\notifications.txt"))
            {
                File.WriteAllText(path + "\\notifications.txt", "true");
            }
            if (!File.Exists(path + "\\links.txt"))
            {
                File.WriteAllText(path + "\\links.txt", "true");
            }
            if (!File.Exists(path + "\\timer.txt"))
            {
                File.WriteAllText(path + "\\timer.txt", "500");
            }
            if (!File.Exists(path + "\\info.txt"))
            {
                System.IO.File.WriteAllText(path + "\\info.txt", "false");
            }
            if (!File.Exists(path + "\\sounds.txt"))
            {
                System.IO.File.WriteAllText(path + "\\sounds.txt", "true");
            }
            if (!File.Exists(path + "\\mentions.txt"))
            {
                System.IO.File.WriteAllText(path + "\\mentions.txt", "true");
            }
            Directory.CreateDirectory(path + "\\ProfilePictures");
            if (!File.Exists(path + "\\ProfilePictures\\default.png"))
            {
                Bitmap bitmap = new Bitmap(SecretChat.Properties.Resources._default);
                bitmap.Save(path + @"\ProfilePictures\default.png");
            }
            if (!File.Exists(path + @"\SecretChat\icon.png"))
            {
                Bitmap bitmap = new Bitmap(SecretChat.Properties.Resources.iconpng);
                bitmap.Save(path + @"\icon.png");

            }
        }

        private static void SaveFilterWords(string path)
        {
            string badWords = $"hMPI16Q0XFKsChhjb/mJ5w=={Environment.NewLine}Wq930h61Dms55XFVp7/VTw=={Environment.NewLine}962eS3CwTTFFPxXoXU8IlQ=={Environment.NewLine}LeKpBmwsVtXyaz2sas0nHg=={Environment.NewLine}9hG6Uktzx4K6Au2MRn9hdA=={Environment.NewLine}UgGkmdK6TD4Wkq3a0rrKIQ=={Environment.NewLine}7vbez0VFl/M4H6YI2O1hDg=={Environment.NewLine}g7Pyxrm4DwGtfPqXV8vjCg=={Environment.NewLine}kuVZKvY+Ym7Zj1aslxHZ+w=={Environment.NewLine}3I0ZZbh03WVv/LJrp+ND6g=={Environment.NewLine}3rR3JGBX7RnG53EW3RRI8g=={Environment.NewLine}L83tXedbrRWjINfEQ2O1Bw=={Environment.NewLine}2PMVKYKhzTrvWO0b6jBItQ=={Environment.NewLine}bOrBtrkpcR5iVt0Dh8dVGA=={Environment.NewLine}dkmhL3yufIjWCjqSJ7RheQ=={Environment.NewLine}TOAGQWNg0W4TM2pWcVlFNg=={Environment.NewLine}ehR2zSq43wMjMWLuJU/1dw=={Environment.NewLine}dV9hNfym84bSkFxAmCeSIQ=={Environment.NewLine}HAHvWZv+3VrtlgZa6KmSKw=={Environment.NewLine}YmA6OcVtqLgB4IEhjyo+yA=={Environment.NewLine}0U4zncfOmWXaTKw3G+3PMg=={Environment.NewLine}nTzEIGVw1THr6Yj7FYgXsA=={Environment.NewLine}gSwK/+O+JKopb+8auuPV4w=={Environment.NewLine}hbu1E2aDwJLdrwEznULBcw=={Environment.NewLine}bQ9gcDIcSDOOzmAZB/h68w=={Environment.NewLine}tZzjQzHgQ2CJvn1IruGHKQ=={Environment.NewLine}TQWfjcm2vMGk+v7AcDUvjg=={Environment.NewLine}zPjaNBvSSE2Pqd/kxPvpWQ=={Environment.NewLine}xmxYI176BVxSTOquYuh9fQ=={Environment.NewLine}ItLgzyeOQE9mprjUVOaP5A=={Environment.NewLine}6xIT0YBfTwi++xUbCUfGpg=={Environment.NewLine}oeZMdvBjAtOQ+O5DtwK5Lw=={Environment.NewLine}ok4qBMihv1XJpZOVafSNfA=={Environment.NewLine}WLCWlwjjYqV/tRuXsLqIKg=={Environment.NewLine}Nvqx2TZd51kift/+qFHb3g=={Environment.NewLine}41aPvneiIAGAPCH2KMqVPQ=={Environment.NewLine}SdmRwQPscymq0ULcKVbaGg=={Environment.NewLine}v3LmHXNZut+IbAGgJcguGw=={Environment.NewLine}POdYElyowXjxuav8RcD1qg=={Environment.NewLine}eAdLqY8g7TOe4QPYMZwJLg=={Environment.NewLine}8AXOV2fSP9SJpADyZJMB1A=={Environment.NewLine}OvmG1w3sGohXdWFz7x6P5Q=={Environment.NewLine}k2akDGQTDC6y0UaU3ZGNYQ=={Environment.NewLine}TEdc9iIbxR/xyc/F6D9HBg==";
            string goodWords = $"****{Environment.NewLine}ba*****{Environment.NewLine}sch********{Environment.NewLine}sch****{Environment.NewLine}sch***{Environment.NewLine}****{Environment.NewLine}***************{Environment.NewLine}*********{Environment.NewLine}sp****{Environment.NewLine}la***{Environment.NewLine}f***{Environment.NewLine}*****{Environment.NewLine}*********{Environment.NewLine}***{Environment.NewLine}*******{Environment.NewLine}sh**{Environment.NewLine}sh******{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}****{Environment.NewLine}********{Environment.NewLine}fr****{Environment.NewLine}*****{Environment.NewLine}*****{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}*******{Environment.NewLine}dr***{Environment.NewLine}****{Environment.NewLine}***{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}****{Environment.NewLine}*****{Environment.NewLine}****{Environment.NewLine}*******{Environment.NewLine}*****{Environment.NewLine}**********{Environment.NewLine}****{Environment.NewLine}*****{Environment.NewLine}***{Environment.NewLine}*******{Environment.NewLine}*********{Environment.NewLine}******";
            File.WriteAllText(path + "\\words.txt", badWords);
            File.WriteAllText(path + "\\goodwords.txt", goodWords);
        }

        private string GetMac()
        {
            string Mac = string.Empty;
            ManagementClass MC = new ManagementClass("Win32_NetworkAdapter");
            ManagementObjectCollection MOCol = MC.GetInstances();
            foreach (ManagementObject MO in MOCol)
                if (MO != null)
                {
                    if (MO["MacAddress"] != null)
                    {
                        Mac = MO["MACAddress"].ToString();
                        if (Mac != string.Empty)
                            break;
                    }
                }
            return Mac;
        }
    }
}
