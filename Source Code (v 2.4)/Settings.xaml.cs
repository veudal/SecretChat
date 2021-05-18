using IWshRuntimeLibrary;
using Microsoft.Toolkit.Uwp.Notifications;
using SecretChat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Brush = System.Windows.Media.Brush;
using MaterialDesignColors;
using MaterialDesignThemes;
using MaterialDesignThemes.Wpf;
using DK.WshRuntime;

namespace SecretChat
{


    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        bool darkOrLight;
        readonly MessageController messageController = new MessageController();
        string name;
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
        }
        public bool Shutdown = false;
        readonly string path;
        readonly string version = "1.9";
        readonly OnlineUsersController onlineUsersController = new OnlineUsersController();
        readonly UsersController usersController = new UsersController();
        readonly MainWindow _mainWindow;
        public Settings(MainWindow mainWindow)
        {
            
            _mainWindow = mainWindow;
            InitializeComponent();
            this.Topmost = true;
            Topmost = false;
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\SecretChat\\";
            if (System.IO.File.ReadAllText(path + "darkorlight.txt") == "light")
            {
                DarkLightSlider.Value = 1;
                SoundsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                EinstellungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                BenachrichtigungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                LinksText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                AutostartText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Grid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF7F7F7"));
                closeBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
                helpBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
                MeinAccount.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Personalisierung.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Gefahrenzone.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Informationen.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Notification.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                MentionsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                ReminderText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Reminder.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                Emojis.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                sun.Visibility = Visibility.Hidden;
                moon.Visibility = Visibility.Hidden;
            }
            else
            {
                sun4LightTheme.Visibility = Visibility.Hidden;
                moon4LightTheme.Visibility = Visibility.Hidden;
            }
            string macAndUser = GetMac() + Environment.UserName;
            string code;
            List<StoredUserEntity> storedUserName ;
            if (System.IO.File.Exists(path + "code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var storedUsers = usersController.GetStoredUsers(macAndUser, code);
            foreach (var u in storedUsers)
            {
                if (u.storedMac == macAndUser)
                {
                    name = u.PartitionKey;
                }
            }
            storedUserName = usersController.GetSpecificUser(name, macAndUser, code);
            foreach (var u in storedUserName)
            {
                if (u.storedMac == macAndUser)
                {
                    if (u.Verification == "true")
                    {
                        PictureButton.Content = "Profilbild";
                    }
                }
            }
            CenterWindowOnScreen();
            if (System.IO.File.ReadAllText(path + "autostart.txt") == "false")
            {
                AutoStart.IsChecked = false;
            }
            else
            {
                AutoStart.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "notifications.txt") == "false")
            {
                Benachrichtigungen.IsChecked = false;
            }
            else
            {
                Benachrichtigungen.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "links.txt") == "false")
            {
                Links.IsChecked = false;
            }
            else
            {
                Links.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "sounds.txt") == "false")
            {
                Sounds.IsChecked = false;
            }
            else
            {
                Sounds.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "mentions.txt") == "false")
            {
                Mentions.IsChecked = false;
            }
            else
            {
                Mentions.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "notificationreminder.txt") == "false")
            {
                Reminder.IsChecked = false;
            }
            else
            {
                Reminder.IsChecked = true;
            }
            if (System.IO.File.ReadAllText(path + "emojis.txt") == "false")
            {
                Emojis.IsChecked = false;
            }
            else
            {
                Emojis.IsChecked = true;
            }
        }

        private void Sound()
        {
            if (System.IO.File.ReadAllText(path + "sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.preview);
                playSound.Play();
            }
        }

        private void Sound2()
        {
            if (System.IO.File.ReadAllText(path + "sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.mixkit_fast_double_click_on_mouse_275);
                playSound.Play();
            }
        }

        private void ChangeNameButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);  
            var dialogResult = ModernWpf.MessageBox.Show("Wenn Du deinen Namen änderst wird dein Profilbild, als auch deine Vertifizierung zurückgesetzt. Bist du dir sicher?", "Fortfahren?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (dialogResult == MessageBoxResult.Yes)
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");
                }
                else
                {
                                        ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
               
                var NameWindow = new NameWindow(name);
                NameWindow.ShowDialog();
                //NameWindow.Close();
            }
        }

        private void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            try
            {
                string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                WshInterop.CreateShortcut(desktop, null, pathToExe, null, null);
                ModernWpf.MessageBox.Show("Verknüpfung wurde erfolgreich erstellt!");
            }
            catch
            {
                ModernWpf.MessageBox.Show("Es gab einen Fehler beim erstellen; womöglich gibt es schon eine Verknüpfung.");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            ModernWpf.MessageBox.Show("Du benutzt SecretChat® v." + version + " entwickelt von Armin Mulic, Idee und Designs von Psenix, Logo Design von ArcadeToast. Alle rechte vorbehalten. Copyright © 2021");
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Task.Run(Sound);
        }

        private void ChangeTimer_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            var timer = new ChangeTimerWindow();
            timer.ShowDialog();
        }

        private void Benachrichtigungen_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Benachrichtigungen.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\notifications.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\notifications.txt", "false");

            }
        }


        private void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (AutoStart.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\autostart.txt", "true");
                string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                string shortcutLocation = System.IO.Path.Combine(startUpPath, "Secret Chat" + ".lnk");
                WshInterop.CreateShortcut(shortcutLocation, null, pathToExe, null, null);
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\autostart.txt", "false");
                string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                System.IO.File.Delete(startUpPath + "\\Secret Chat.lnk");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialogResult = System.Windows.MessageBox.Show("Möchtest Du Deinen Account wirklich löschen? Beim löschen Deines Accounts wird dein Benutzername frei, und deine Direktnachrichten, als auch alle die Du jemals gesendet hast werden gelöscht. Du musst nach dem Löschen Deines Accounts wieder einen neuen Account erstellen um Secret Chat® zu nutzen. Deine Vertifizierung , sowie alle Einstellungen werden ebenfalls zurückgesetzt. Wenn Du auf 'Ja' drückst wird Dein Account gelöscht und Secret Chat® beendet.", "ACCOUNT LÖSCHEN?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (dialogResult == MessageBoxResult.Yes)
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code;
                List<StoredUserEntity> storedUserNames = null;
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");
                    storedUserNames = usersController.GetStoredUsers(macAndUser, code);
                }
                else
                {
                    System.Windows.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var u in storedUserNames)
                {
                    if (u.storedMac == macAndUser)
                    {
                        string userName = u.PartitionKey;
                        
                        try
                        {                         
                            string Response = onlineUsersController.DeleteOnlineUser(userName, macAndUser, code);
                            if (Response != "True")
                            {
                                this.Hide();
                                System.Windows.MessageBox.Show(Response);
                                Environment.Exit(0);
                            }
                            string response = usersController.DeleteStoredUser(userName, macAndUser, code);
                            if (response != "True")
                            {
                                this.Hide();
                                System.Windows.MessageBox.Show(response);
                                Environment.Exit(0);
                            }
                        }
                        catch
                        {
                            ModernWpf.MessageBox.Show("Du wurdest aufgrund eines Fehlers nicht aus den aktiven/abwesenden Nutzern entfernt.");
                        }
                       
                    }
                }
                System.IO.File.Delete(path + "autostart.txt");
                System.IO.File.Delete(path + "notifications.txt");
                System.IO.File.Delete(path + "timer.txt");
                System.IO.File.Delete(path + "counter.txt");
                System.IO.File.Delete(path + "darkorlight.txt");
                System.IO.File.Delete(path + "goodwords.txt");
                System.IO.File.Delete(path + "icon.png");
                System.IO.File.Delete(path + "info.txt");
                System.IO.File.Delete(path + "words.txt");
                System.IO.File.Delete(path + "links.txt");
                System.IO.File.Delete(path + "code.txt");
                System.IO.File.Delete(path + "mentions.txt");
                System.IO.File.Delete(path + "sounds.txt");
                System.IO.File.Delete(path + "notificationreminder.txt");
                System.IO.File.Delete(path + "emojis.txt");
                try
                {
                    System.IO.Directory.Delete(path);
                }
                catch
                {
                    try
                    {
                        System.IO.Directory.Delete(path);
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Die gespeicherten Profilbilder konnten nicht gelöscht werden, diese können manuell über diesen Pfad entfernt werden: " + path + "ProfilePictures\\");
                    }
                }
                System.Windows.MessageBox.Show("Dein Account wurde gelöscht, Secret Chat® wird nun beendet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0);
            }
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

        public static void SaveJpeg(string path, Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");

            // Encoder parameter for image quality 
            EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
            // JPEG image codec 
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;
            img.Save(path, jpegCodec, encoderParams);
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }

        private void PictureButton_Click(object sender, RoutedEventArgs e)
        {
            if (PictureButton.Content as string == "Profilbild")
            {
                bool sucess = true;
                OpenFileDialog fileDialog = new OpenFileDialog
                {
                    Filter = "Image Files(*.PNG); (*.JPG)|*.PNG; *.JPG"/* + "|All files(*.*)|*.*"*/,
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string macAndUser = GetMac() + Environment.UserName;
                    string code ;
                    List<StoredUserEntity> storedUsers = null;
                    if (System.IO.File.Exists(path + "code.txt"))
                    {
                        code = System.IO.File.ReadAllText(path + "code.txt");
                        storedUsers = usersController.GetStoredUsers(macAndUser, code);
                    }
                    else
                    {
                        ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }              
                    long length = new System.IO.FileInfo(fileDialog.FileName).Length;
                    //Image myImage = Image.FromFile(fileDialog.FileName, true);
                    //myImage.Save(fileDialog.FileName);
                    int i = 100;
                    bool compressionNeeded = false;
                    try
                    {
                        while (length > 18000)
                        {
                            compressionNeeded = true;
                            if (System.IO.File.Exists(path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png"))
                            {
                                System.IO.File.Delete(path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png");
                            }
                            SaveJpeg(/*path + "ProfilePictures\\" + macOfDevice.Replace(":", ".") + ".png"*/ path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png", Image.FromFile(fileDialog.FileName), i);
                            length = new FileInfo(path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png").Length;
                            i -= 5;
                            Task.Delay(50);

                        }
                        if (compressionNeeded == false)
                        {
                            SaveJpeg(/*path + "ProfilePictures\\" + macOfDevice.Replace(":", ".") + ".png"*/ path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png", Image.FromFile(fileDialog.FileName), 95);
                        }

                        byte[] imageArray = System.IO.File.ReadAllBytes(path + "ProfilePictures\\" + macAndUser.Replace(":", ".") + "copy.png");
                        string base64Text = Convert.ToBase64String(imageArray);
                        foreach (var u in storedUsers)
                        {
                            if (u.storedMac == macAndUser)
                            {
                                name = u.PartitionKey;
                            }
                        }
                        if (System.IO.File.Exists(path + "code.txt"))
                        {
                            code = System.IO.File.ReadAllText(path + "code.txt");
                            string Response = usersController.InsertUser(name, macAndUser, base64Text, "true", code);
                            if (Response != "True")
                            {
                                this.Hide();
                                System.Windows.MessageBox.Show(Response);
                                Environment.Exit(0);
                            }                           
                        }
                        else
                        {
                            ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    catch
                    {
                        sucess = false;
                        ModernWpf.MessageBox.Show("Fehler; das Bild ist entweder zu groß oder ungültig. Probiere es mit dem PNG Format, sollte dies am momentanigen Bild nicht der Fall sein.");
                    }
                    if (sucess)
                    {
                        ModernWpf.MessageBox.Show("Profildbild erfolgreich geändert. Um Dein neues Profilbild sehen zu können wird Secret Chat® nun neugestartet. (Damit andere die Änderungen sehen können, müssen sie es ebenfalls neustarten.)");
                        var startInfo = Process.GetCurrentProcess().ProcessName;
                        Process.Start(startInfo, "restart");
                        Process.GetCurrentProcess().Kill();
                    }

                }
            }
            else
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code ;
                List<StoredUserEntity> storedUsers = null;
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");
                    storedUsers = usersController.GetStoredUsers(macAndUser, code);
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var u in storedUsers)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                    }
                }
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");            
                    string Response = usersController.InsertUser(name, macAndUser, "", "false", code);
                    if (Response != "True")
                    {
                        this.Hide();
                        System.Windows.MessageBox.Show(Response);
                        Environment.Exit(0);
                    }
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode("Vertifizierung wurde erfolgreich beantragt"));
                stringElements[1].AppendChild(toastXml.CreateTextNode("Sobald Du bestätigt wirst kannst Du Dein Profilbild ändern. Du wirst infomiert sobald dies geschieht."));
                new AdaptiveImage()
                {
                    Source = "Assets/newmsgbtn.png"
                };
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "icon.png";
                //var newAttribute = toastXml.CreateAttribute("placement");
                //newAttribute.Value = "appLogoOverride";
                //imageElements[0].Attributes.SetNamedItem(newAttribute);



                var toast = new ToastNotification(toastXml);
                //toast.Failed += ToastFailed;
                ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            }
        }


        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(DarkLightSlider.Value == 0)
            {
                darkOrLight = false;
            }
            else
            {
                darkOrLight = true;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (DarkLightSlider.Value == 0)
            {
                DarkLightSlider.Value = 1;
            }
            else
                DarkLightSlider.Value = 0;
            Task.Run(Sound);       
            Task.Delay(400);
            var dialogResult = ModernWpf.MessageBox.Show("Secret Chat® muss neugestartet werden, wenn Du das Design wechseln möchtest. Bist Du dir sicher?", "Neustart erforderlich", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                if(darkOrLight == true)
                {
                    System.IO.File.WriteAllText(path + "darkorlight.txt", "light");
                }
                else
                {
                    System.IO.File.WriteAllText(path + "darkorlight.txt", "dark");
                }
                var startInfo = Process.GetCurrentProcess().ProcessName;
                Process.Start(startInfo, "restart");
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                if (DarkLightSlider.Value == 0)
                {
                    DarkLightSlider.Value = 1;
                }
                else
                    DarkLightSlider.Value = 0;
            }
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = Process.GetCurrentProcess().ProcessName;
            Process.Start(startInfo, "restart");
            Process.GetCurrentProcess().Kill();
        }

        private void DefaultPicture_Click(object sender, RoutedEventArgs e)
        {
            var dialogResult = ModernWpf.MessageBox.Show("Bist Du sicher das Du Dein Profilbild entfernen möchtest?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Information);
            string vertification = "false";
            if (dialogResult == MessageBoxResult.Yes)
            {
                string code;
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string macAndUser = GetMac() + Environment.UserName;
                List<StoredUserEntity> storedUsers = null;
                if (System.IO.File.Exists(path + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "code.txt");
                    storedUsers = usersController.GetStoredUsers(macAndUser, code);
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var u in storedUsers)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                        vertification = u.Verification;
                    }
                }
                if (vertification == "true")
                {                  
                    string Response = usersController.InsertUser(name, macAndUser, "x", "true", code);
                    if (Response != "True")
                    {
                        this.Hide();
                        System.Windows.MessageBox.Show(Response);
                        Environment.Exit(0);
                    }
                }
                else
                {                  
                    string Response = usersController.InsertUser(name, macAndUser, "x", "null", code);
                    if (Response != "True")
                    {
                        this.Hide();
                        System.Windows.MessageBox.Show(Response);
                        Environment.Exit(0);
                    }
                }

                ModernWpf.MessageBox.Show("Profildbild erfolgreich zurückgesetzt. Um Dein Profilbild sehen zu können wird Secret Chat® nun neugestartet. (Damit andere die Änderungen sehen können, müssen sie es ebenfalls neustarten.)");
                var startInfo = Process.GetCurrentProcess().ProcessName;
                Process.Start(startInfo, "restart");
                Process.GetCurrentProcess().Kill();
            }
        }

        private void Links_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Links.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\links.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\links.txt", "false");
            }
        }

        private void Sounds_Click(object sender, RoutedEventArgs e)
        {
            if (Sounds.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\sounds.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\sounds.txt", "false");
            }
            Task.Run(Sound2);
        }

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat");
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            Grid.Visibility = Visibility.Visible;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Shutdown = true;
            this.Close();
        }

        private void Credits_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Mitwirkende%20&%20Anerkennungen");
        }

        private void SCProject_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat");
        }

        private void Mentions_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Mentions.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\mentions.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\mentions.txt", "false");

            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Du bekommst bei jeder neuen Nachricht im Chat eine Benachrichtigungen, wenn du weiterhin Erwähnungen angeschaltet hast, werden diese Benachrichtigungen als Erwähnungen angezeigt.");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Du bekommst nur Benachrichtigungen, wenn jemand dich erwähnt ( @[DeinName] ).");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Damit Du Benachrichtigungen empfängst, gehe bitte auf das Windows Aktions-Center (Windows logo Taste + A) und stelle den 'Fokus Assistent' auf aus.");
        }

        private void Reminder_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Reminder.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\notificationreminder.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\notificationreminder.txt", "false");

            }
        }

        private void Emojis_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Emojis.IsChecked == true)
            {
                System.IO.File.WriteAllText(path + "\\emojis.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(path + "\\emojis.txt", "false");

            }
        }
    }
}