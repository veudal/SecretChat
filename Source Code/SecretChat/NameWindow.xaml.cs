using MaterialDesignThemes.Wpf;
using SecretChat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using XamlAnimatedGif;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NameWindow : Window
    {
        bool macStored = false;
        string notRenamedName;
        readonly OnlineUsersController tableStorage2 = new OnlineUsersController();
        readonly UsersController tableStorageUsers = new UsersController();
        readonly VersionController versionController = new VersionController();
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
        }
        public NameWindow(string notRenamedName)
        {
            this.notRenamedName = notRenamedName;
            this.Hide();
            InitializeComponent();
            CenterWindowOnScreen();
            this.Topmost = true;
            Topmost = false;
            Title = "Benutzernamen festlegen";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (File.Exists(path + "\\secretchat\\darkorlight.txt"))
            {
                string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
                if (dorl == "light")
                {
                    Grid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF7F7F7"));
                    Text1.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF0083FF"));
                    closeBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
                    minimizeBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
                    helpBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
                }
            }
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                try
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                    var storedMacAdresses = tableStorageUsers.GetStoredUsers(macAndUser, code);
                    if (storedMacAdresses != null)
                    {
                        foreach (var mac in storedMacAdresses)
                        {
                            if (mac.storedMac == macAndUser)
                            {
                                macStored = true;
                            }
                        }
                        if (macStored == true)
                        {
                            TextBlocks.Visibility = Visibility.Hidden;
                            Datenschutz.Visibility = Visibility.Hidden;
                            Nutzungsbedingungen.Visibility = Visibility.Hidden;
                        }
                    }
                    macStored = false;
                }
                catch
                {
                    macStored = false;
                }
            }
            else
            {
                ModernWpf.MessageBox.Show("Gebe deinen Zugangs-Code im folgenden Fenster bitte ein um auf Secret Chat zuzugreifen", "Authentifizierungscode wurde nicht gefunden", MessageBoxButton.OK,MessageBoxImage.Error);
                return;
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

        private void NamedVertify_Click(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string name = nameBox.Text;
            if (nameBox.Text.Length < 16)
            {
                if (nameBox.Text.Length > 1)
                {
                    if (Regex.IsMatch(nameBox.Text, @"^[a-zA-Z0-9_]+$"))
                    {
                        try
                        {
                            bool alreadyStored = false;
                            string macAndUser = GetMac() + Environment.UserName;
                            string code = codeBox.Text;
                            List<StoredUserEntity> storedUserNames = null;
                            code = codeBox.Text;
                            bool hasAccess = false;
                            if (notRenamedName != null)
                            {
                                var testAccess = tableStorageUsers.GetStoredUsers(macAndUser, code);
                                if (testAccess != null)
                                    hasAccess = true;
                            }
                            else
                                hasAccess = false;

                            if (hasAccess == false)
                            {
                                try
                                {
                                    tableStorageUsers.InsertNewUser(name, macAndUser, "", null, code);
                                    var validCode = versionController.ValidInformationTest(name, macAndUser, code);
                                    if (validCode != null)
                                    {
                                        File.WriteAllText(path + "\\secretchat\\code.txt", code);
                                        SoundPlayer playSound = new SoundPlayer(SecretChat.Properties.Resources.preview);
                                        playSound.Play();
                                        var startInfo = Process.GetCurrentProcess().ProcessName;
                                        Process.Start(startInfo, "restart");
                                        Process.GetCurrentProcess().Kill();
                                    }
                                    else
                                    {
                                        ModernWpf.MessageBox.Show("Ungültige Authentifizierung, frage Armin nach einem gültigen Code und beachte das wenn Du bereits angemeldet warst, Du deinen Namen genau gleich schreiben musst wie vorher (auf klein oder großschreibung wird geachtet).");
                                        return;
                                    }

                                }
                                catch
                                {
                                    ModernWpf.MessageBox.Show("Ungültige Authentifizierung, frage Armin nach einem gültigen Code und beachte das wenn Du bereits angemeldet warst, Du deinen Namen genau gleich schreiben musst wie vorher (auf klein oder großschreibung wird geachtet).");
                                    return;
                                }
                            }
                            try
                            {
                                storedUserNames = tableStorageUsers.GetStoredUsers(macAndUser, code);
                                foreach (var v in storedUserNames)
                                {
                                    if (v.PartitionKey == nameBox.Text)
                                    {
                                        alreadyStored = true;
                                    }
                                }
                            }
                            catch
                            {
                                ModernWpf.MessageBox.Show("Ungültige Authentifizierung, frage Armin nach einem gültigen Code und beachte das wenn Du bereits angemeldet warst, Du deinen Namen genau gleich schreiben musst wie vorher (auf klein oder großschreibung wird geachtet).");
                                return;
                            }


                            if (alreadyStored == false)
                            {
                                foreach (var u in storedUserNames)
                                {
                                    if (u.storedMac == macAndUser)
                                    {
                                        string oldName = u.PartitionKey;
                                        tableStorageUsers.UpdateUser(name, macAndUser, "", null, code, oldName);
                                        tableStorageUsers.DeleteStoredUser(oldName, macAndUser, code);
                                    }
                                }
                                File.WriteAllText(path + "\\secretchat\\code.txt", code);
                                SoundPlayer playSound = new SoundPlayer(SecretChat.Properties.Resources.preview);
                                playSound.Play();
                                if (hasAccess == true)
                                {
                                    ModernWpf.MessageBox.Show("Dein Benutzername wurde erfolgreich gesetzt! Secret Chat® wird nun Neugestartet.", "Neustart erforderlich", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                var startInfo = Process.GetCurrentProcess().ProcessName;
                                Process.Start(startInfo, "restart");
                                Process.GetCurrentProcess().Kill();
                                return;
                            }
                            else
                            {
                                ModernWpf.MessageBox.Show("Dein Name ist schon vergeben!");
                                return;
                            }
                        }
                        catch
                        {
                            ModernWpf.MessageBox.Show("Ungültige Authentifizierung, frage Armin nach einem gültigen Code und beachte das wenn Du bereits angemeldet warst, Du deinen Namen genau gleich schreiben musst wie vorher (auf klein oder großschreibung wird geachtet).");
                            return;
                        }


                    }
                    else
                    {
                        ModernWpf.MessageBox.Show("Dein Name darf nur Buchstaben, Zahlen und unterstriche enthalten!");
                    }
                }
                else
                    ModernWpf.MessageBox.Show("Dein Name muss mindestens aus zwei Zeichen bestehen!");
            }
            else
                ModernWpf.MessageBox.Show("Dein Name darf nicht über 15 Zeichen lang sein!");


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Datenschutzerkl%C3%A4rung");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
             Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Nutzungsbediungen");
        }

        private void Datenschutz_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            DatenschutzerklärungTextBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF4993FF"));
            DatenschutzerklärungTextBlock.TextDecorations = TextDecorations.Underline;
        }

        private void Datenschutz_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            DatenschutzerklärungTextBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF1D3DEE"));
            DatenschutzerklärungTextBlock.TextDecorations = null;

        }

        private void Nutzungsbedingungen_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            NutzungsbedingungenTextBlock.TextDecorations = TextDecorations.Underline;
            NutzungsbedingungenTextBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF4993FF"));
        }

        private void Nutzungsbedingungen_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            DatenschutzerklärungTextBlock.TextDecorations = null;
            NutzungsbedingungenTextBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF1D3DEE"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (File.Exists(path + "\\secretchat\\code.txt"))
            {
                codeBox.Text = File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            //this.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
        }

        private void minimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void helpBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat");
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            Grid.Visibility = Visibility.Visible;
        }

        private void codeBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string text = codeBox.Text;
            if (text.Length < 7)
            {
                try
                {
                    Convert.ToInt32(codeBox.Text);
                }
                catch
                {
                    try
                    {
                        text = text.Remove(text.Length - 1);
                        codeBox.Text = text;
                        codeBox.Text = codeBox.Text.Replace(" ", "");
                        codeBox.CaretIndex = text.Length;
                        codeBox.UpdateLayout();
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                try
                {
                    text = text.Substring(0, 6);
                    codeBox.Text = text;
                    codeBox.Text = codeBox.Text.Replace(" ", "");
                    codeBox.CaretIndex = text.Length;
                    codeBox.UpdateLayout();
                }
                catch
                {

                }
            }


        }


        //private void Window_StateChanged(object sender, EventArgs e)
        //{
        //        WindowState = WindowState.Maximized;
        //}
    }
}
