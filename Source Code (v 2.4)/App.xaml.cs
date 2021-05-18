using AutoUpdaterDotNET;
using DK.WshRuntime;
using ModernMessageBoxLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        bool upToDate = false; //ACHTUNG, muss auf false gesetzt werden nur fürs debuggen auf true!!!
        string code;
        NameWindow userNameInput = null;
        readonly string currentVersion = "2.4";
        readonly string SecretChatPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SecretChat\\Versions\\";
        readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SecretChat";
        readonly string path2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        readonly UsersController usersController = new UsersController();

        protected override void OnStartup(StartupEventArgs e)
        {
            var splashScreen = new SplashScreenWindow();
            splashScreen.Show();
            string macAndUser = null;
            List<StoredUserEntity> storedUsers = new List<StoredUserEntity>();
            VersionController versionController = new VersionController();
            base.OnStartup(e);
            try
            {
                Directory.CreateDirectory(path);
                InstanceAlreadyRunning(e);
                CompareVersions(currentVersion, versionController);
                string currentExePath = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                string exePath = SecretChatPath + Process.GetCurrentProcess().ProcessName + ".exe";
                //if (Regex.IsMatch(currentExePath.Replace(@"\", ""), exePath.Replace(@"\", ""), RegexOptions.IgnoreCase))
                //{
                //    //Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu ) + "\\Programs");
                //    //File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\SecretChat\\test.txt", "bruh");
                DeleteOldVersions();
                CreateStartMenuShortcut();
                //}
                //else
                //{
                //    //if (!currentExePath.Contains("System32"))
                //    //{
                //    //    try
                //    //    {
                //    //        if (System.IO.File.Exists(SecretChatPath + Process.GetCurrentProcess().ProcessName + ".exe"))
                //    //        {
                //    //            System.IO.File.Delete(SecretChatPath + Process.GetCurrentProcess().ProcessName + ".exe");
                //    //        }
                //    //        System.IO.File.Copy(Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe", SecretChatPath + Process.GetCurrentProcess().ProcessName + currentVersion + ".exe");
                //    //    }
                //    //    catch
                //    //    {

                //    //    }
                //    //}
                //    CreateStartMenuShortcut(currentVersion);
                //}
                if (System.IO.File.Exists(path + "\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\code.txt");

                }
                else
                {
                    code = "000000";
                }
                macAndUser = GetMac() + Environment.UserName;
                storedUsers = usersController.GetStoredUsers(macAndUser, code);
                if (storedUsers == null)
                {
                    userNameInput = new NameWindow(null);
                    splashScreen.Close();
                    userNameInput.ShowDialog();
                }
                //IsUserStored(storedUsers, ref macStored, macAndUser);

                //userNameInput.Close();
                RemoveProfilePictures(path2, storedUsers);
                CreateFiles(path, macAndUser, path2);
                SaveFilterWords(path);
                Task.Factory.StartNew(() =>
                {
                    //System.Threading.Thread.Sleep(time); //eigentlich 2700
                    this.Dispatcher.Invoke(() =>
                        {
                            var mainWindow = new MainWindow("true", currentVersion);
                            //this.MainWindow = mainWindow;
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
                    string ErrorInfo = "Zeitüberschreitung, möglicherweise sind unsere Server sind momentan leider nicht verfügbar. Fehler:  " + ex.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                    ModernWpf.MessageBox.Show(ErrorInfo);
                    Environment.Exit(0);

                }
                else
                {
                    splashScreen.Hide();
                    QModernMessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal. Fehler: " + ex.Message, "Fehler", QModernMessageBox.QModernMessageBoxButtons.Ok, ModernMessageboxIcons.Warning);
                    Environment.Exit(0);
                }
            }

        }

        private void DeleteOldVersions()
        {
            if (Directory.Exists(SecretChatPath))
            {
                var directories = Directory.GetDirectories(SecretChatPath);
                if (directories != null)
                {
                    foreach (var subfolder in directories)
                    {
                        if (subfolder != SecretChatPath + currentVersion)
                        {
                            try
                            {
                                File.Delete(subfolder + "\\SecretChat.exe");
                                Directory.Delete(subfolder);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
        }

        private void CreateStartMenuShortcut()
        {
            string pathToExe = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SecretChat\\Versions\\" + currentVersion +  "\\SecretChat.exe";
            string shortcutLocation = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "\\Programs\\", "A Secret Chat" + ".lnk");
            if (!File.Exists(shortcutLocation))
            {
                WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
            }
            else if (GetShortcutTarget(shortcutLocation) != pathToExe)
            {
                File.Delete(shortcutLocation);
                WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
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

        private void RemoveProfilePictures(string path2, List<StoredUserEntity> storedUsers)
        {
            foreach (var u in storedUsers)
            {
                if (u.UserID != null)
                {
                    if (u.Picture == null || u.Picture == "x")
                    {
                        if (System.IO.File.Exists(path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png"))
                        {
                            try
                            {
                                System.IO.File.Delete(path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png");
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }
            if (File.Exists(path + "\\ProfilePictures"))
            {
                string filepath = path + "\\ProfilePictures";
                DirectoryInfo d = new DirectoryInfo(filepath);

                foreach (var file in d.GetFiles("*.png"))
                {
                    if (!file.Name.Contains("GUID") && file.Name != "default.png")
                    {
                        File.Delete(file.FullName);
                    }
                }
            }

        }

        //private void IsUserStored(List<StoredUserEntity> storedUsers, ref bool macStored, string macAndUser)
        //{
        //    if (storedUsers.Count != 0)
        //    {
        //        macStored = true;
        //    }
        //    if (macStored == false)
        //    {
        //        userNameInput = new NameWindow(null);
        //        userNameInput.ShowDialog();
        //    }

        //}

        private static void InstanceAlreadyRunning(StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                Process[] processlist = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                foreach (var p in processlist)
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        Thread.Sleep(6000);

                    }
                }
            }
        }

        private void CompareVersions(string currentVersion, VersionController versionController)
        {
            string latestVer = "";
            try
            {
                var latestVersion = versionController.GetVersion();
                foreach (var v in latestVersion)
                {
                    latestVer = v.VersionNumber;
                    if (v.VersionNumber.Contains(currentVersion))
                    {
                        upToDate = true; //TRUE
                    }
                }
            }
            catch (Exception e)
            {
                if (IsConnectedToInternet() == false)
                {

                    QModernMessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal. Fehler: " + e.Message, "Fehler", QModernMessageBox.QModernMessageBoxButtons.Ok, ModernMessageboxIcons.Warning);
                    Environment.Exit(0);
                }
                else
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    string ErrorInfo = "Unbekannter Fehler, sende ihn bitte an Armin um ihn zu beheben. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                    Clipboard.SetText(ErrorInfo);
                    ModernWpf.MessageBox.Show("Ein unbekannter Fehler ist aufgetreten, möglicherweise haben wir Teschnische Schwierigkeiten, Du wirst nun zu dem Download weitergeleitet (wenn die Versionsnummern gleich sind dann warte bis zum nächsten Update), da die neuste Version dieses Problem höchstwahrscheinlich beheben wird. Damit dies nicht wieder vorkommt sende den Fehler (er wurde in Deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + ". Der Fehler wurde in Deine Zwischenablage gespeichert.");
                    Process.Start("https://github.com/SagMeinenNamen/SecretChat/raw/main/SecretChat.exe");
                    Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/SecretChat.exe");
                    Environment.Exit(0);
                }
            }
            if (upToDate == false)
            {
                if (!File.Exists(SecretChatPath + latestVer + "\\SecretChat.exe"))
                {
                    UpdateInfoEventArgs args = new UpdateInfoEventArgs
                    {
                        DownloadURL = "https://github.com/SagMeinenNamen/SecretChat/raw/main/SecretChat.exe",
                        CurrentVersion = latestVer
                    };
                    Version version = new Version(currentVersion);
                    args.InstalledVersion = version;
                    try
                    {
                        AutoUpdater.ShowSkipButton = false;
                        AutoUpdater.RunUpdateAsAdmin = false;
                        AutoUpdater.ShowRemindLaterButton = false;
                        AutoUpdater.DownloadPath = SecretChatPath + latestVer;
                        AutoUpdater.AppTitle = "Secret Chat";
                        AutoUpdater.Mandatory = true;
                        AutoUpdater.UpdateMode = Mode.ForcedDownload;
                        AutoUpdater.DownloadUpdate(args);
                        //if (System.IO.File.Exists(SecretChatPath + "SecretChat.exe"))
                        //{
                        //    try
                        //    {
                        //        System.IO.File.Delete(SecretChatPath + "SecretChat.exe");
                        //    }
                        //    catch
                        //    {

                        //    }
                        //}
                        //System.IO.File.Move(path3, SecretChatPath + "SecretChat" + latestVer + ".exe");
                        //var startInfo = SecretChatPath + "SecretChat" + latestVer + ".exe";
                        //Process.Start(startInfo, "restart");
                        Process.GetCurrentProcess().Kill();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Ein Fehler ist während der Installation aufgetreten: " + e.Message + Environment.NewLine + "Du wirst nun zum manuellen download weitergeleitet.");
                        //ModernWpf.MessageBox.Show("Du brauchst die neuste Version von Secret Chat. Du wirst nun automatisch zum download weitergeleitet. Installiere sie und führe sie aus um Secret Chat wie gewohnt weiter zu verwenden. In Updates sind meistens Fehlerkorrekturen, neue Funktionen oder neue Designs verfügbar!");
                        Process.Start("https://github.com/SagMeinenNamen/SecretChat/raw/main/SecretChat.exe");
                        Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/SecretChat.exe");
                        Thread.Sleep(1000);
                        Environment.Exit(0);
                    }
                }
                else
                {
                    var startInfo = SecretChatPath + latestVer + "\\SecretChat.exe";
                    Process.Start(startInfo, "restart");
                    Environment.Exit(0);
                }

            }

        }

        private void CreateFiles(string path, string macAndUser, string path2)
        {
            var user = usersController.GetSpecificUserName(macAndUser, code);
            foreach (var u in user)
            {
                if (System.IO.File.Exists(path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + "copy.png"))
                {
                    System.IO.File.Delete(path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png");
                    System.IO.File.Move(path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + "copy.png", path2 + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png");
                }
                if (!System.IO.File.Exists(path + "\\darkorlight.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\darkorlight.txt", "dark");
                }
                if (!System.IO.File.Exists(path + "\\autostart.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\autostart.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\notifications.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\notifications.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\links.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\links.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\timer.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\timer.txt", "500");
                }
                if (!System.IO.File.Exists(path + "\\info.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\info.txt", "false");
                }
                if (!System.IO.File.Exists(path + "\\sounds.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\sounds.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\mentions.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\mentions.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\notificationreminder.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\notificationreminder.txt", "true");
                }
                if (!System.IO.File.Exists(path + "\\emojis.txt"))
                {
                    System.IO.File.WriteAllText(path + "\\emojis.txt", "true");
                }
                Directory.CreateDirectory(path + "\\ProfilePictures");
                if (!System.IO.File.Exists(path + "\\ProfilePictures\\default.png"))
                {
                    Bitmap bitmap = new Bitmap(SecretChat.Properties.Resources._default);
                    bitmap.Save(path + @"\ProfilePictures\default.png");
                }
                if (!System.IO.File.Exists(path + @"\SecretChat\icon.png"))
                {
                    Bitmap bitmap = new Bitmap(SecretChat.Properties.Resources.SCIcon);
                    bitmap.Save(path + @"\icon.png");

                }
            }
        }

        private static void SaveFilterWords(string path)
        {
            string badWords = $"hMPI16Q0XFKsChhjb/mJ5w=={Environment.NewLine}Wq930h61Dms55XFVp7/VTw=={Environment.NewLine}962eS3CwTTFFPxXoXU8IlQ=={Environment.NewLine}LeKpBmwsVtXyaz2sas0nHg=={Environment.NewLine}9hG6Uktzx4K6Au2MRn9hdA=={Environment.NewLine}UgGkmdK6TD4Wkq3a0rrKIQ=={Environment.NewLine}7vbez0VFl/M4H6YI2O1hDg=={Environment.NewLine}g7Pyxrm4DwGtfPqXV8vjCg=={Environment.NewLine}kuVZKvY+Ym7Zj1aslxHZ+w=={Environment.NewLine}3I0ZZbh03WVv/LJrp+ND6g=={Environment.NewLine}3rR3JGBX7RnG53EW3RRI8g=={Environment.NewLine}L83tXedbrRWjINfEQ2O1Bw=={Environment.NewLine}2PMVKYKhzTrvWO0b6jBItQ=={Environment.NewLine}bOrBtrkpcR5iVt0Dh8dVGA=={Environment.NewLine}dkmhL3yufIjWCjqSJ7RheQ=={Environment.NewLine}TOAGQWNg0W4TM2pWcVlFNg=={Environment.NewLine}ehR2zSq43wMjMWLuJU/1dw=={Environment.NewLine}dV9hNfym84bSkFxAmCeSIQ=={Environment.NewLine}HAHvWZv+3VrtlgZa6KmSKw=={Environment.NewLine}YmA6OcVtqLgB4IEhjyo+yA=={Environment.NewLine}0U4zncfOmWXaTKw3G+3PMg=={Environment.NewLine}nTzEIGVw1THr6Yj7FYgXsA=={Environment.NewLine}gSwK/+O+JKopb+8auuPV4w=={Environment.NewLine}hbu1E2aDwJLdrwEznULBcw=={Environment.NewLine}bQ9gcDIcSDOOzmAZB/h68w=={Environment.NewLine}tZzjQzHgQ2CJvn1IruGHKQ=={Environment.NewLine}TQWfjcm2vMGk+v7AcDUvjg=={Environment.NewLine}zPjaNBvSSE2Pqd/kxPvpWQ=={Environment.NewLine}xmxYI176BVxSTOquYuh9fQ=={Environment.NewLine}ItLgzyeOQE9mprjUVOaP5A=={Environment.NewLine}6xIT0YBfTwi++xUbCUfGpg=={Environment.NewLine}oeZMdvBjAtOQ+O5DtwK5Lw=={Environment.NewLine}ok4qBMihv1XJpZOVafSNfA=={Environment.NewLine}WLCWlwjjYqV/tRuXsLqIKg=={Environment.NewLine}Nvqx2TZd51kift/+qFHb3g=={Environment.NewLine}41aPvneiIAGAPCH2KMqVPQ=={Environment.NewLine}SdmRwQPscymq0ULcKVbaGg=={Environment.NewLine}v3LmHXNZut+IbAGgJcguGw=={Environment.NewLine}POdYElyowXjxuav8RcD1qg=={Environment.NewLine}eAdLqY8g7TOe4QPYMZwJLg=={Environment.NewLine}8AXOV2fSP9SJpADyZJMB1A=={Environment.NewLine}OvmG1w3sGohXdWFz7x6P5Q=={Environment.NewLine}k2akDGQTDC6y0UaU3ZGNYQ=={Environment.NewLine}TEdc9iIbxR/xyc/F6D9HBg==";
            string goodWords = $"****{Environment.NewLine}ba*****{Environment.NewLine}sch********{Environment.NewLine}sch****{Environment.NewLine}sch***{Environment.NewLine}****{Environment.NewLine}***************{Environment.NewLine}*********{Environment.NewLine}sp****{Environment.NewLine}la***{Environment.NewLine}f***{Environment.NewLine}*****{Environment.NewLine}*********{Environment.NewLine}***{Environment.NewLine}*******{Environment.NewLine}sh**{Environment.NewLine}sh******{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}****{Environment.NewLine}********{Environment.NewLine}fr****{Environment.NewLine}*****{Environment.NewLine}*****{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}*******{Environment.NewLine}dr***{Environment.NewLine}****{Environment.NewLine}***{Environment.NewLine}*****{Environment.NewLine}******{Environment.NewLine}****{Environment.NewLine}*****{Environment.NewLine}****{Environment.NewLine}*******{Environment.NewLine}*****{Environment.NewLine}**********{Environment.NewLine}****{Environment.NewLine}*****{Environment.NewLine}***{Environment.NewLine}*******{Environment.NewLine}*********{Environment.NewLine}******";
            System.IO.File.WriteAllText(path + "\\words.txt", badWords);
            System.IO.File.WriteAllText(path + "\\goodwords.txt", goodWords);
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
        private string GetShortcutTarget(string file)
        {
            try
            {
                if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
                {
                    throw new Exception("Supplied file must be a .LNK file");
                }

                FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
                using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
                {
                    fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                    uint flags = fileReader.ReadUInt32();        // Read flags
                    if ((flags & 1) == 1)
                    {                      // Bit 1 set means we have to
                                           // skip the shell item ID list
                        fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                        uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                        fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                    }

                    long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                                 // structure begins
                    uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                    fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                    uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                               // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                    fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                        // base pathname (target)
                    long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                        // the base pathname. I don't need the 2 terminating nulls.
                    char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                    var link = new string(linkTarget);

                    int begin = link.IndexOf("\0\0");
                    if (begin > -1)
                    {
                        int end = link.IndexOf("\\\\", begin + 2) + 2;
                        end = link.IndexOf('\0', end) + 1;

                        string firstPart = link.Substring(0, begin);
                        string secondPart = link.Substring(end);

                        return firstPart + secondPart;
                    }
                    else
                    {
                        return link;
                    }
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
