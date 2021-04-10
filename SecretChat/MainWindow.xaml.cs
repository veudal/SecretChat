using IWshRuntimeLibrary;
using SecretChat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Windows.UI.Notifications;
using System.Net.NetworkInformation;
using Brush = System.Windows.Media.Brush;

namespace SecretChat
{
    public partial class MainWindow : Window
    {
                            
        readonly string version = "1.9";
        readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string name;
        string timer;
        string ErrorInfo;
        readonly string startInfo;
        int oneTwoThree = 1;
        int InfoPosition = 0;
        int unreadedMessages = 0;
        int ProgrammActive = 0;
        bool collapsed = false;
        bool defaultStartSelection = true;
        bool newMessagesItem = false;
        bool timerElapsed = false;
        bool finishedLoop4FirstTime = false;
        bool crash = false;
        bool isUserOnline;
        bool defaultSelection = true;
        bool test = false;
        readonly bool textChanged = false;
        readonly ToolTip tooltip = new ToolTip();
        public System.Timers.Timer closeToolTipTimer = new System.Timers.Timer(1500);
        public System.Timers.Timer lastKeyPressedTimer = new System.Timers.Timer(1000);
        List<StoredUserEntity> storedUsers;
        readonly MessagesModel messagesModel = new MessagesModel();
        readonly VersionModel versionModel = new VersionModel();
        MessageController messageController;
        readonly OnlineUsersController onlineUsersController = new OnlineUsersController();
        readonly UsersController tableStorageUsers = new UsersController();
        readonly TrayContextMenu trayContextMenu = new TrayContextMenu();

        public MainWindow(string startInfo)
        { 
            InitializeComponent();
            this.startInfo = startInfo;
            //this.Icon = new BitmapImage(new Uri("pack://application:,,,/resources/icon.ico"));

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

        private async Task Refresher()
        {
            try
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                    Environment.Exit(0);
                }
                List<OnlineUserEntity> absentUsers = onlineUsersController.GetAllAbsentUsers(name, macAndUser, code); ;
                VerificationToast(macAndUser);
                AppIsActive(absentUsers);
                while (true)
                {
                    await KillOtherInstance();
                        absentUsers = onlineUsersController.GetAllAbsentUsers(name, macAndUser, code);
                        CheckIfUserIsOnline();
                        foreach (var item in absentUsers)
                        {
                            item.AbsentUser = storedUsers.SingleOrDefault(absentUser => absentUser.PartitionKey == item.userName);
                        }
                        await Dispatcher.BeginInvoke((Action)(() =>
                        {
                            OnlineUsers_Copy.Items.Refresh();
                            OnlineUsers_Copy.ItemsSource = absentUsers;
                            OnlineUsers_Copy.Items.Refresh();
                        }));             
                    await NewMessagesHandler();
                    //foreach (var u in storedUsers)
                    //{
                    //    if (u.storedMac == macAndUser)
                    //    {
                    //        name = u.storedUserName;

                    //    }
                    //    if (System.IO.File.Exists(path + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png"))
                    //    {
                    //        if (u.Picture == null || u.Picture == "")
                    //        {
                    //            u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    //        }
                    //        else
                    //        {
                    //            u.Picture = path + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png";
                    //        }
                    //    }
                    //    else
                    //    {
                    //        u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    //    }

                    //}

                    if (ProgrammActive > 50000)
                    {
                        VerificationToast(macAndUser);
                        AppIsActive(absentUsers);
                        ProgrammActive = 0;
                    }


                    finishedLoop4FirstTime = true;
                    timer = System.IO.File.ReadAllText(path + "\\SecretChat\\timer.txt");
                    if (ApplicationIsActivated() == true)
                    {
                        ProgrammActive += Convert.ToInt32(timer);
                        await Task.Delay(Convert.ToInt32(timer));
                    }
                    else
                    {
                        ProgrammActive += 5000;
                        await Task.Delay(5000);
                    }
                }

            }
            catch (Exception e)
            {
                if (IsConnectedToInternet() == true)
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    crash = true;
                    ErrorInfo = "Entweder sind unsere Server momentan nicht verfügbar oder ein unbekannter Fehler ist aufgetreten. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                }
                else
                {
                    await Dispatcher.BeginInvoke((Action)(() => WindowState = WindowState.Minimized));
                    ModernWpf.MessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal.");

                }
            }
        }

        private void CheckIfUserIsOnline()
        {
            if (isUserOnline == true)
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                    Environment.Exit(0);
                }
                onlineUsersController.InsertOnlineUser(name, "online", null, macAndUser, code);
            }
        }

        private async Task NewMessagesHandler()
        {
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                Environment.Exit(0);
            }
            var storageList = messageController.GetMessages(name, macAndUser, code);
            var sortedList = storageList.OrderBy(s => s.Timestamp);
            foreach (var item in sortedList)
            {
                SetColorsForItems(item);
                if (!messagesModel.Messages.Any(m => m.MessageID == item.MessageID))
                {
                    await AddNewMessagesItem(item);
                    item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.From);

                    messagesModel.Messages.Add(item);
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (item.From != name)
                        {

                            unreadedMessages++;
                            IconOverlay();
                            NewMsgBtnOverlay();
                            if (ApplicationIsActivated() != true)
                            {
                                if (finishedLoop4FirstTime == true)
                                {
                                    FlashWindow.Start(this);
                                    if (Regex.IsMatch(item.Message, "@" + name, RegexOptions.IgnoreCase))
                                    { 
                                        if (System.IO.File.ReadAllText(path + "\\secretchat\\mentions.txt") == "true")
                                        {
                                            SendMentionNotification(item);
                                        }
                                        else if (System.IO.File.ReadAllText(path + "\\secretchat\\notifications.txt") == "true")
                                        {
                                            SendNotification(item);
                                        }
                                    }
                                    else if (System.IO.File.ReadAllText(path + "\\secretchat\\notifications.txt") == "true")
                                    {
                                        SendNotification(item);
                                    }
                                }
                            }
                            NewMessagesGrid.Visibility = Visibility.Visible;
                            list.Items.Refresh();
                        }
                        else
                        {
                            defaultSelection = true;
                            NewMessagesGrid.Visibility = Visibility.Hidden;
                            list.Items.Refresh();
                            if (test == true)
                            {
                                list.SelectedIndex = list.Items.Count - 1;
                                list.ScrollIntoView(list.SelectedItem);
                                list.UnselectAll();
                            }
                                defaultSelection = false;
                        }

                    }));


                }

            }
        }

        private void AppIsActive(List<OnlineUserEntity> absentUsers)
        {
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                Environment.Exit(0);
            }
            foreach (var absent in absentUsers)
            {
                if (absent.userName == name)
                {
                    onlineUsersController.InsertOnlineUser(name, "absent", DateTime.UtcNow.AddHours(-2), macAndUser, code);

                }
                absent.AbsentUser = storedUsers.SingleOrDefault(user => user.PartitionKey == absent.userName);
            }
            var onlineUsers = onlineUsersController.GetAllOnlineUsers(name, macAndUser, code);
            foreach (var online in onlineUsers)
            {
                if (online.userName == name)
                {
                    onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow.AddHours(-2), macAndUser, code);

                }
                online.User = storedUsers.SingleOrDefault(user => user.PartitionKey == online.userName);
            }
        }

        private void VerificationToast(string macAndUser)
        {
            foreach (var u in storedUsers)
            {
                if (u.storedMac == macAndUser)
                {
                    if (u.Verification == "true")
                    {

                        if (System.IO.File.ReadAllText(path + "\\secretchat\\info.txt") == "false")
                        {
                            System.IO.File.WriteAllText(path + "\\secretchat\\info.txt", "true");
                            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

                            var stringElements = toastXml.GetElementsByTagName("text");
                            stringElements[0].AppendChild(toastXml.CreateTextNode("Dein Vertifizierungsantrag wurde angemommen!"));
                            stringElements[1].AppendChild(toastXml.CreateTextNode("Du bist nun vertifiziert, Du kannst nun Dein Profilbild ändern"));
                            var imageElements = toastXml.GetElementsByTagName("image");
                            imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";
                            //var newAttribute = toastXml.CreateAttribute("placement");
                            //newAttribute.Value = "appLogoOverride";
                            //imageElements[0].Attributes.SetNamedItem(newAttribute);
                            var toast = new ToastNotification(toastXml);
                            //toast.Failed += ToastFailed;
                            string appId = "Secret Chat";
                            ToastNotificationManager.CreateToastNotifier(appId).Show(toast);
                            toast.Activated += VertificationToastActivated;
                        }
                    }
                    else
                    {
                        System.IO.File.WriteAllText(path + "\\secretchat\\info.txt", "false");
                    }
                }

            }
        }

        private async Task KillOtherInstance()
        {
            Process[] processlist = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            foreach (var p in processlist)
            {
                var test = Process.GetCurrentProcess().SessionId;
                if (p.SessionId == Process.GetCurrentProcess().SessionId)
                {
                    if (p.ProcessName == Process.GetCurrentProcess().ProcessName)
                    {
                        if (p.Id != Process.GetCurrentProcess().Id)
                        {
                            p.Kill();
                            await Dispatcher.BeginInvoke((Action)(() => this.Show()));
                            await Dispatcher.BeginInvoke((Action)(() => this.ShowInTaskbar = true));
                            await Dispatcher.BeginInvoke((Action)(() => this.Activate()));
                            await Dispatcher.BeginInvoke((Action)(() => WindowState = WindowState.Maximized));
                        }
                    }
                }
            }
        }

        private async Task AddNewMessagesItem(MessageEntity item)
        {
            if (item.From != name)
            {
                if (newMessagesItem == false)
                {
                    newMessagesItem = true;
                    MessageEntity messageEntity = new MessageEntity
                    {
                        Message = Environment.NewLine + "______________________________________________Neue Nachrichten______________________________________________",
                        TextColor2 = "BlueViolet",
                        MessageOrInfo = "Collapsed"
                    };
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (WindowState == WindowState.Maximized)
                            messageEntity.LeftOrCenter = "Right";
                        else
                            messageEntity.LeftOrCenter = "Left";
                        messagesModel.Messages.Add(messageEntity);
                        list.Items.Refresh();
                    }));
                    InfoPosition = messagesModel.Messages.Count;
                }
            }
        }

        private async Task WhoIsTyping()
        {
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                Environment.Exit(0);
            }
            while (true)
            {
                
                    var onlineUsers = onlineUsersController.GetAllOnlineUsers(name, macAndUser, code);
                    List<OnlineUserEntity> typing = null;

                    foreach (var item in onlineUsers)
                    {
                        item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.userName);
                        if (item.userName == name)
                        {
                            item.Visibility = "Collapsed";
                        }
                        else
                        {
                            if (item.Time != null)
                            {
                                if (oneTwoThree == 1)
                                {
                                    item.Dot1Font = "UltraBold";
                                    item.Dot2Font = "Normal";
                                    item.Dot3Font = "Normal";
                                }
                                if (oneTwoThree == 2)
                                {
                                    item.Dot1Font = "Normal";
                                    item.Dot2Font = "UltraBold";
                                    item.Dot3Font = "Normal";
                                }
                                if (oneTwoThree == 3)
                                {
                                    item.Dot1Font = "Normal";
                                    item.Dot2Font = "Normal";
                                    item.Dot3Font = "UltraBold";
                                }
                                if (oneTwoThree < 3)
                                    oneTwoThree++;
                                else
                                    oneTwoThree = 1;

                                if (item.Time.HasValue && item.Time.Value.CompareTo(DateTime.UtcNow.AddSeconds(-2)) >= 0)
                                    item.IsTyping = true;
                                else
                                    item.IsTyping = false;

                            }
                        }
                    }
                    typing = onlineUsers.Where(u => u.IsTyping == true).ToList();
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        Typing.ItemsSource = typing;
                        OnlineUsers.ItemsSource = onlineUsers;
                    }));
                if(ApplicationIsActivated() == true)
                    await Task.Delay(200);
                else
                    await Task.Delay(3000);

            }
        }

        //private async Task StillActive()
        //{
        //try
        //{
        //    string macAndUser = GetMac() + Environment.UserName;
        //    while (true)
        //    {
        //        var storedUsers = tableStorageUsers.GetAllStoredUsers();

        //        await Task.Delay(59000);

        //    }
        //}
        //catch (Exception e)
        //{
        //    // Get stack trace for the exception with source file information
        //    var st = new StackTrace(e, true);
        //    // Get the top stack frame
        //    var frame = st.GetFrame(0);
        //    // Get the line number from the stack frame
        //    var line = frame.GetFileLineNumber();
        //    crash = true;
        //    ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an Dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
        //    //ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
        //}
        //}

        private void SendNotification(MessageEntity item)
        {

            //var cb = new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder();
            //cb.AddText("test");
            //cb.SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short);
            //var toastContent = cb.GetToastContent();

            //var xDoc = toastContent.GetXml();
            //var bindingElements = xDoc.GetElementsByTagName("binding");
            //bindingElements[0].Attributes.GetNamedItem("template").NodeValue = "ToastImageAndText02";
            //var toast = new Windows.UI.Notifications.ToastNotification(xDoc);
            //toast.Activated += ToastActivated;
            //toast.Failed += ToastFailed;
            //ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);


            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(item.From));
            stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
            var imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";
            //var newAttribute = toastXml.CreateAttribute("placement");
            //newAttribute.Value = "appLogoOverride";
            //imageElements[0].Attributes.SetNamedItem(newAttribute);
            var toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            //toast.Failed += ToastFailed;
            ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
        }

        private void SendMentionNotification(MessageEntity item)
        {

            //var cb = new Microsoft.Toolkit.Uwp.Notifications.ToastContentBuilder();
            //cb.AddText("test");
            //cb.SetToastDuration(Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short);
            //var toastContent = cb.GetToastContent();

            //var xDoc = toastContent.GetXml();
            //var bindingElements = xDoc.GetElementsByTagName("binding");
            //bindingElements[0].Attributes.GetNamedItem("template").NodeValue = "ToastImageAndText02";
            //var toast = new Windows.UI.Notifications.ToastNotification(xDoc);
            //toast.Activated += ToastActivated;
            //toast.Failed += ToastFailed;
            //ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);


            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(item.From + " hat dich erwähnt: "));
            stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
            var imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";
            //var newAttribute = toastXml.CreateAttribute("placement");
            //newAttribute.Value = "appLogoOverride";
            //imageElements[0].Attributes.SetNamedItem(newAttribute);
            var toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            //toast.Failed += ToastFailed;
            ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
        }

        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;
            }
            var procId = Process.GetCurrentProcess().Id;
            GetWindowThreadProcessId(activatedHandle, out int activeProcId);

            return activeProcId == procId;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);



        private void ToastFailed(ToastFailedEventArgs args)
        {
            ModernWpf.MessageBox.Show("Benachrichtigung konnte nicht gesendet werden: " + args.ToString() + args.ErrorCode);
        }

        private void ToastActivated(ToastNotification sender, object args)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                unreadedMessages = 0;
                NewMsgBtnOverlay();
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Topmost = true;
                Topmost = false;
                this.Show();
                this.Activate();
                ShowInTaskbar = true;
                defaultSelection = true;
                NewMessagesGrid.Visibility = Visibility.Hidden;
                list.Items.Refresh();
                list.SelectedIndex = list.Items.Count - 1;
                list.ScrollIntoView(list.SelectedItem);
                list.UnselectAll();
                defaultSelection = false;
                IconOverlay();
            }));
        }

        private void VertificationToastActivated(ToastNotification sender, object args)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                Topmost = true;
                Topmost = false;
                this.Show();
                this.Activate();
                ShowInTaskbar = true;
                Task.Run(Sound);
                var settings = new Settings(this);
                settings.ShowDialog();

            }));
        }

        private DateTime _lastMessageSentAt = DateTime.Now.AddMinutes(-1);

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()  
        {
            try
            {
                unreadedMessages = 0;
                NewMsgBtnOverlay();
                IconOverlay();
                defaultSelection = true;
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                    Environment.Exit(0);
                }
                onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow.AddHours(-2), macAndUser, code);
                if (System.IO.File.Exists(path + "\\SecretChat\\counter.txt"))
                {
                    try
                    {
                        int count = Convert.ToInt32(System.IO.File.ReadAllText(path + "\\SecretChat\\counter.txt"));
                        count++;
                        System.IO.File.WriteAllText(path + "\\SecretChat\\counter.txt", Convert.ToString(count));
                    }
                    catch
                    {
                        System.IO.File.WriteAllText(path + "\\SecretChat\\counter.txt", "1");
                        ModernWpf.MessageBox.Show("Der Nachrichten Zähler wurde manipuliert. Es wird nun von vorne gezählt wieviele Nachirchten Du geschrieben hast.");
                    }
                }
                else
                {
                    System.IO.File.WriteAllText(path + "\\SecretChat\\counter.txt", "1");
                }
                string[] words = System.IO.File.ReadAllLines(path + "\\secretchat\\words.txt");
                string[] goodWords = System.IO.File.ReadAllLines(path + "\\secretchat\\goodWords.txt");
                string text = chatbox.Text;
                for (int i = 0; i < words.Length; i++)
                {
                    if (i == 1 || i == 2 || i == 3 || i == 4 || i == 6 || i == 7 || i == 12 || i == 14 || i == 16 || i == 19 || i == 20 || i == 22 || i == 25 || i == 26 || i == 30 || i == 31 || i == 33 || i == 36 || i == 34 || i == 37 || i == 38 || i == 42)
                        text = Regex.Replace(text, Encrypt.DecryptString(words[i], "gl2yz4Gf0KzQyk"), goodWords[i], RegexOptions.IgnoreCase);
                    else
                        text = Regex.Replace(text, @"\b" + Encrypt.DecryptString(words[i], "gl2yz4Gf0KzQyk") + @"\b", goodWords[i], RegexOptions.IgnoreCase);
                }
                text = text.Replace(">:(", "😠");
                text = text.Replace(":)", "🙂");
                text = text.Replace(";)", "😉");
                text = text.Replace(":P", "😛");
                text = text.Replace(";P", "😜");
                text = text.Replace(":(", "🙁");
                text = text.Replace("):", "🙁");
                text = text.Replace(";(", "😢");
                text = text.Replace(":(", "🙁");
                text = text.Replace("<3", "❤");
                text = text.Replace(" :/", "😕");
                text = text.Replace(":O", "😮");
                text = text.Replace("-_-", "😑");
                text = text.Replace("._.", "😐");
                text = text.Replace("O_O", "😲");
                text = text.Replace("(:", "🙃");
                text = text.Replace("xD", "😆");
                text = text.Replace(":D", "😀");
                if (IsAllUpper(text))
                {
                    text = text.ToLower();
                }
                bool blocked = false;
                sendButton.Visibility = Visibility.Collapsed;
                MessageController tableStorage = new MessageController();
                if (_lastMessageSentAt.CompareTo(DateTime.Now.AddSeconds(-1.5)) >= 0)
                {
                    ModernWpf.MessageBox.Show("Bitte warte eine Sekunde bevor Du eine Nachricht sendest");
                    return;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.Length < 2000)
                    {
                        if (chatbox.LineCount < 21)
                        {
                            Task.Run(Sound);
                            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                            {
                                var storedUsers = tableStorageUsers.GetStoredUsers( macAndUser, code);
                                foreach (var u in storedUsers)
                                {
                                    if (u.storedMac == macAndUser)
                                    {
                                        name = u.PartitionKey;
                                        if (u.Blocked == true)
                                        {
                                            blocked = true;
                                        }
                                    }
                                }
                                if (blocked == false)
                                {
                                    chatbox.Clear();
                                    if (newMessagesItem == true)
                                    {
                                        if (InfoPosition > 0)
                                        {
                                            messagesModel.Messages.RemoveAt(InfoPosition - 1);
                                            list.Items.Refresh();
                                            newMessagesItem = false;
                                        }
                                    }
                                    tableStorage.InsertMessage(Convert.ToString(Guid.NewGuid()), name, text, name, macAndUser, code);
                                    _lastMessageSentAt = DateTime.Now;
                                    //chatbox.AcceptsReturn = true;
                                }
                                else
                                {
                                    ModernWpf.MessageBox.Show("Du wurdest von einem Administrator blockiert. Der Grund sollte DIR bekannt sein. Höchstwahrscheinlich kannst du innerhalb der nächsten 24 Stunden wieder Nachrichten schreiben.");
                                }
                            }
                            else
                            {
                                ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                        else
                            ModernWpf.MessageBox.Show("Deine Nachricht darf nicht mehr als 20 Zeilen beinhalten. Aktuelle Zeichen: " + chatbox.LineCount);
                    }
                    else
                        ModernWpf.MessageBox.Show("Deine Nachricht darf nicht über 2000 Zeichen lang sein! Aktuelle Zeichen: " + text.Length);

                }
            }
            catch (Exception e)
            {
                if (IsConnectedToInternet() == true)
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    crash = true;
                    ErrorInfo = "Unsere Server sind momentan leider nicht verfügbar. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() => WindowState = WindowState.Minimized));
                    MessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal.");

                }
            }
        }

        bool IsAllUpper(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
                    return false;
            }
            return true;
        }

        private void Sound()
        {
            if (System.IO.File.ReadAllText(path + "\\secretchat\\sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.preview);
                playSound.Play();
            }
        }


        private void List_Loaded(object sender, RoutedEventArgs e)
        {
            //Application.Current.MainWindow.Height = 421;
            //Thread.Sleep(50);
            //WindowState = WindowState.Maximized;
        }

        private void Chatbox_KeyUp(object sender, KeyEventArgs e)
        {
            SetSendButtonVisibility(sender, e);
            chatbox.AcceptsReturn = false;
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                chatbox.AcceptsReturn = true;
            }
        }

        private void Chatbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                chatbox.AcceptsReturn = true;
            }
            if (e.Key == Key.Enter)
            {
                if (!Keyboard.IsKeyDown(Key.LeftShift))
                {
                    if (!String.IsNullOrWhiteSpace(chatbox.Text))
                    {
                        chatbox.AcceptsReturn = false;
                        SendMessage();
                    }
                }
                else
                    chatbox.AcceptsReturn = true;
            }
            if (e.Key != Key.Back)
            {
                if (timerElapsed == true)
                {
                    Task.Run(InsertOnlineUser);
                    timerElapsed = false;
                    lastKeyPressedTimer.Stop();
                    lastKeyPressedTimer.Start();
                }
            }



        }

        private void InsertOnlineUser()
        {
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                Environment.Exit(0);
            }
            onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow, macAndUser, code);
        }

        private void SetSendButtonVisibility(object sender, KeyEventArgs e)
        {
            string message = chatbox.Text;
            if (string.IsNullOrEmpty(message))
            {
                sendButton.Visibility = Visibility.Hidden;
            }
            else
            {
                sendButton.Visibility = Visibility.Visible;
            }
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ShowInTaskbar = false;
            this.Hide();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            var settings = new Settings(this);
            settings.ShowDialog();
            if(settings.Shutdown == true)
            {
                SystemTray.Dispose();
                Environment.Exit(0);
            }
        }

        private void Chatbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (defaultStartSelection == false)
            {
                if (System.IO.File.ReadAllText(path + "\\secretchat\\sounds.txt") == "true")
                {
                    SoundPlayer playSound = new SoundPlayer(SecretChat.Properties.Resources.mixkit_fast_double_click_on_mouse_275);
                    playSound.Play();
                }
            }
            else
            {
                defaultStartSelection = false;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            unreadedMessages = 0;
            NewMsgBtnOverlay();
            if (newMessagesItem == true)
            {
                if (InfoPosition > 0)
                {
                    messagesModel.Messages.RemoveAt(InfoPosition - 1);
                    list.Items.Refresh();
                    newMessagesItem = false;
                }
            }
            try
            {
                FlashWindow.Stop(this);
            }
            catch
            {

            }
            defaultSelection = true;
            list.Items.Refresh();
            list.SelectedIndex = list.Items.Count - 1;
            list.ScrollIntoView(list.SelectedItem);
            list.UnselectAll();
            defaultSelection = false;
            IconOverlay();
            Task.Run(Sound);
            NewMessagesGrid.Visibility = Visibility.Hidden;
        }


        private void Window_Activated_1(object sender, EventArgs e)
        {
            FlashWindow.Stop(this);
            string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
            if (dorl == "light")
            {
                Chat.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF7F7F7"));
            }
            else
            {
                Chat.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            }
            isUserOnline = true;

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            isUserOnline = false;
            string macAndUser = GetMac() + Environment.UserName;
            string code = null;
            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
            {
                code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
            }
            else
            {
                ModernWpf.MessageBox.Show("Ungültiger Zugangs-Code, frage Armin nach einem gültigen Code.");
                Environment.Exit(0);
            }
            onlineUsersController.InsertOnlineUser(name, "absent", DateTime.UtcNow.AddHours(-2), macAndUser, code);
        }

        public void SecretChat_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            this.Show();
            Topmost = true;
            Topmost = false;
            ShowInTaskbar = true;
        }

        private void SecretChat_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            if (startInfo != "false")
            {
                if (trayContextMenu.IsVisible)
                {
                    trayContextMenu.Hide();
                }
                trayContextMenu.Show();
                trayContextMenu.Activate();
                Task.Delay(100);
                Task.Run(TrayHandler);

            }
        }

        private async Task TrayHandler()
        {
            while (trayContextMenu.ernc == "n")
            {
                await Task.Delay(100);
            }

            if (trayContextMenu.ernc == "e")
            {
                await Task.Delay(100);
                await Dispatcher.BeginInvoke((Action)(() => SystemTray.Dispose()));
                Environment.Exit(0);
            }

            if (trayContextMenu.ernc == "r")
            {
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    SystemTray.Dispose();
                    var startInfo = Process.GetCurrentProcess().ProcessName;
                    Process.Start(startInfo, "restart");
                    Process.GetCurrentProcess().Kill();
                }));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
            defaultSelection = true;
            list.Items.Refresh();
            list.SelectedIndex = list.Items.Count - 1;
            list.ScrollIntoView(list.SelectedItem);
            list.UnselectAll();
            defaultSelection = false;
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
        }


        private void CreateShortcut(string version)
        {
            string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutLocation = System.IO.Path.Combine(desktop, "Secret Chat" + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
            shortcut.Description = "SecretChat " + version;
            shortcut.TargetPath = pathToExe;
            shortcut.Save();
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


        private void StartAndPrepareAsync(string startInfo)
        {
            if (startInfo != "false")
            {
                //int t = 0;
                //string i = "jojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojo";
                //while (t < 22)
                //{
                //    try
                //    {
                //        System.IO.File.AppendAllText(path + "\\Secretchat\\tttest.txt", Convert.ToString(i));
                //        i = i + i;
                //        t++;
                //    }
                //    catch
                //    { 
                //    }
                //}
                sendButton.Visibility = Visibility.Hidden;
                NewMessagesGrid.Visibility = Visibility.Hidden;
                this.StateChanged += new EventHandler(Window1_StateChanged);
                list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, System.Windows.Controls.ScrollBarVisibility.Hidden);
                this.Topmost = true;
                tooltip.Visibility = Visibility.Hidden;
                ShowInTaskbar = true;
                SystemTray.Visibility = Visibility.Visible;
                if (System.IO.File.ReadAllText(path + "\\secretchat\\autostart.txt") != "false")
                {
                    string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                    string shortcutLocation = System.IO.Path.Combine(startUpPath, "Secret Chat" + ".lnk");
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                    shortcut.Description = "SecretChat " + version;
                    shortcut.TargetPath = pathToExe;
                    shortcut.Save();
                }
                if (Convert.ToInt32(System.IO.File.ReadAllText(path + "\\SecretChat\\timer.txt")) < 200 || Convert.ToInt32(System.IO.File.ReadAllText(path + "\\SecretChat\\timer.txt")) > 2000)
                {
                    System.IO.File.WriteAllText(path + "\\SecretChat\\timer.txt", "500");
                }
                string pathForShortcut = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + "\\Secret Chat.lnk";
                if (System.IO.File.Exists(pathForShortcut))
                {
                    CreateShortcut(version);
                }
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                    try
                    {
                        storedUsers = tableStorageUsers.GetStoredUsers(macAndUser, code);
                    }

                    catch (Exception e)
                    {
                        if (IsConnectedToInternet() == true)
                        {
                            var st = new StackTrace(e, true);
                            var frame = st.GetFrame(0);
                            var line = frame.GetFileLineNumber();
                            crash = true;
                            ErrorInfo = "Unsere Server sind momentan leider nicht verfügbar. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                        }
                        else
                        {
                            Dispatcher.BeginInvoke((Action)(() => WindowState = WindowState.Minimized));
                            ModernWpf.MessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal.");

                        }
                    }
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                WindowState = WindowState.Maximized;
                Title = "Secret Chat   v" + version;
                messageController = new MessageController();
                list.Items.Refresh();
                list.ItemsSource = messagesModel.Messages;
                OnlineUsers.Items.Refresh();               
                foreach (var u in storedUsers)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                    }
                    if (!string.IsNullOrEmpty(u.Picture))
                    {
                        if (u.Picture != "x")
                        {
                            string base64String = u.Picture;
                            byte[] imgBytes = Convert.FromBase64String(base64String);

                            BitmapImage bitmapImage = new BitmapImage();
                            MemoryStream ms = new MemoryStream(imgBytes);
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();
                            using (var imageFile = new FileStream(path + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png", FileMode.Create))
                            {
                                imageFile.Write(imgBytes, 0, imgBytes.Length);
                                imageFile.Flush();
                                imageFile.Close();
                            }
                        }

                    }
                    if (u.storedMac != null)
                    {
                        if (System.IO.File.Exists(path + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png"))
                        {
                            if (!string.IsNullOrEmpty(u.Picture))
                                u.Picture = path + "\\SecretChat\\ProfilePictures\\" + u.storedMac.Replace(":", ".") + ".png";
                            else
                                u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                        }
                        else
                        {
                            u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                        }
                    }
                }
                var storageList = messageController.GetAllMessages(name, macAndUser, code);
                var sortedList = storageList.OrderBy(s => s.Timestamp);
            
                foreach (var item in sortedList)
                {                  
                    SetColorsForItems(item);
                    item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.From);
                    list.Items.Refresh();
                    messagesModel.Messages.Add(item);
                    list.Items.Refresh();
                }
                try
                {
                    onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow.AddHours(-2), macAndUser, code);
                }
                catch
                {
                    ModernWpf.MessageBox.Show("Du konntest nicht in die Aktiven Nutzern hinzugefügt werden aufgrund eines Fehler entfernt. Höchstwahrscheinlich warst Du vorher schon durch einen Fehler noch in der Liste.");
                }
                lastKeyPressedTimer.Start();
                lastKeyPressedTimer.Elapsed += LastKeyPressedTimer_Elapsed;
                closeToolTipTimer.Elapsed += CloseToolTipTimer_Elapsed;
                Task.Run(Refresher);
                Task.Run(WhoIsTyping);
                chatbox.Focus();
                chatbox.SelectionLength = 0;
                Topmost = false;
                this.Show();
                this.Activate();
            }
        }

        private void SetColorsForItems(MessageEntity item)
        {
            bool isUri = false;
            if (item.Message.StartsWith("https://"))
            {
                isUri = Uri.IsWellFormedUriString(item.Message, UriKind.Absolute);
            }
            else if (item.Message.StartsWith("http://"))
            {
                isUri = Uri.IsWellFormedUriString(item.Message, UriKind.Absolute);
            }
            if (item.From == name)
            {

                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
                if (dorl == "light")
                {
                    item.TextColor = "blue";
                    item.TextColor2 = "blue";
                    if (isUri == true)
                    {
                        item.UnderlinedOrNot = "Underline";
                        item.TextColor2 = "#FF269FF4";
                    }
                }
                else
                {
                    item.TextColor2 = "lightblue";
                    item.TextColor = "lightblue";
                    if (isUri == true)
                    {
                        item.TextColor2 = "#FF269FF4";
                        item.UnderlinedOrNot = "Underline";
                    }
                }
            }
            else
            {
                if (isUri == true)
                {
                    item.TextColor2 = "#FF269FF4";
                    item.UnderlinedOrNot = "Underline";
                }
            }
            if (Regex.IsMatch(item.Message, "@" + name, RegexOptions.IgnoreCase))
            {
                item.TextColor2 = "#FF269FF3";
                item.UnderlinedOrNot = "None";
            }
        }

        private void NewMsgBtnOverlay()
        {
            if (unreadedMessages != 0)
            {
                if (unreadedMessages > 9)
                {
                    NotificationAmount.Text = "9+";
                }
                else
                {
                    NotificationAmount.Text = Convert.ToString(unreadedMessages);
                }
            }
            else
            {
                NotificationAmount.Text = "";
            }
        }

        private void IconOverlay()
        {
            this.TaskbarItemInfo = new TaskbarItemInfo();
            if (unreadedMessages != 0)
            {
                if (unreadedMessages > 9)
                {
                    Bitmap bitmap = new Bitmap(128, 128, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    bitmap.MakeTransparent();
                    Graphics g = Graphics.FromImage(bitmap);
                    g.FillEllipse(System.Drawing.Brushes.OrangeRed, 5, 0, 55, 55);
                    g.DrawString("9+", new Font("Arial", 25), System.Drawing.Brushes.White, new PointF(7, 9));
                    IntPtr hBitmap = bitmap.GetHbitmap();
                    ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    TaskbarItemInfo.Overlay = wpfBitmap;
                }
                else
                {
                    Bitmap bitmap = new Bitmap(60, 60, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    bitmap.MakeTransparent();
                    Graphics g = Graphics.FromImage(bitmap);
                    g.FillEllipse(System.Drawing.Brushes.OrangeRed, 10, 5, 50, 50);
                    g.DrawString(Convert.ToString(unreadedMessages), new Font("Arial", 25), System.Drawing.Brushes.White, new PointF(17, 10));
                    IntPtr hBitmap = bitmap.GetHbitmap();
                    ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    TaskbarItemInfo.Overlay = wpfBitmap;
                }
            }
            else
            {
                TaskbarItemInfo.Overlay = null;
            }
        }

        private void DtGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0)
            {
                if (e.VerticalOffset + e.ViewportHeight == e.ExtentHeight)
                {
                    test = true;
                    unreadedMessages = 0;
                    IconOverlay();
                    NewMessagesGrid.Visibility = Visibility.Hidden;
                    try
                    {
                        FlashWindow.Stop(this);
                    }
                    catch
                    {

                    }
                }
                else
                    test = false;
            }
        }

        private void List_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                if (defaultSelection == false)
                {
                    var SlctdItm = list.SelectedItem as MessageEntity;
                    if (SlctdItm.TextColor2 == "#FF269FF4")
                    {
                        if (System.IO.File.ReadAllText(path + "\\secretchat\\links.txt") != "false")
                        {
                            var dialogResult = ModernWpf.MessageBox.Show("Bist Du dir sicher, dass Du diesen Link öffnen möchtest? ", "Achtung!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (dialogResult == MessageBoxResult.Yes)
                            {
                                Process.Start(SlctdItm.Message);
                            }
                        }
                        else
                        {
                            Process.Start(SlctdItm.Message);
                        }
                    }
                    else
                    {
                        list.ToolTip = tooltip;
                        tooltip.Content = "Nachricht kopiert.";
                        closeToolTipTimer.Stop();
                        tooltip.Visibility = Visibility.Visible;
                        tooltip.IsOpen = true;
                        closeToolTipTimer.Start();
                        try
                        {
                            Clipboard.SetText(SlctdItm.Message);
                        }
                        catch
                        {
                            ModernWpf.MessageBox.Show("Bitte nicht so schnell kopieren.");
                        }
                    }

                }

            }
        }

        private void List_LostFocus(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItem is MessageEntity)
            {
                try
                {
                    list.UnselectAll();
                    list.Items.Refresh();

                }
                catch
                {

                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView)
            {
                listView.SelectedIndex = -1;
            }
        }

        private void LastKeyPressedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timerElapsed = true;
        }

        void Window1_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            else
                list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible);
        }


        private void Chatbox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (textChanged == false)
            {
                if (chatbox.Text != "")
                {
                    if (collapsed == false)
                    {
                        collapsed = true;
                        defaultChatboxText.Visibility = Visibility.Collapsed;
                    }
                }
                else if (collapsed == true)
                {


                    collapsed = false;
                    defaultChatboxText.Visibility = Visibility.Visible;

                }
                if (Keyboard.IsKeyDown(Key.Enter))
                {
                    chatbox.AcceptsReturn = false;
                }
            }
        }

        private void CloseToolTipTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => tooltip.Visibility = tooltip.Visibility = Visibility.Hidden));
            Dispatcher.BeginInvoke((Action)(() => tooltip.IsOpen = false));
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (crash == true)
            {
                Clipboard.SetText(ErrorInfo);
                ModernWpf.MessageBox.Show(ErrorInfo);
                Task.Delay(5000);
                Environment.Exit(0);
            }
        }

        private void DefaultChatboxText_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Keyboard.ClearFocus();
            chatbox.Focus();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                StartAndPrepareAsync(startInfo);
            }
            catch (Exception ex)
            {
                var st = new StackTrace(ex, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                crash = true;
                ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an Dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + ex.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                //ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
            }
        }
    }


    public static class FlashWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        public const uint FLASHW_STOP = 0;
        public const uint FLASHW_CAPTION = 1;
        public const uint FLASHW_TRAY = 2;
        public const uint FLASHW_ALL = 3;
        public const uint FLASHW_TIMER = 4;
        public const uint FLASHW_TIMERNOFG = 12;

        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }


        public static bool Start(Window form)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(new WindowInteropHelper(form).Handle, FLASHW_ALL, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }

        public static bool Stop(Window form)
        {
            if (Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(new WindowInteropHelper(form).Handle, FLASHW_STOP, uint.MaxValue, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }

        private static bool Win2000OrLater
        {
            get { return System.Environment.OSVersion.Version.Major >= 5; }

        }


    }
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object),
            typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}