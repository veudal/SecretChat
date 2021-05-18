using DK.WshRuntime;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using ModernMessageBoxLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement.Core;
using Brush = System.Windows.Media.Brush;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SecretChat
{
    public partial class MainWindow : Window
    {

        readonly string version;
        readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string name;
        string timer;
        string ErrorInfo;
        public string currentChannel = "<General>";
        string macAndUser;
        string to;
        string code = null;
        string guid;
        readonly string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\secretchat\\";
        readonly string startInfo;
        int oneTwoThree = 1;
        int InfoPosition = 0;
        int unreadedMessages = 0;
        int ProgrammActive = 0;
        bool defaultStartSelection = true;
        bool newMessagesItem = false;
        bool timerElapsed = false;
        bool finishedLoop4FirstTime = false;
        bool crash = false;
        bool isUserOnline = true;
        bool defaultSelection = true;
        bool currentlyAtTheBottom = true;
        bool mouseReleased = true;
        readonly System.Windows.Controls.ToolTip tooltip = new System.Windows.Controls.ToolTip();
        public System.Timers.Timer closeToolTipTimer = new System.Timers.Timer(1500);
        public System.Timers.Timer lastKeyPressedTimer = new System.Timers.Timer(1000);
        public System.Timers.Timer shiftIsActiveTimer = new System.Timers.Timer(200);
        List<StoredUserEntity> storedUsers;
        readonly List<MessageEntity> DuplicateCheckerList = new List<MessageEntity>();
        public FriendsModel friendsModel = new FriendsModel();
        readonly MessagesModel messagesModel = new MessagesModel();
        readonly Channel1Model channel1Model = new Channel1Model();
        //MessageEntityListModel ListModel = new MessageEntityListModel();
        MessageController messageController;
        public OnlineUsersController onlineUsersController = new OnlineUsersController();
        readonly UsersController tableStorageUsers = new UsersController();
        readonly TrayContextMenu trayContextMenu = new TrayContextMenu();
        List<OnlineUserEntity> onlineAndAbsentUsers = new List<OnlineUserEntity>();
        Window darkwindow = new Window();

        public MainWindow(string startInfo, string v)
        {
            this.version = v;
            this.startInfo = startInfo;
            InitializeComponent();
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
                VerificationToast(macAndUser);
                while (true)
                {
                    await KillOtherInstance();
                    onlineAndAbsentUsers = onlineUsersController.GetAllOnlineAndAbsentUsers(name, macAndUser, code);
                    if (to != null)
                    {
                        bool offline = true;
                        foreach (var user in onlineAndAbsentUsers)
                        {
                            if (user.userName == to)
                            {
                                offline = false;
                                if (user.Status == "online")
                                {
                                    await Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Online"));
                                    break;
                                }
                                else if (user.Status == "absent")
                                {
                                    await Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Abwesend"));
                                    break;
                                }
                            }
                        }
                        if (offline == true)
                        {
                            await Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Offline"));
                        }
                    }
                    CheckIfUserIsOnline();
                    var absentUsers = onlineAndAbsentUsers.Where(u => u.Status == "absent").ToList();
                    foreach (var item in absentUsers)
                    {
                        item.AbsentUser = storedUsers.SingleOrDefault(absentUser => absentUser.PartitionKey == item.userName);
                    }
                    await Dispatcher.BeginInvoke((Action)(() => AbsentUsers.ItemsSource = absentUsers));
                    await Dispatcher.BeginInvoke((Action)(() => AbsentUsers.Items.Refresh()));
                    NewMessagesHandler();
                    if (ProgrammActive > 50000)
                    {
                        VerificationToast(macAndUser);
                        AppIsActive(onlineAndAbsentUsers);
                        await Dispatcher.BeginInvoke((Action)(() =>
                        {
                            this.Icon = new BitmapImage(new Uri("pack://application:,,,/resources/SecretChatIcon.ico"));
                        }));
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
                        isUserOnline = false;
                        ProgrammActive += 5000;
                        await Task.Delay(5000);
                    }
                }

            }
            catch (Exception e)
            {
                ShowInTaskbar = true;
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
                    this.Hide();
                    QModernMessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal. Fehler: " + ErrorInfo, "Fehler", QModernMessageBox.QModernMessageBoxButtons.Ok, ModernMessageboxIcons.Warning);
                    Environment.Exit(0);

                }
            }
        }

        private void CheckIfUserIsOnline()
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
            if (isUserOnline == true)
            {
                string Response = onlineUsersController.InsertOnlineUser(name, "online", null, macAndUser, code);
                if (Response != "True")
                {
                    this.Hide();
                    System.Windows.MessageBox.Show(Response);
                    Environment.Exit(0);
                }
            }
            else
            {
                string Response = onlineUsersController.InsertOnlineUser(name, "absent", DateTime.UtcNow.AddHours(-2), macAndUser, code);
                if (Response != "True")
                {
                    this.Hide();
                    System.Windows.MessageBox.Show(Response);
                    Environment.Exit(0);
                }
            }
        }

        private async Task CheckForNewMessages()
        {
            var lastItemGeneral = new MessageEntity();
            var lastItemChannel1 = new MessageEntity();
            var lastItemFriendChat = new MessageEntity();
            var storageList = messageController.GetMessages(name, macAndUser, code);
            var sortedList = storageList.OrderBy(s => s.Time);
            foreach (var item in sortedList)
            {

                if (item.To == null)
                {
                    if (messagesModel.Messages.Any(m => m.Channel == item.Channel))
                    {
                        if (!messagesModel.Messages.Any(m => m.MessageID == item.MessageID))
                        {
                        
                                if (lastItemGeneral.Time.ToLongDateString() + lastItemGeneral.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                {
                                    item.MessageOrInfo = "Visible";
                                    if (lastItemGeneral.from == item.from)
                                    {
                                        item.MessageOrInfo = "Collapsed";
                                    }
                                }
                            lastItemGeneral = item;
                            SetColorsForItems(item);
                            await AddNewMessagesItemGeneral(item);
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            InsertEmotes(item);
                            await Dispatcher.BeginInvoke((Action)(() =>
                            {
                                messagesModel.Messages.Add(item);
                                list.Items.Refresh();
                                if (item.from != name)
                                {
                                    if (item.Channel != currentChannel)
                                    {
                                        GeneralPoint.Visibility = Visibility.Visible;
                                        General.FontWeight = FontWeights.Bold;
                                        General.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                                    }
                                    unreadedMessages++;
                                    IconOverlay();
                                    NewMsgBtnOverlay();
                                    //NewMessagesGrid.Visibility = Visibility.Visible;
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

                                }
                                if (currentlyAtTheBottom == true && ApplicationIsActivated())
                                {
                                    ScrollToBottom();
                                }
                            }));
                        }

                    }
                    if (channel1Model.Channel1Messages.Any(m => m.Channel == item.Channel))
                    {
                        if (!channel1Model.Channel1Messages.Any(m => m.MessageID == item.MessageID))
                        {
                                if (lastItemChannel1.Time.ToLongDateString() + lastItemChannel1.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                {
                                    item.MessageOrInfo = "Visible";
                                    if (lastItemChannel1.from == item.from)
                                    {
                                        item.MessageOrInfo = "Collapsed";
                                    }
                                }
                            lastItemChannel1 = item;
                            SetColorsForItems(item);
                            //await AddNewMessagesItemChannel1(item);
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            InsertEmotes(item);
                            await Dispatcher.BeginInvoke((Action)(() =>
                            {
                                channel1Model.Channel1Messages.Add(item);
                                Channel1List.Items.Refresh();

                                if (item.from != name)
                                {
                                    if (item.Channel != currentChannel)
                                    {
                                        Channel1Point.Visibility = Visibility.Visible;
                                        Channel1.FontWeight = FontWeights.Bold;
                                        Channel1.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                                    }
                                    unreadedMessages++;
                                    IconOverlay();
                                    NewMsgBtnOverlay();
                                    //NewMessagesGrid.Visibility = Visibility.Visible;
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

                                }

                                if (currentlyAtTheBottom == true && ApplicationIsActivated())
                                {
                                    ScrollToBottomChannel1();
                                }
                            }));
                        }
                    }
                }
                else
                {
                    if (friendsModel.Messages != null)
                    {
                        if (!friendsModel.Messages.Any(m => m.MessageID == item.MessageID))
                        {
                            bool foundFriend = false;
                            foreach (var m in friendsModel.Friends)
                            {
                                if (m.from == item.from)
                                {
                                    foundFriend = true;
                                    break;
                                }

                            }
                            //if(foundFriend == true)
                            //{
                            //    var onlyFrom = new MessageEntity
                            //    {
                            //        from = item.from,
                            //        User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from)
                            //    };
                            //    if (onlyFrom.User.Picture == "x" || onlyFrom.User.Picture == null)
                            //    {
                            //        byte[] imgBytes = System.IO.File.ReadAllBytes(path + "\\SecretChat\\ProfilePictures\\default.png"); ;
                            //        string base64String = Convert.ToBase64String(imgBytes);
                            //        onlyFrom.User.Picture = base64String;
                            //    }
                            //    onlyFrom.TextColor = "#FF6E7178";
                            //    foreach (var f in friendsModel.Friends)
                            //    {
                            //        if(f.From == item.From || f.From == item.To)
                            //        {
                            //            friendsModel.Friends.Remove(f);
                            //        }
                            //    }
                            //    friendsModel.Friends.Insert(0, onlyFrom);
                            //}

                            if (item.from != name)
                            {
                                if (foundFriend == false)
                                {
                                    var onlyFrom = new MessageEntity
                                    {
                                        from = item.from,
                                        User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from)
                                    };
                                    if (onlyFrom.User.Picture == "x" || onlyFrom.User.Picture == null)
                                    {
                                        byte[] imgBytes = System.IO.File.ReadAllBytes(path + "\\SecretChat\\ProfilePictures\\default.png"); ;
                                        string base64String = Convert.ToBase64String(imgBytes);
                                        onlyFrom.User.Picture = base64String;
                                    }
                                    onlyFrom.TextColor = "#FF6E7178";
                                    //friendsModel.Friends.Reverse();
                                    friendsModel.Friends.Add(onlyFrom);
                                    //friendsModel.Friends.Reverse();
                                    //friendsModel.Friends.Insert(0, onlyFrom);
                                    await Dispatcher.BeginInvoke((Action)(() => Friendslist.Items.Refresh()));
                                }

                            }
                                if (lastItemFriendChat.Time.ToLongDateString() + lastItemFriendChat.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                {
                                    item.MessageOrInfo = "Visible";
                                    if (lastItemFriendChat.from == item.from)
                                    {
                                        item.MessageOrInfo = "Collapsed";
                                    }
                                }
                            
                            lastItemFriendChat = item;
                            SetColorsForItems(item);
                            //await AddNewMessagesItemFriend(item);
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            InsertEmotes(item);

                            await Dispatcher.BeginInvoke((Action)(() =>
                            {
                                if (item.Friend == to || item.To == to)
                                {
                                    friendsModel.Messages.Add(item);
                                    FriendChat.Items.Refresh();
                                }

                                if (item.from != name)
                                {
                                    bool isDuplicate = false;
                                    if (DuplicateCheckerList != null)
                                    {
                                        foreach (var fm in DuplicateCheckerList)
                                        {
                                            if (fm.MessageID == item.MessageID)
                                            {
                                                isDuplicate = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (isDuplicate == false)
                                    {
                                        DuplicateCheckerList.Add(item);
                                        foreach (var f in friendsModel.Friends)
                                        {
                                            if (f.from == item.from)
                                            {
                                                f.TextColor = "White";
                                                f.PointVisibility = "Visible";
                                                f.TextWeight = "Bold";
                                                Friendslist.Items.Refresh();
                                            }
                                        }
                                        unreadedMessages++;
                                        IconOverlay();
                                        NewMsgBtnOverlay();
                                        //NewMessagesGrid.Visibility = Visibility.Visible;
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
                                                    else
                                                    {
                                                        SendNotification(item);
                                                    }
                                                }
                                                else
                                                {
                                                    SendNotification(item);
                                                }
                                            }
                                        }

                                    }
                                }
                                if (currentlyAtTheBottom == true && ApplicationIsActivated())
                                {
                                    ScrollToBottomFriendChat();
                                }

                            }));

                        }



                    }

                }
            }
        }

        private void NewMessagesHandler()
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
            string channelName = currentChannel;
            Task.Run(CheckForNewMessages);
        }

        private void ScrollToBottom()
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    defaultSelection = true;
                    NewMessagesGrid.Visibility = Visibility.Hidden;
                    list.Items.Refresh();
                    list.SelectedIndex = list.Items.Count - 1;
                    list.ScrollIntoView(list.SelectedItem);
                    list.UnselectAll();
                    defaultSelection = false;
                }));
            }
            catch
            {

            }
        }

        private void ScrollToBottomChannel1()
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    defaultSelection = true;
                    NewMessagesGrid.Visibility = Visibility.Hidden;
                    Channel1List.Items.Refresh();
                    Channel1List.SelectedIndex = Channel1List.Items.Count - 1;
                    Channel1List.ScrollIntoView(Channel1List.SelectedItem);
                    Channel1List.UnselectAll();
                    defaultSelection = false;
                }));
            }
            catch
            {

            }
        }

        private void ScrollToBottomFriendChat()
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    defaultSelection = true;
                    NewMessagesGrid.Visibility = Visibility.Hidden;
                    FriendChat.Items.Refresh();
                    FriendChat.SelectedIndex = FriendChat.Items.Count - 1;
                    FriendChat.ScrollIntoView(FriendChat.SelectedItem);// FriendChat.Items.Count - 1);
                    FriendChat.UnselectAll();
                    defaultSelection = false;
                }));
            }
            catch
            {

            }
        }

        private static void InsertEmotes(MessageEntity item)
        {
            if (item.Message.ToLower().EndsWith("pog"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 3);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote1.png";
            }
            else if (item.Message.ToLower().EndsWith("sadge"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 5);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote3.png";
            }
            else if (item.Message.ToLower().EndsWith("omegalul"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 8);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote2.png";
            }
            else if (item.Message.ToLower().EndsWith("robbe"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 5);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote5.png";
            }
            else if (item.Message.ToLower().EndsWith("ez"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 2);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote4.png";
            }
            else if (item.Message.ToLower().EndsWith("mern"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 4);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote6.png";
            }
            else if (item.Message.EndsWith("juergen"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 7);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote7.png";
            }
            else if (item.Message.EndsWith("DetektivDefinitivNichtJuergenHart"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 33);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote8.png";
            }
            else if (item.Message.ToLower().EndsWith("jmn"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 3);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote9.png";
            }
            else if (item.Message.ToLower().EndsWith("robbenman"))
            {
                item.MessageWidth = double.NaN.ToString();
                item.Message = item.Message.Remove(item.Message.Length - 9);
                item.EmoteVisibility = "Visible";
                item.EmoteSource = "pack://application:,,,/SecretChat;component/resources/emote10.png";
            }
        }

        private void AppIsActive(List<OnlineUserEntity> onlineOrAbsentUsers)
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
            foreach (var onlineOrAbsentUser in onlineOrAbsentUsers)
            {
                if (onlineOrAbsentUser.Status == "online")
                {
                    if (onlineOrAbsentUser.userName == name)
                    {
                        string Response = onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow.AddHours(-2), macAndUser, code);
                        if (Response != "True")
                        {
                            this.Hide();
                            System.Windows.MessageBox.Show(Response);
                            Environment.Exit(0);
                        }
                    }
                    onlineOrAbsentUser.User = storedUsers.SingleOrDefault(user => user.PartitionKey == onlineOrAbsentUser.userName);
                }
                else
                {
                    if (onlineOrAbsentUser.userName == name)
                    {
                        string Response = onlineUsersController.InsertOnlineUser(name, "absent", DateTime.UtcNow.AddHours(-2), macAndUser, code);
                        if (Response != "True")
                        {
                            this.Hide();
                            System.Windows.MessageBox.Show(Response);
                            Environment.Exit(0);
                        }

                    }
                    onlineOrAbsentUser.AbsentUser = storedUsers.SingleOrDefault(user => user.PartitionKey == onlineOrAbsentUser.userName);
                }
            }
        }

        private void VerificationToast(string macAndUser)
        {
            foreach (var u in storedUsers)
            {
                if (u.PartitionKey == name)
                {
                    if (u.Verification == "true")
                    {

                        if (System.IO.File.ReadAllText(path + "\\secretchat\\info.txt") == "false")
                        {
                            NotificationSound();
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
                    break;
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

        private async Task AddNewMessagesItemGeneral(MessageEntity item)
        {
            if (item.from != name)
            {
                if (newMessagesItem == false)
                {
                    newMessagesItem = true;
                    MessageEntity messageEntity = new MessageEntity
                    {
                        Message = Environment.NewLine + "_____________________Neue Nachrichten_____________________",
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

        private async Task AddNewMessagesItemChannel1(MessageEntity item)
        {
            if (item.from != name)
            {
                if (newMessagesItem == false)
                {
                    newMessagesItem = true;
                    MessageEntity messageEntity = new MessageEntity
                    {
                        Message = Environment.NewLine + "_____________________Neue Nachrichten_____________________",
                        TextColor2 = "BlueViolet",
                        MessageOrInfo = "Collapsed"
                    };
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (WindowState == WindowState.Maximized)
                            messageEntity.LeftOrCenter = "Right";
                        else
                        {
                            messageEntity.LeftOrCenter = "Left";
                        }
                        channel1Model.Channel1Messages.Add(messageEntity);
                        Channel1List.Items.Refresh();
                    }));
                    InfoPosition = channel1Model.Channel1Messages.Count;
                }
            }
        }

        //private async Task AddNewMessagesItemFriend(MessageEntity item)
        //{
        //    if (item.from != name)
        //    {
        //        if (newMessagesItem == false)
        //        {
        //            newMessagesItem = true;
        //            MessageEntity messageEntity = new MessageEntity
        //            {
        //                Message = Environment.NewLine + "_____________________Neue Nachrichten_____________________",
        //                TextColor2 = "BlueViolet",
        //                MessageOrInfo = "Collapsed"
        //            };
        //            await Dispatcher.BeginInvoke((Action)(() =>
        //            {
        //                if (WindowState == WindowState.Maximized)
        //                    messageEntity.LeftOrCenter = "Right";
        //                else
        //                    messageEntity.LeftOrCenter = "Left";
        //                friendsModel.FriendsMessages.Add(messageEntity);
        //                FriendChat.Items.Refresh();
        //            }));
        //            InfoPosition = friendsModel.FriendsMessages.Count;
        //        }
        //    }
        //}

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

                List<OnlineUserEntity> typing = null;
                var onlineUsers = onlineAndAbsentUsers.Where(u => u.Status == "online").ToList();
                foreach (var item in onlineUsers)
                {
                    if (to != null)
                    {
                        if (item.userName == to)
                        {
                            await Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Online"));
                        }
                    }
                    item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.userName);
                    if (item.userName != name /*&& (item.userName == to || (currentChannel == "<General>" ||currentChannel == "<Channel 1>"))*/)
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

                            if (item.Time.HasValue && item.Time.Value.CompareTo(DateTime.UtcNow.AddSeconds(-4)) >= 0)
                                item.IsTyping = true;
                            else
                                item.IsTyping = false;

                        }

                    }
                    else
                        item.Visibility = "Collapsed";
                }
                typing = onlineUsers.Where(u => u.IsTyping == true).ToList();
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    Typing.ItemsSource = typing;
                    OnlineUsers.ItemsSource = onlineUsers;
                }));
                if (ApplicationIsActivated() == true)
                {
                    if (typing.Count > 0)
                        await Task.Delay(200);
                    else
                        await Task.Delay(800);
                }
                else
                {
                    if (typing.Count > 0)
                        await Task.Delay(200);
                    else
                        await Task.Delay(1500);
                }

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
        //    ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an Dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in Deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
        //    //ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in Deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
        //}
        //}

        private void SendNotification(MessageEntity item)
        {
            if (item.Channel != null)
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
                NotificationSound();

                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                var launchAttr = toastXml.CreateAttribute("launch");
                launchAttr.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
                toastXml.GetElementsByTagName("toast").First().Attributes.SetNamedItem(launchAttr);
                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(item.from + " in #" + item.Channel.Replace("<Channel 1>", "Verbesserungsideen").Replace("<General>", "Allgemein")));
                stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";

                XmlElement audio = toastXml.CreateElement("audio");
                audio.SetAttribute("src", "ms-winsoundevent:Notification.Default");
                audio.SetAttribute("silent", "true");
                toastXml.DocumentElement.AppendChild(audio);
                XmlElement actions = toastXml.CreateElement("actions");
                toastXml.DocumentElement.AppendChild(actions);

                var toast = new ToastNotification(toastXml);
                toast.Activated += ToastActivated;
                //toast.Failed += ToastFailed;
                ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            }
            else if (item.To != null)
            {
                NotificationSound();
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                var launchAttr = toastXml.CreateAttribute("launch");
                launchAttr.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
                toastXml.GetElementsByTagName("toast").First().Attributes.SetNamedItem(launchAttr);
                //var argumentsAttr = toastXml.CreateAttribute("arguments");
                //argumentsAttr.Value = "argumentsParam";
                //toastXml.GetElementsByTagName("toast").First().Attributes.SetNamedItem(argumentsAttr);

                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(item.from));
                stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";

                XmlElement audio = toastXml.CreateElement("audio");
                audio.SetAttribute("src", "ms-winsoundevent:Notification.Default");
                audio.SetAttribute("silent", "true");
                toastXml.DocumentElement.AppendChild(audio);
                XmlElement actions = toastXml.CreateElement("actions");
                toastXml.DocumentElement.AppendChild(actions);

                var toast = new ToastNotification(toastXml);

                toast.Activated += ToastActivated;//+= ToastActivated("parameter");
                //toast.Failed += ToastFailed;
                ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            }
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
            if (item.Channel != null)
            {
                NotificationSound();
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                var launchAttr = toastXml.CreateAttribute("launch");
                launchAttr.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
                toastXml.GetElementsByTagName("toast").First().Attributes.SetNamedItem(launchAttr);
                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(item.from + " hat dich in #" + item.Channel.Replace("<Channel 1>", "Verbessserungsideen").Replace("<General>", "Allgemein") + " erwähnt"));
                stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";

                XmlElement audio = toastXml.CreateElement("audio");
                audio.SetAttribute("src", "ms-winsoundevent:Notification.Default");
                audio.SetAttribute("silent", "true");
                toastXml.DocumentElement.AppendChild(audio);
                XmlElement actions = toastXml.CreateElement("actions");
                toastXml.DocumentElement.AppendChild(actions);

                var toast = new ToastNotification(toastXml);
                toast.Activated += ToastActivated;
                ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            }
            else if (item.To != null)
            {
                NotificationSound();
                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
                var launchAttr = toastXml.CreateAttribute("launch");
                launchAttr.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
                toastXml.GetElementsByTagName("toast").First().Attributes.SetNamedItem(launchAttr);
                var stringElements = toastXml.GetElementsByTagName("text");
                stringElements[0].AppendChild(toastXml.CreateTextNode(item.from + " hat dich im Privat-Chat erwähnt"));
                stringElements[1].AppendChild(toastXml.CreateTextNode(item.Message));
                var imageElements = toastXml.GetElementsByTagName("image");
                imageElements[0].Attributes.GetNamedItem("src").NodeValue = path + "\\SecretChat\\icon.png";

                XmlElement audio = toastXml.CreateElement("audio");
                audio.SetAttribute("src", "ms-winsoundevent:Notification.Default");
                audio.SetAttribute("silent", "true");
                toastXml.DocumentElement.AppendChild(audio);
                XmlElement actions = toastXml.CreateElement("actions");
                toastXml.DocumentElement.AppendChild(actions);

                var toast = new ToastNotification(toastXml);
                toast.Activated += ToastActivated;
                //toast.Failed += ToastFailed;
                ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            }
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



        //private void ToastFailed(ToastFailedEventArgs args)
        //{
        //    ModernWpf.MessageBox.Show("Benachrichtigung konnte nicht gesendet werden: " + args.ToString() + args.ErrorCode);
        //}

        private void ToastActivated(ToastNotification sender, object args)
        {
            MessageEntity item = JsonConvert.DeserializeObject<MessageEntity>(Encoding.UTF8.GetString(Convert.FromBase64String(((ToastActivatedEventArgs)args).Arguments)));
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
                if (item.Channel != null)
                {
                    if (item.Channel == "<General>")
                    {
                        ChannelsList.SelectedItem = GeneralItem;
                        ScrollToBottom();
                    }
                    else if (item.Channel == "<Channel 1>")
                    {
                        ChannelsList.SelectedItem = Channel1Item;
                        ScrollToBottomChannel1();
                    }
                }
                else if (item.To != null)
                {
                    foreach (var friend in friendsModel.Friends)
                    {
                        if (friend.from == item.from)
                        {
                            Friendslist.SelectedItem = friend;
                            Friendslist.Items.Refresh();
                            ScrollToBottomFriendChat();
                        }
                    }
                }
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
                RoutedEventArgs eventArgs = new RoutedEventArgs();
                Settings_Click(args, eventArgs);

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
                Task.Run(InsertOnlineUserAsync);
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
                if (System.IO.File.ReadAllText(path + "\\secretchat\\emojis.txt") == "true")
                {
                    text = ConvertToEmojis(text);
                }
                if (IsAllUpper(text))
                {
                    text = text.ToLower();
                }
                while (text.EndsWith("\r\n"))
                {
                    text = text.Remove(text.Length - 2);
                }
                bool blocked = false;
                sendButton.Visibility = Visibility.Collapsed;
                MessageController tableStorage = new MessageController();
                if (_lastMessageSentAt.CompareTo(DateTime.Now.AddSeconds(-0.5)) >= 0)
                {
                    ModernWpf.MessageBox.Show("Deine Nachricht wurde aufgrund von spam nicht versendet.");
                    return;
                }
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.Length < 2000)
                    {
                        if (chatbox.LineCount < 21)
                        {
                            if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                            {
                                var storedUsers = tableStorageUsers.GetSpecificUser(name, macAndUser, code);
                                StoredUserEntity User = storedUsers.ElementAt(0);
                                if (User.Blocked == true)
                                {
                                    blocked = true;
                                }
                                if (blocked == false)
                                {
                                    chatbox.Clear();
                                    if (newMessagesItem == true)
                                    {
                                        if (InfoPosition > 0)
                                        {
                                            list.Items.Refresh();
                                            messagesModel.Messages.RemoveAt(InfoPosition - 1);
                                            list.Items.Refresh();
                                            newMessagesItem = false;
                                        }
                                    }
                                    text = Encrypt.EncryptString(text, "fJx82E@$48!L");
                                    string insertMessageResponse = tableStorage.InsertMessage(Convert.ToString(Guid.NewGuid()), text, name, macAndUser, code, currentChannel, to);
                                    if (insertMessageResponse == "True")
                                    {
                                        _lastMessageSentAt = DateTime.Now;
                                        Task.Run(CheckForNewMessages);
                                        NewMsgBtnOverlay();
                                        IconOverlay();
                                        Task.Run(Sound);
                                        if (currentChannel == "<General>")
                                        {
                                            ScrollToBottom();
                                        }
                                        else if (currentChannel == "<Channel 1>")
                                        {
                                            ScrollToBottomChannel1();
                                        }
                                        else if (currentChannel == null)
                                        {
                                            ScrollToBottomFriendChat();
                                        }
                                    }
                                    else
                                    {
                                        ModernWpf.MessageBox.Show(insertMessageResponse);
                                        Environment.Exit(0);
                                    }
                                    //chatbox.AcceptsReturn = true;
                                }
                                else
                                {
                                    ModernWpf.MessageBox.Show("Du wurdest von einem Administrator blockiert. Der Grund sollte DIR bekannt sein. Höchstwahrscheinlich kannst Du innerhalb der nächsten 24 Stunden wieder Nachrichten schreiben.");
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
                ShowInTaskbar = true;
                if (IsConnectedToInternet() == true)
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame.GetFileLineNumber();
                    crash = true;
                    ErrorInfo = "Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                }
                else
                {
                    this.Hide();
                    QModernMessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal. " + ErrorInfo, "Fehler", QModernMessageBox.QModernMessageBoxButtons.Ok, ModernMessageboxIcons.Warning);
                    Environment.Exit(0);

                }
            }
        }

        private void InsertOnlineUserAsync()
        {
            string Response = onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow.AddHours(-2), macAndUser, code);
            if (Response != "True")
            {
                this.Hide();
                System.Windows.MessageBox.Show(Response);
                Environment.Exit(0);
            }
        }

        private static string ConvertToEmojis(string text)
        {
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
            return text;
        }

        bool IsAllUpper(string input)
        {

            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsLetter(input[i]) && !Char.IsUpper(input[i]))
                    return false;
            }
            if (input.Length > 5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Sound()
        {
            if (System.IO.File.ReadAllText(path + "\\secretchat\\sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.preview);
                playSound.Play();
            }
        }




        private void Chatbox_KeyUp(object sender, KeyEventArgs e)
        {
            chatbox.AcceptsReturn = false;
            SetSendButtonVisibility();
        }

        private void Chatbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                if (!String.IsNullOrWhiteSpace(chatbox.Text))
                {
                    SendMessage();
                }
            }
            chatbox.AcceptsReturn = true;
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
            string Response = onlineUsersController.InsertOnlineUser(name, "online", DateTime.UtcNow, macAndUser, code);
            if (Response != "True")
            {
                this.Hide();
                System.Windows.MessageBox.Show(Response);
                Environment.Exit(0);
            }
        }

        private void SetSendButtonVisibility()
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
            //var settings = new Settings(this);
            //settings.ShowDialog();
            //if(settings.Shutdown == true)
            //{
            //    SystemTray.Dispose();
            //    Environment.Exit(0);
            //}
            SettingsGrid.Visibility = Visibility.Visible;
            this.UpdateLayout();
            Task.Run(PrepareSettingssWindow);
        }

        private async Task PrepareSettingssWindow()
        {
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                List<StoredUserEntity> storedUser;
                if (System.IO.File.Exists(settingsPath + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                string count = "0";
                if (System.IO.File.Exists(settingsPath + "counter.txt"))
                {
                    count = System.IO.File.ReadAllText(settingsPath + "counter.txt");
                }
                PersonalaziationList.Visibility = Visibility.Collapsed;
                NotificationSettingsList.Visibility = Visibility.Collapsed;
                OtherList.Visibility = Visibility.Collapsed;
                MyAccountList.Visibility = Visibility.Visible;
                SettingsMenuList.SelectedItem = MyAccountItem;
                SendMsgsCount.Text = count;
                storedUser = tableStorageUsers.GetSpecificUser(name, macAndUser, code);
                StoredUserEntity User = storedUser.ElementAt(0);
                StatusInfo.Text = User.Info;
                foreach (var u in storedUser)
                {
                    if (u.PartitionKey == name)
                    {
                        if (u.Verification == "true")
                        {
                            PictureButton.IsEnabled = true;
                            RequestVerification.Visibility = Visibility.Collapsed;
                        }
                    }
                    if (u.storedMac == macAndUser)
                    {
                        NameBlockCopy.Text = u.PartitionKey;
                    }

                }
                SetOneProfilePicture(storedUser);
                if (System.IO.File.ReadAllText(settingsPath + "autostart.txt") == "false")
                {
                    AutoStart.IsChecked = false;
                }
                else
                {
                    AutoStart.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "notifications.txt") == "false")
                {
                    Benachrichtigungen.IsChecked = false;
                }
                else
                {
                    Benachrichtigungen.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "links.txt") == "false")
                {
                    Links.IsChecked = false;
                }
                else
                {
                    Links.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "sounds.txt") == "false")
                {
                    Sounds.IsChecked = false;
                }
                else
                {
                    Sounds.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "mentions.txt") == "false")
                {
                    Mentions.IsChecked = false;
                }
                else
                {
                    Mentions.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "emojis.txt") == "false")
                {
                    Emojis.IsChecked = false;
                }
                else
                {
                    Emojis.IsChecked = true;
                }
                if (System.IO.File.ReadAllText(settingsPath + "darkorlight.txt") == "dark")
                {
                    ModeCheckbox.IsChecked = true;
                }
                else
                {
                    ModeCheckbox.IsChecked = false;
                }
                this.UpdateLayout();
            }));
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
            if (currentChannel == "<General>")
            {
                if (newMessagesItem == true)
                {
                    if (InfoPosition > 0)
                    {
                        list.Items.Refresh();
                        messagesModel.Messages.RemoveAt(InfoPosition - 1);
                        list.Items.Refresh();
                        newMessagesItem = false;
                    }
                }
            }
            try
            {
                FlashWindow.Stop(this);
            }
            catch
            {

            }
            //ScrollToBottom();
            IconOverlay();
            Task.Run(Sound);
            NewMessagesGrid.Visibility = Visibility.Hidden;
        }


        private void Window_Activated_1(object sender, EventArgs e)
        {
            try
            {
                isUserOnline = true;
                try
                {
                    FlashWindow.Stop(this);
                }
                catch
                {
                    try
                    {
                        FlashWindow.Stop(this);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            { }

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            isUserOnline = false;
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
            this.Activate();
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
                    Environment.Exit(0);
                }));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            NotificationReminder();
            ScrollToBottom();
        }

        //private void CenterWindowOnScreen()
        //{
        //    double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        //    double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
        //    double windowWidth = this.Width;
        //    double windowHeight = this.Height;
        //    this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
        //    this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
        //}


        private void CreateShortcut()
        {
            string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutLocation = System.IO.Path.Combine(desktop, "Secret Chat" + ".lnk");
            WshInterop.CreateShortcut(shortcutLocation, "Secret Chat", pathToExe, null, null);

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


        private void StartAndPrepare(string startInfo)
        {
            if (startInfo != "false")
            {
                Topmost = true;
                this.MinWidth = 1100;
                this.MinHeight = 630;

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
                DataContext = onlineUsersController;
                //DataContext = new MessageController();
                if (System.IO.File.ReadAllText(settingsPath + "darkorlight.txt") == "light")
                {
                    SetEverythingForLightMode();
                }
                else
                {
                    //sun4LightTheme.Visibility = Visibility.Hidden;
                    //moon4LightTheme.Visibility = Visibility.Hidden;
                }
                macAndUser = GetMac() + Environment.UserName;
                sendButton.Visibility = Visibility.Hidden;
                Channel1List.Visibility = Visibility.Collapsed;
                //FriendChat.Visibility = Visibility.Collapsed;
                //NewMessagesGrid.Visibility = Visibility.Visible;
                this.StateChanged += new EventHandler(Window1_StateChanged);
                //list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, System.Windows.Controls.ScrollBarVisibility.Hidden);
                tooltip.Visibility = Visibility.Hidden;
                ShowInTaskbar = true;
                SystemTray.Visibility = Visibility.Visible;
                if (System.IO.File.ReadAllText(path + "\\secretchat\\autostart.txt") != "false")
                {
                    string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                    string shortcutLocation = System.IO.Path.Combine(startUpPath, "Secret Chat" + ".lnk");
                    WshInterop.CreateShortcut(shortcutLocation, "Secret Chat", pathToExe, null, null);
                }
                if (Convert.ToInt32(System.IO.File.ReadAllText(path + "\\SecretChat\\timer.txt")) < 200 || Convert.ToInt32(System.IO.File.ReadAllText(path + "\\SecretChat\\timer.txt")) > 2000)
                {
                    System.IO.File.WriteAllText(path + "\\SecretChat\\timer.txt", "500");
                }
                string pathForShortcut = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory) + "\\Secret Chat.lnk";
                if (System.IO.File.Exists(pathForShortcut))
                {
                    CreateShortcut();
                }
                if (System.IO.File.Exists(path + "\\secretchat\\code.txt"))
                {
                    code = System.IO.File.ReadAllText(path + "\\secretchat\\code.txt");
                    try
                    {
                        storedUsers = tableStorageUsers.GetStoredUsers(macAndUser, code);
                    }

                    catch (Exception e)
                    {
                        ShowInTaskbar = true;
                        if (IsConnectedToInternet() == true)
                        {
                            var st = new StackTrace(e, true);
                            var frame = st.GetFrame(0);
                            var line = frame.GetFileLineNumber();
                            crash = true;
                            ErrorInfo = "Zeitüberschreitung, möglicherweise sind unsere Server sind momentan leider nicht verfügbar. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                        }
                        else
                        {
                            this.Hide();
                            QModernMessageBox.Show("Du bist nicht mit dem Internet verbunden! Bitte versuche es später noch einmal. " + ErrorInfo, "Fehler", QModernMessageBox.QModernMessageBoxButtons.Ok, ModernMessageboxIcons.Warning);
                            Environment.Exit(0);
                        }
                    }
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                messageController = new MessageController();
                OnlineUsers.Items.Refresh();
                var specificUsername = tableStorageUsers.GetSpecificUserName(macAndUser, code);
                foreach (var u in specificUsername)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                        NameBlock.Text = name;
                        break;
                    }
                }
                SetProfilePictures();
                //ChannelsList.ItemsSource = friendsModel.Friends;
                Friendslist.ItemsSource = friendsModel.Friends;
                Friendslist.Items.Refresh();
                FriendChat.ItemsSource = friendsModel.Messages;
                FriendChat.Items.Refresh();
                list.ItemsSource = messagesModel.Messages;
                list.Items.Refresh();
                Channel1List.ItemsSource = channel1Model.Channel1Messages;
                Channel1List.Items.Refresh();
                var storageList = messageController.GetAllMessages(name, macAndUser, code);
                var sortedList = storageList.OrderBy(s => s.Time);
                SetItemsProperties(sortedList);
                var lastItemChannel1 = new MessageEntity();
                var lastItemGeneral = new MessageEntity();
                var messages = messageController.GetMessages(name, macAndUser, code);
                var sortedMessages = messages.OrderBy(s => s.Time);
                foreach (var item in sortedMessages)
                {
                    if (item.To == null)
                    {
                        if (item.Channel == "<General>" || item.Channel == null)
                        {
                                if (lastItemGeneral.Time.ToLongDateString() + lastItemGeneral.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                {
                                    item.MessageOrInfo = "Visible";
                                    if (lastItemGeneral.from == item.from)
                                    {
                                        item.MessageOrInfo = "Collapsed";
                                    }
                                }
                            
                            lastItemGeneral = item;
                            SetColorsForItems(item);
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            InsertEmotes(item);
                            messagesModel.Messages.Add(item);
                        }
                        else if (item.Channel == "<Channel 1>")
                        {
                         
                                if (lastItemChannel1.Time.ToLongDateString() + lastItemChannel1.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                {
                                    item.MessageOrInfo = "Visible";
                                    if (lastItemChannel1.from == item.from)
                                    {
                                        item.MessageOrInfo = "Collapsed";
                                    }
                                }

                            lastItemChannel1 = item;
                            SetColorsForItems(item);
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            InsertEmotes(item);
                            channel1Model.Channel1Messages.Add(item);
                        }
                    }
                    else
                    {
                        if (item.Friend != name)
                        {
                            bool ignoreFriend = false;
                            if (File.Exists(settingsPath + "ignoreList.txt"))
                            {
                                var linesRead = File.ReadLines(settingsPath + "ignoreList.txt");
                                foreach (var lineRead in linesRead)
                                {
                                    if (Regex.IsMatch(lineRead, item.Friend, RegexOptions.IgnoreCase))
                                    {
                                        ignoreFriend = true;
                                    }
                                }
                            }
                            if (ignoreFriend == false)
                            {
                                if (friendsModel.Friends.Count != 0)
                                {
                                    var onlyFrom = new MessageEntity
                                    {
                                        from = item.from
                                    };
                                    bool copy = false;
                                    foreach (var f in friendsModel.Friends.ToList())
                                    {
                                        if (f.Friend == onlyFrom.from)
                                        {
                                            copy = true;
                                            break;
                                        }
                                    }
                                    if (copy == false)
                                    {
                                        onlyFrom.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                                        onlyFrom.TextColor = "#FF6E7178";
                                        friendsModel.Friends.Add(onlyFrom);
                                        Friendslist.Items.Refresh();
                                    }
                                }
                                else
                                {
                                    var onlyFrom = new MessageEntity
                                    {
                                        from = item.from,
                                        User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from),
                                        TextColor = "#FF6E7178"
                                    };
                                    friendsModel.Friends.Add(onlyFrom);
                                    Friendslist.Items.Refresh();
                                }
                            }
                        }
                    }
                }
                using (StringReader reader = new StringReader(GetUpdateNews()))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Replace("# ", "");
                        line = line.Replace("#", "");
                        UpdateInfoList.Items.Add(line);
                    }
                }
                Task.Run(Refresher);
                Task.Run(WhoIsTyping);
                CloseWIndowUsingIdentifier("MySplashScreen");
                WindowState = WindowState.Maximized;
                Title = "Secret Chat   v" + version;
                this.Show();
                this.Activate();
                Topmost = false;
                chatbox.Focus();
                chatbox.SelectionLength = 0;
                ChannelsList.SelectedItem = General;


                //friendsModel.Messages = messageController.GetAllFriendsMessages(name, macAndUser, code);
                lastKeyPressedTimer.Start();
                lastKeyPressedTimer.Elapsed += LastKeyPressedTimer_Elapsed;
                closeToolTipTimer.Elapsed += CloseToolTipTimer_Elapsed;
                shiftIsActiveTimer.Elapsed += ShiftIsActiveTimer_Elapsed;

                //this.Show();
            }
        }

        private void SetItemsProperties(IOrderedEnumerable<MessageEntity> sortedList)
        {
            var lastItemGeneral = new MessageEntity();
            var lastItemChannel1 = new MessageEntity();
            foreach (var item in sortedList)
            {
                if (item.To == null)
                {
                    if (item.Channel == "<General>" || item.Channel == null)
                    {
                            if (lastItemGeneral.Time.ToLongDateString() + lastItemGeneral.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                            {
                                item.MessageOrInfo = "Visible";
                                if (lastItemGeneral.from == item.from)
                                {
                                    item.MessageOrInfo = "Collapsed";
                                }
                            }
                        
                        SetColorsForItems(item);
                        item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                        InsertEmotes(item);
                        messagesModel.Messages.Add(item);
                        lastItemGeneral = item;

                    }
                    else if (item.Channel == "<Channel 1>")
                    {
                            if (lastItemChannel1.Time.ToLongDateString() + lastItemChannel1.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                            {
                                item.MessageOrInfo = "Visible";
                                if (lastItemChannel1.from == item.from)
                                {
                                    item.MessageOrInfo = "Collapsed";
                                }
                            }

                        lastItemChannel1 = item;
                        SetColorsForItems(item);
                        item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                        InsertEmotes(item);
                        channel1Model.Channel1Messages.Add(item);
                    }
                }
                else
                {
                    if (item.Friend != name)
                    {
                        bool ignoreFriend = ShouldIgnoreFriend(item);
                        if (ignoreFriend == false)
                        {
                            if (friendsModel.Friends.Count != 0)
                            {
                                MessageEntity onlyFrom = new MessageEntity
                                {
                                    from = item.from
                                };
                                bool copy = false;
                                foreach (var f in friendsModel.Friends.ToList())
                                {
                                    if (f.Friend == onlyFrom.from)
                                    {
                                        copy = true;
                                        break;
                                    }
                                }
                                if (copy == false)
                                {
                                    onlyFrom.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                                    onlyFrom.TextColor = "#FF6E7178";
                                    friendsModel.Friends.Add(onlyFrom);
                                    Friendslist.Items.Refresh();
                                }
                            }
                            else
                            {
                                MessageEntity onlyFrom = new MessageEntity
                                {
                                    from = item.from,
                                    User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from),
                                    TextColor = "#FF6E7178"
                                };
                                friendsModel.Friends.Add(onlyFrom);
                                Friendslist.Items.Refresh();
                            }
                        }
                    }
                }
            }
        }

        private bool ShouldIgnoreFriend(MessageEntity item)
        {
            bool ignoreFriend = false;
            if (File.Exists(settingsPath + "ignoreList.txt"))
            {
                var linesRead = File.ReadLines(settingsPath + "ignoreList.txt");
                foreach (var lineRead in linesRead)
                {
                    if (Regex.IsMatch(lineRead, item.Friend, RegexOptions.IgnoreCase))
                    {
                        ignoreFriend = true;
                    }
                }
            }

            return ignoreFriend;
        }

        private void SetEverythingForLightMode()
        {
            SoundsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            EinstellungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            BenachrichtigungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            LinksText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            AutostartText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            SoundsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            EinstellungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            BenachrichtigungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            LinksText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            AutostartText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            RefreshTimeTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            RefreshTimeText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ShortcutTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ShortcutText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            NotifcationText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            DeleteAccountTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            DeleteAccountText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ExitSCTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ExitSCText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            RestartSCTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            RestartSCText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            helpBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF343434"));
            MeinAccount.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            Personalisierung.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            Gefahrenzone.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            Informationen.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            Notification.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            MentionsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            EmojisText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            ModeText.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            NameBlockCopy.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            UpdateInfoList.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            StatusInfo.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            SetPictureTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            SetPictureText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ResetPictureText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ResetPictureTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ChangeNameTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            ChangeNameText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            SendMsgsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            NameBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF333333"));
            FriendNameBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("Black"));
            chatbox.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF333333"));
            SettingsGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF7F7F7"));
            ChannelsList.Background = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            AddFriendBtn.Background = (Brush)(new BrushConverter().ConvertFrom("#FFAFABAB"));
            Chat.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF2F2F2"));
            chatbox.Background = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            FriendNameBox.Background = (Brush)(new BrushConverter().ConvertFrom("LightGray"));
            NameAndPictureGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFB4C7E7"));
            Friendslist.Background = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            AddFriendGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            OverlayRectangle.Fill = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            RectangleOverlay.Fill = (Brush)(new BrushConverter().ConvertFrom("#FFDAE3F3"));
            AddFriendBtn.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("DarkGray"));
            FriendNameBox.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("Gray"));
            RightSideRectangle.Visibility = Visibility.Visible;
            sendButtonImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/SecretChat;component/resources/send message light.png", UriKind.Absolute));
            SettingsImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/SecretChat;component/resources/Settings Light.png", UriKind.Absolute));
        }

        private void SetEverythingForDarkMode()
        {
            SoundsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            EinstellungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            BenachrichtigungenText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            LinksText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            AutostartText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            RefreshTimeTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            RefreshTimeText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ShortcutTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ShortcutText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            NotifcationText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            DeleteAccountTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            DeleteAccountText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ExitSCTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ExitSCText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            RestartSCTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            RestartSCText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            helpBtn.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            MeinAccount.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            Personalisierung.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            Gefahrenzone.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            Informationen.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            Notification.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            MentionsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            EmojisText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ModeText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            NameBlockCopy.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            UpdateInfoList.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            StatusInfo.Foreground = (Brush)(new BrushConverter().ConvertFrom("WhiteSmoke"));
            SetPictureTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            SetPictureText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ResetPictureText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ResetPictureTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ChangeNameTextBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            ChangeNameText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            SendMsgsText.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            NameBlock.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            FriendNameBox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            chatbox.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
            SettingsGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#F92D2D30"));
            ChannelsList.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2F3136"));
            AddFriendBtn.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2B76B4"));
            Chat.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            chatbox.Background = (Brush)(new BrushConverter().ConvertFrom("#FF444448"));
            FriendNameBox.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
            NameAndPictureGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#FF292B2F"));
            Friendslist.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2F3136"));
            AddFriendGrid.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2F3136"));
            OverlayRectangle.Fill = (Brush)(new BrushConverter().ConvertFrom("#FF2F3136"));
            RectangleOverlay.Fill = (Brush)(new BrushConverter().ConvertFrom("#FF2F3136"));
            AddFriendBtn.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("#FF2B76B4"));
            FriendNameBox.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("#FF4C4C4F"));
            RightSideRectangle.Visibility = Visibility.Collapsed;
            sendButtonImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/SecretChat;component/resources/send message.png", UriKind.Absolute));
            SettingsImage.Source = new BitmapImage(new Uri(@"pack://application:,,,/SecretChat;component/resources/Settings.png", UriKind.Absolute));
        }

        private void SetProfilePictures()
        {
            foreach (var u in storedUsers)
            {
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
                        using (var imageFile = new FileStream(path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png", FileMode.Create))
                        {
                            imageFile.Write(imgBytes, 0, imgBytes.Length);
                            imageFile.Flush();
                            imageFile.Close();
                        }
                        if (u.PartitionKey == name)
                        {
                            BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png"));
                            picture.ImageSource = image;
                        }

                    }
                    else if (u.PartitionKey == name)
                    {
                        BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\default.png"));
                        picture.ImageSource = image;

                    }
                }
                else
                {
                    if (u.PartitionKey == name)
                    {
                        BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\default.png"));
                        picture.ImageSource = image;
                    }
                }
                if (u.UserID != null)
                {
                    if (System.IO.File.Exists(path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png"))
                    {
                        if (!string.IsNullOrEmpty(u.Picture))
                            u.Picture = path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png";
                        else
                            u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    }
                    else
                    {
                        u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    }
                }
            }
        }

        private void SetOneProfilePicture(List<StoredUserEntity> storedUser)
        {
            foreach (var u in storedUser)
            {
                if (!string.IsNullOrEmpty(u.Picture))
                {
                    if (u.Picture != "x")
                    {
                        if (u.PartitionKey == name)
                        {
                            BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png"));
                            pictureCopy.ImageSource = image;
                        }

                    }
                    else if (u.PartitionKey == name)
                    {
                        BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\default.png"));
                        pictureCopy.ImageSource = image;

                    }
                }
                else
                {
                    if (u.PartitionKey == name)
                    {
                        BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\default.png"));
                        pictureCopy.ImageSource = image;
                    }
                }
                if (u.UserID != null)
                {
                    if (System.IO.File.Exists(path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png"))
                    {
                        if (!string.IsNullOrEmpty(u.Picture))
                            u.Picture = path + "\\SecretChat\\ProfilePictures\\" + u.UserID + ".png";
                        else
                            u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    }
                    else
                    {
                        u.Picture = path + "\\SecretChat\\ProfilePictures\\default.png";
                    }
                }
            }
        }

        private void NotificationReminder()
        {
            this.Topmost = true;
            this.ShowInTaskbar = true;
            if (System.IO.File.ReadAllText(path + "\\secretchat\\notificationreminder.txt") != "false")
            {
                this.Hide();
                ModernMessageBox modernMessageBox = new ModernMessageBox();
                modernMessageBox.CheckboxVisibility = Visibility.Visible;
                modernMessageBox.CheckboxText = "Nicht mehr anzeigen";
                modernMessageBox.Title = "Für Secret Chat Benachrichtigungen, deaktiviere 'Fokus Assitent'";
                modernMessageBox.Message = "Stelle sicher 'Fokus Assitent' von Windows zu deaktivieren um Benachrichtigungen zu empfangen. Drücke WIN + A um ins Aktions-Center zu gelangen. Soll eine Tutorial-Seite geöffnet werden?";
                modernMessageBox.Button2Status = ModernMessageboxButtonStatus.Normal;
                modernMessageBox.Button1Text = "Ja";
                modernMessageBox.Button2Text = "Nein";
                modernMessageBox.Background = (Brush)(new BrushConverter().ConvertFrom("#FF2D2D30"));
                modernMessageBox.ShowInTaskbar = true;
                modernMessageBox.BringIntoView();
                modernMessageBox.ShowDialog();
                if (modernMessageBox.Result == ModernMessageboxResult.Button1)
                {
                    Process.Start("https://support.microsoft.com/en-us/windows/turn-focus-assist-on-or-off-in-windows-10-5492a638-b5a3-1ee0-0c4f-5ae044450e09#:~:text=Here%27s%20how%20to%20turn%20focus%20assist%20on%20or,search%20box%20on%20the%20taskbar%2C%20and%20then%20");
                }
                if (modernMessageBox.CheckboxChecked == true)
                {
                    System.IO.File.WriteAllText(settingsPath + "notificationreminder.txt", "false");
                }
            }
            Topmost = false;
            this.Show();
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
            if (item.from == name || item.Friend == name)
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
                    Bitmap bitmap = new Bitmap(60, 60, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
            HideTaskbarOverlayAndFlash(e);
        }

        private void HideTaskbarOverlayAndFlash(ScrollChangedEventArgs e)
        {
            currentlyAtTheBottom = false;
            if ((e.VerticalOffset + e.ViewportHeight > e.ExtentHeight - 2))
            {
                if (ApplicationIsActivated() == true)
                {
                    if (currentChannel == "<General>")
                    {
                        GeneralPoint.Visibility = Visibility.Hidden;
                        General.FontWeight = FontWeights.DemiBold;
                        General.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                    }
                    else if (currentChannel == "<Channel 1>")
                    {
                        Channel1Point.Visibility = Visibility.Hidden;
                        Channel1.FontWeight = FontWeights.DemiBold;
                        Channel1.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                    }
                    else
                    {
                        foreach (var f in friendsModel.Friends.ToList())
                        {
                            if (f.Friend == to)
                            {
                                if (f.PointVisibility == "Visible")
                                {
                                    f.TextColor = "White";
                                    f.PointVisibility = "Hidden";
                                    f.TextWeight = "DemiBold";
                                    Friendslist.Items.Refresh();
                                    Friendslist.UpdateLayout();
                                }
                                break;
                            }
                        }
                    }
                    //point visiblitly
                    currentlyAtTheBottom = true;
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
                    this.UpdateLayout();
                }
            }



        }


        private void List_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            HandleClickOnMsg();
        }

        private void HandleClickOnMsg()
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
                            System.Windows.Clipboard.SetText(SlctdItm.Message);
                        }
                        catch
                        {

                        }
                    }

                }

            }


        }

        private void HandleClickOnMsgChannel1()
        {
            if (Channel1List.SelectedItem != null)
            {
                var SlctdItm = Channel1List.SelectedItem as MessageEntity;
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
                        System.Windows.Clipboard.SetText(SlctdItm.Message);
                    }
                    catch
                    {

                    }
                }



            }
        }

        private void HandleClickOnMsgFriendChat()
        {
            if (FriendChat.SelectedItem != null)
            {
                var SlctdItm = FriendChat.SelectedItem as MessageEntity;
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
                        System.Windows.Clipboard.SetText(SlctdItm.Message);
                    }
                    catch
                    {

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
            if (sender is System.Windows.Controls.ListView listView)
            {
                listView.SelectedIndex = -1;
            }
        }

        private void LastKeyPressedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timerElapsed = true;
        }


        private void ShiftIsActiveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            shiftIsActiveTimer.Stop();
        }

        void Window1_StateChanged(object sender, EventArgs e)
        {
            //if (WindowState == WindowState.Maximized)
            //    list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            //else
            //{
            //    list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Visible);
            //    if (WindowState != WindowState.Minimized)
            //    {

            //    }
            //}
        }




        private void CloseToolTipTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => tooltip.Visibility = tooltip.Visibility = Visibility.Hidden));
            Dispatcher.BeginInvoke((Action)(() => tooltip.IsOpen = false));
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
                StartAndPrepare(startInfo);
            }
            catch (Exception ex)
            {
                var st = new StackTrace(ex, true);
                var frame = st.GetFrame(0);
                var line = frame.GetFileLineNumber();
                crash = true;
                ErrorInfo = "Ein Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an Dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in Deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + ex.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                System.Windows.MessageBox.Show(ErrorInfo);
                System.Windows.Clipboard.SetText(ErrorInfo);
                Environment.Exit(0);
                //ErrorInfo = "Ein unbekannter Fehler ist aufgetreten, es tut uns Leid, es liegt nicht an dir, sondern an uns, damit dies nicht wieder vorkommt sende den Fehler (er wurde in Deine Zwischenablage gespeichert) an folgende Email-Adresse: armulic@live.de. Secret Chat® wird nun automatisch beendet. Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
            }
        }


        private void HideShowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (HideShowBtn.Content.ToString() == "ˆ")
            {
                GeneralItem.Visibility = Visibility.Visible;
                Channel1Item.Visibility = Visibility.Visible;
                UpdateInfosItem.Visibility = Visibility.Visible;
                HideShowBtn.Content = "ˇ";
            }
            else
            {
                GeneralItem.Visibility = Visibility.Collapsed;
                Channel1Item.Visibility = Visibility.Collapsed;
                UpdateInfosItem.Visibility = Visibility.Collapsed;
                HideShowBtn.Content = "ˆ";
            }
        }

        private void ListView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

            if (ChannelsList.SelectedItem != null)
            {
                to = null;
                if (this.IsActive == true)
                {
                    var SlctdItm = ChannelsList.SelectedItem;
                    if (SlctdItm != ChannelsListItem)
                    {
                        chatbox.IsEnabled = true;
                        InfoBar.Visibility = Visibility.Collapsed;
                        InfoBar.UpdateLayout();
                        Friendslist.UnselectAll();
                        //onlineUsersController.Visibility = "Collapsed";
                        onlineUsersController.ChatVisibility = Visibility.Collapsed;
                        Channel1List.Visibility = Visibility.Collapsed;
                        list.Visibility = Visibility.Collapsed;
                        UpdateInfoList.Visibility = Visibility.Collapsed;
                        General.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                        Channel1.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                        UpdateInfos.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                        if (SlctdItm == GeneralItem)
                        {
                            GeneralPoint.Visibility = Visibility.Hidden;
                            list.Visibility = Visibility.Visible;
                            currentChannel = "<General>";
                            //list.UpdateLayout();
                            ScrollToBottom();
                            defaultSelection = false;
                            General.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                            General.FontWeight = FontWeights.Normal;
                        }
                        else if (SlctdItm == Channel1Item)
                        {
                            Channel1Point.Visibility = Visibility.Hidden;
                            Channel1List.Visibility = Visibility.Visible;
                            currentChannel = "<Channel 1>";
                            ScrollToBottomChannel1();
                            Channel1.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                            Channel1.FontWeight = FontWeights.Normal;
                        }
                        else if (SlctdItm == UpdateInfosItem)
                        {
                            //currentChannel = "<Update-Infos>";
                            sendButton.Visibility = Visibility.Hidden;
                            chatbox.IsEnabled = false;
                            UpdateInfos.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                            UpdateInfos.FontWeight = FontWeights.Normal;
                            UpdateInfoList.Visibility = Visibility.Visible;


                        }
                        foreach (var F in friendsModel.Friends)
                        {
                            if (F.PointVisibility == "Hidden")
                            {
                                F.TextColor = "#FF6E7178";
                            }

                            Friendslist.Items.Refresh();
                        }
                        Dispatcher.BeginInvoke((Action)(() => chatbox.Focus()));
                    }
                }


            }

        }

        private static string GetUpdateNews()
        {
            var client = new HttpClient();
            HttpResponseMessage result = client.GetAsync("https://raw.githubusercontent.com/SagMeinenNamen/SecretChat/main/Update-Infos.md").Result;
            return result.Content.ReadAsStringAsync().Result;
        }

        private void HideShowBtn2_Click(object sender, RoutedEventArgs e)
        {
            if (HideShowBtn2.Content.ToString() == "ˆ")
            {
                HideShowBtn2.Content = "ˇ";
                Friendslist.Visibility = Visibility.Visible;
            }
            else
            {
                Friendslist.Visibility = Visibility.Collapsed;
                HideShowBtn2.Content = "ˆ";
            }
        }

        public static void CloseWIndowUsingIdentifier(string windowTag)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w.GetType().Assembly == currentAssembly && w.Tag.Equals(windowTag))
                {
                    w.Close();
                    break;
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.ReadAllText(path + "\\secretchat\\sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(SecretChat.Properties.Resources.mixkit_fast_double_click_on_mouse_275);
                playSound.Play();
            }
            AddFriendBtn.Visibility = Visibility.Collapsed;
            FriendNameBox.Visibility = Visibility.Visible;
            CancelBtn.Visibility = Visibility.Visible;
            VerifyAddFriend.Visibility = Visibility.Visible;


        }

        private void VerifyAddFriend_Click(object sender, RoutedEventArgs e)
        {
            //ChannelsList.Items.Add(FriendItem);
            if (Regex.IsMatch(FriendNameBox.Text, name, RegexOptions.IgnoreCase) && FriendNameBox.Text.Length == name.Length)
            {
                ModernWpf.MessageBox.Show("Freunde sind ANDERE Menschen, die dich gut kennen und mit denen Du Zeit verbringst, WICHTIGE INFO FÜR DICH: es muss eine andere Person sein ;)");
                return;
            }
            else if (!string.IsNullOrEmpty(FriendNameBox.Text))
            {
                var friendName = messageController.GetAllMessagesNoQuery(name, macAndUser, code, FriendNameBox.Text);
                if (friendName != null)
                {
                    foreach (var item in friendName)
                    {
                        if (Regex.IsMatch(FriendNameBox.Text, item.Friend, RegexOptions.IgnoreCase) && FriendNameBox.Text.Length == item.Friend.Length)
                        {
                            foreach (var f in friendsModel.Friends)
                            {
                                if (f.Friend == item.Friend)
                                {
                                    ModernWpf.MessageBox.Show("Dieser Nutzer ist bereits Dein Freund!");
                                    return;
                                }
                            }
                            HideShowBtn2.Content = "ˇ";
                            Friendslist.Visibility = Visibility.Visible;
                            item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                            item.TextColor = "#FF6E7178";
                            //File.AppendAllText(settingsPath + "ignoreList.txt", FriendNameBox.Text + Environment.NewLine);
                            if (File.Exists(settingsPath + "ignoreList.txt"))
                            {
                                var linesRead = File.ReadLines(settingsPath + "ignoreList.txt");
                                int count = 0;
                                foreach (var lineRead in linesRead)
                                {
                                    if (Regex.IsMatch(lineRead, FriendNameBox.Text, RegexOptions.IgnoreCase))
                                    {
                                        List<string> linesList = File.ReadAllLines(settingsPath + "ignoreList.txt").ToList();
                                        linesList.RemoveAt(count);
                                        Dispatcher.BeginInvoke((Action)(() => File.WriteAllLines(settingsPath + "ignoreList.txt", linesList.ToArray())));
                                    }
                                    count++;
                                }
                            }
                            friendsModel.Friends.Add(item);
                            Friendslist.Items.Refresh();
                            AddFriendBtn.Visibility = Visibility.Visible;
                            FriendNameBox.Visibility = Visibility.Collapsed;
                            CancelBtn.Visibility = Visibility.Collapsed;
                            VerifyAddFriend.Visibility = Visibility.Collapsed;
                            FriendNameBox.Text = "";
                            return;
                        }
                    }
                }
                ModernWpf.MessageBox.Show("Nutzer wurde nicht gefunden, stelle bitte sicher, dass die Person schon mindestens eine Nachricht in Secret Chat verschickt hat.");
                return;
            }

        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            FriendNameBox.Text = "";
            AddFriendBtn.Visibility = Visibility.Visible;
            FriendNameBox.Visibility = Visibility.Collapsed;
            CancelBtn.Visibility = Visibility.Collapsed;
            VerifyAddFriend.Visibility = Visibility.Collapsed;
        }

        private void Friendslist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //InfoBar.Visibility = Visibility.Visible;
            //Channel1List.Visibility = Visibility.Collapsed;
            //list.Visibility = Visibility.Collapsed;
            //UpdateInfoList.Visibility = Visibility.Collapsed;
            //onlineUsersController.ChatVisibility = Visibility.Visible;
            //FriendChat.UpdateLayout();
            //Chat.UpdateLayout();
            //this.UpdateLayout();
            //////Task.Delay(3000);
            //Thread.Sleep(1000);
            //onlineUsersController.ChatVisibility = Visibility.Visible;
            //FriendChat.UpdateLayout();
            ////onlineUsersController.Visibility = "Collapsed";
            //Chat.UpdateLayout();
            //this.UpdateLayout();
            if (Friendslist.SelectedItem != null)
            {
                if (this.IsActive == true)
                {
                    mouseReleased = false;
                    chatbox.SelectionLength = 0;
                    var SlctdItm = Friendslist.SelectedItem;
                    InfoBar.Visibility = Visibility.Visible;
                    Channel1List.Visibility = Visibility.Collapsed;
                    list.Visibility = Visibility.Collapsed;
                    UpdateInfoList.Visibility = Visibility.Collapsed;
                    onlineUsersController.ChatVisibility = Visibility.Visible;
                    ChannelsList.UnselectAll();
                    chatbox.IsEnabled = true;
                    General.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                    Channel1.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                    UpdateInfos.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF6E7178"));
                    if (friendsModel.Messages != null)
                    {
                        friendsModel.Messages.Clear();
                    }
                    FriendChat.Items.Refresh();
                    //FriendChat.UpdateLayout();
                    foreach (var F in friendsModel.Friends.ToList())
                    {
                        if (SlctdItm == F)
                        {
                            to = F.Friend;
                        }
                    }
                    bool offline = true;
                    foreach (var user in onlineAndAbsentUsers)
                    {
                        if (user.userName == to)
                        {
                            offline = false;
                            if (user.Status == "online")
                            {
                                Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Online"));
                                break;
                            }
                            else if (user.Status == "absent")
                            {
                                Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Abwesend"));
                                break;
                            }
                        }
                    }
                    if (offline == true)
                    {
                        Dispatcher.BeginInvoke((Action)(() => FriendStatus.Text = "Offline"));
                    }

                    var allMessages = messageController.FriendMessages(name, macAndUser, code, to);
                    var sortedAllMessages = allMessages.OrderBy(s => s.Time);
                    friendsModel.Messages = new List<MessageEntity>();
                    friendsModel.Messages.Clear();
                    var lastItem = new MessageEntity();
                    foreach (var F in friendsModel.Friends.ToList())
                    {
                        F.TextColor = "#FF6E7178";
                        if (SlctdItm == F)
                        {
                            FriendName.Text = F.Friend;
                            F.TextColor = "White";
                            F.PointVisibility = "Hidden";
                            F.TextWeight = "DemiBold";

                            foreach (var item in sortedAllMessages)
                            {
                                //if (friendsModel.Messages.Count != 0)
                                //{
                                //    foreach (var m in friendsModel.Messages)
                                //    {
                                        if (lastItem.Time.ToLongDateString() + lastItem.Time.ToShortTimeString() == item.Time.ToLongDateString() + item.Time.ToShortTimeString())
                                        {
                                            item.MessageOrInfo = "Visible";
                                            if (lastItem.from == item.from)
                                            {
                                                item.MessageOrInfo = "Collapsed";
                                            }
                                        }

                                //    }
                                //}
                                //else
                                //{
                                //    item.MessageOrInfo = "Visible";
                                //}
                                lastItem = item;
                                SetColorsForItems(item);
                                item.User = storedUsers.SingleOrDefault(user => user.PartitionKey == item.from);
                                InsertEmotes(item);
                                friendsModel.Messages.Add(item);
                                FriendChat.Items.Refresh();
                            }

                            //foreach (var i in ListModel.MessagesModelsList)
                            //{
                            //    if (i == F)
                            //        }
                            //if (!ListModel.MessagesModelsList.Contains(F)
                            //friendsModel.FriendsMessages.Add(item);
                            friendsModel.Messages.OrderBy(s => s.Time);
                            FriendChat.ItemsSource = friendsModel.Messages;
                            FriendChat.Items.Refresh();
                            F.TextColor = "White";
                            F.UnderlinedOrNot = "None";
                            currentChannel = null;
                        }
                    }
                    ScrollToBottomFriendChat();
                    Friendslist.Items.Refresh();
                    this.UpdateLayout();
                    chatbox.Focus();
                    //}));
                }
            }
            //onlineUsersController.ChatVisibility = "Visible";
            //onlineUsersController.ChatVisibility = Visibility.Visible;
        }




        private void Friendslist_LostFocus(object sender, RoutedEventArgs e)
        {
        }



        private void Sound2()
        {
            if (System.IO.File.ReadAllText(settingsPath + "sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.mixkit_fast_double_click_on_mouse_275);
                playSound.Play();
            }
        }

        private void NotificationSound()
        {
            if (System.IO.File.ReadAllText(settingsPath + "sounds.txt") == "true")
            {
                SoundPlayer playSound = new SoundPlayer(Properties.Resources.notificationSound);
                playSound.Play();
            }
        }

        private void ChangeNameButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            var dialogResult = ModernWpf.MessageBox.Show("Wenn Du deinen Namen änderst wird Dein Profilbild, als auch Deine Vertifizierung zurückgesetzt. Bist Du dir sicher?", "Fortfahren?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (dialogResult == MessageBoxResult.Yes)
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code = null;
                if (System.IO.File.Exists(settingsPath + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(settingsPath + "code.txt");
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
                string shortcutLocation = System.IO.Path.Combine(desktop, "Secret Chat" + ".lnk");
                WshInterop.CreateShortcut(shortcutLocation, "Secret Chat", pathToExe, null, null);
                ModernWpf.MessageBox.Show("Verknüpfung wurde erfolgreich erstellt!");
            }
            catch
            {
                ModernWpf.MessageBox.Show("Es gab einen Fehler beim erstellen; womöglich gibt es schon eine Verknüpfung.");
            }
        }

        private void ButtonSettings_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            ModernWpf.MessageBox.Show("Du benutzt SecretChat® v." + version + " entwickelt von Armin Mulic, Idee und Designs von Psenix, Logo Design von ArcadeToast. Alle rechte vorbehalten. Copyright © 2021");
        }

        private void GoBackBtn_Click(object sender, RoutedEventArgs e)
        {
            SettingsGrid.Visibility = Visibility.Collapsed;
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
                System.IO.File.WriteAllText(settingsPath + "notifications.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "notifications.txt", "false");

            }
        }


        private void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (AutoStart.IsChecked == true)
            {
                System.IO.File.WriteAllText(settingsPath + "autostart.txt", "true");
                string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string pathToExe = Directory.GetCurrentDirectory() + "\\" + Process.GetCurrentProcess().ProcessName + ".exe";
                string shortcutLocation = System.IO.Path.Combine(startUpPath, "Secret Chat" + ".lnk");
                WshInterop.CreateShortcut(shortcutLocation, "Secret Chat", pathToExe, null, null);
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "autostart.txt", "false");
                string startUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                System.IO.File.Delete(startUpPath + "\\Secret Chat.lnk");
            }
        }

        private void ButtonSettings_Click_1(object sender, RoutedEventArgs e)
        {
            var dialogResult = System.Windows.MessageBox.Show("Möchtest Du Deinen Account wirklich löschen? Beim löschen Deines Accounts wird Dein Benutzername frei. Alle Nachrichten die Du gesendet hast, als auch alle die an dich gesendet wurden werden gelöscht. Du musst nach dem Löschen Deines Accounts wieder einen neuen Account erstellen um Secret Chat® zu nutzen. Deine Vertifizierung , sowie alle Einstellungen werden ebenfalls zurückgesetzt. Wenn Du auf 'Ja' drückst wird Dein Account gelöscht und Secret Chat® beendet.", "ACCOUNT LÖSCHEN?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
            if (dialogResult == MessageBoxResult.Yes)
            {
                string macAndUser = GetMac() + Environment.UserName;
                string code;
                List<StoredUserEntity> storedUserNames;
                if (System.IO.File.Exists(settingsPath + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                    storedUserNames = tableStorageUsers.GetStoredUsers(macAndUser, code);
                }
                else
                {
                    System.Windows.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }


                try
                {
                    messageController.DeleteAllMessages(name, macAndUser, code);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Nicht alle Nachrichten, die Du geschickt hast oder die an dich gerichtet waren wurden gelöscht. Fehler: " + ex.Message);
                }
                System.IO.File.Delete(settingsPath + "autostart.txt");
                System.IO.File.Delete(settingsPath + "notifications.txt");
                System.IO.File.Delete(settingsPath + "timer.txt");
                System.IO.File.Delete(settingsPath + "counter.txt");
                System.IO.File.Delete(settingsPath + "darkorlight.txt");
                System.IO.File.Delete(settingsPath + "goodwords.txt");
                System.IO.File.Delete(settingsPath + "icon.png");
                System.IO.File.Delete(settingsPath + "info.txt");
                System.IO.File.Delete(settingsPath + "words.txt");
                System.IO.File.Delete(settingsPath + "links.txt");
                System.IO.File.Delete(settingsPath + "code.txt");
                System.IO.File.Delete(settingsPath + "mentions.txt");
                System.IO.File.Delete(settingsPath + "sounds.txt");
                System.IO.File.Delete(settingsPath + "notificationreminder.txt");
                System.IO.File.Delete(settingsPath + "emojis.txt");
                System.IO.File.Delete(settingsPath + "ignoreList.txt");
                try
                {
                    System.IO.Directory.Delete(settingsPath);
                }
                catch
                {
                    try
                    {
                        System.IO.Directory.Delete(settingsPath);
                    }
                    catch
                    {
                        var result = System.Windows.MessageBox.Show("Einige Dateien oder Ordner konnten nicht gelöscht werden, diese können manuell entfernt werden. Soll der Dateipfad geöffnet werden?", "Fehler", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(settingsPath);
                        }
                    }
                }
                var storedUserName = tableStorageUsers.GetSpecificUser(name, macAndUser, code);
                foreach (var u in storedUserName)
                {
                    if (u.storedMac == macAndUser)
                    {
                        string userName = u.PartitionKey;
                        try
                        {
                            string Response = onlineUsersController.DeleteOnlineUser(userName, macAndUser, code);
                        }
                        catch
                        {

                        }
                        try
                        {
                            string response = tableStorageUsers.DeleteStoredUser(userName, macAndUser, code);
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
                System.Windows.MessageBox.Show("Dein Account wurde gelöscht, Secret Chat® wird nun beendet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                Environment.Exit(0);
            }
        }


        public static void SaveJpeg(string path, System.Drawing.Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");

            // Encoder parameter for image quality 
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
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

            bool sucess = true;
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Image Files(*.PNG); (*.JPG)|*.PNG; *.JPG"/* + "|All files(*.*)|*.*"*/,
                CheckFileExists = true,
                Multiselect = false
            };
            if (fileDialog.ShowDialog() == true)
            {
                var user = tableStorageUsers.GetSpecificUserName(macAndUser, code);
                foreach (var u in user)
                {
                    string macAndUser = GetMac() + Environment.UserName;
                    string code;
                    List<StoredUserEntity> storedUsers;
                    if (System.IO.File.Exists(settingsPath + "code.txt"))
                    {
                        code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                        storedUsers = tableStorageUsers.GetStoredUsers(macAndUser, code);
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
                            if (System.IO.File.Exists(settingsPath + "ProfilePictures\\" + u.UserID + "copy.png"))
                            {
                                System.IO.File.Delete(settingsPath + "ProfilePictures\\" + u.UserID + "copy.png");
                            }
                            SaveJpeg(/*path + "ProfilePictures\\" + macOfDevice.Replace(":", ".") + ".png"*/ settingsPath + "ProfilePictures\\" + u.UserID + "copy.png", System.Drawing.Image.FromFile(fileDialog.FileName), i);
                            length = new FileInfo(settingsPath + "ProfilePictures\\" + u.UserID + "copy.png").Length;
                            i -= 5;
                            Task.Delay(50);

                        }
                        if (compressionNeeded == false)
                        {
                            SaveJpeg(/*path + "ProfilePictures\\" + macOfDevice.Replace(":", ".") + ".png"*/ settingsPath + "ProfilePictures\\" + u.UserID + "copy.png", System.Drawing.Image.FromFile(fileDialog.FileName), 95);
                        }

                        byte[] imageArray = System.IO.File.ReadAllBytes(settingsPath + "ProfilePictures\\" + u.UserID + "copy.png");
                        string base64Text = Convert.ToBase64String(imageArray);
                        var storedUser = tableStorageUsers.GetSpecificUserName(macAndUser, code);
                        foreach (var U in storedUser)
                        {
                            if (U.storedMac == macAndUser)
                            {
                                name = U.PartitionKey;
                            }
                        }
                        if (System.IO.File.Exists(settingsPath + "code.txt"))
                        {
                            code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                            string Response = tableStorageUsers.InsertUser(name, macAndUser, base64Text, "true", code, guid, null);
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
                        Environment.Exit(0);
                    }
                }
            }

        }




        private void ButtonSettings_Click_2(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound);
            //Task.Delay(400);
          
            if (ModeCheckbox.IsChecked == false)
            {
                System.IO.File.WriteAllText(settingsPath + "darkorlight.txt", "light");
                SetEverythingForLightMode();
                onlineUsersController.HoverColor = "#FFafcdf2";
                onlineUsersController.StillFocusedColor = "#FFafcdf2";
            }
            else
            {
                onlineUsersController.HoverColor = "#FF34373C";
                SetEverythingForDarkMode();
                onlineUsersController.StillFocusedColor = "#FF393C43";
                System.IO.File.WriteAllText(settingsPath + "darkorlight.txt", "dark");
            }
            var dialogResult = ModernWpf.MessageBox.Show("Secret Chat® sollte neugestartet werden um alles richtig einzustellen für den neuen Modus.", "Neustart empfohlen", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                var startInfo = Process.GetCurrentProcess().ProcessName;
                Process.Start(startInfo, "restart");
                Environment.Exit(0);
            }
            //}
            //else
            //{
            //    if (ModeCheckbox.IsChecked == true)
            //        ModeCheckbox.IsChecked = false;
            //    else
            //        ModeCheckbox.IsChecked = true;
            //}
        }

        private void RestartBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemTray.Dispose();
            var startInfo = Process.GetCurrentProcess().ProcessName;
            Process.Start(startInfo, "restart");
            Environment.Exit(0);
        }

        private void DefaultPicture_Click(object sender, RoutedEventArgs e)
        {
            var dialogResult = ModernWpf.MessageBox.Show("Bist Du sicher das Du Dein Profilbild entfernen möchtest?", "Sicher?", MessageBoxButton.YesNo, MessageBoxImage.Information);
            string vertification = "false";
            if (dialogResult == MessageBoxResult.Yes)
            {
                string code;
                if (System.IO.File.Exists(settingsPath + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string macAndUser = GetMac() + Environment.UserName;
                List<StoredUserEntity> storedUsers = null;
                if (System.IO.File.Exists(settingsPath + "code.txt"))
                {
                    code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                    storedUsers = tableStorageUsers.GetStoredUsers(macAndUser, code);
                }
                else
                {
                    ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var storedUser = tableStorageUsers.GetSpecificUserName(macAndUser, code);
                foreach (var u in storedUser)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                        vertification = u.Verification;
                    }
                }
                if (vertification == "true")
                {
                    string Response = tableStorageUsers.InsertUser(name, macAndUser, "x", "true", code, null, null);
                    if (Response != "True")
                    {
                        this.Hide();
                        System.Windows.MessageBox.Show(Response);
                        Environment.Exit(0);
                    }
                }
                else
                {
                    string Response = tableStorageUsers.InsertUser(name, macAndUser, "x", "null", code, null, null);
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
                Environment.Exit(0);
            }
        }

        private void Links_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Links.IsChecked == true)
            {
                System.IO.File.WriteAllText(settingsPath + "links.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "links.txt", "false");
            }
        }

        private void Sounds_Click(object sender, RoutedEventArgs e)
        {
            if (Sounds.IsChecked == true)
            {
                System.IO.File.WriteAllText(settingsPath + "sounds.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "sounds.txt", "false");
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
            SettingsGrid.Visibility = Visibility.Visible;
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => SystemTray.Dispose()));
            Environment.Exit(0);
        }

        private void Credits_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Mitwirkende%20&%20Anerkennungen");
        }

        private void SCProject_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://secret-chat.jimdosite.com/");
        }

        private void Mentions_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Mentions.IsChecked == true)
            {
                System.IO.File.WriteAllText(settingsPath + "mentions.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "mentions.txt", "false");

            }
        }

        private void ButtonSettings_Click_3(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Du bekommst bei jeder neuen Nachricht im Chat eine Benachrichtigungen, wenn Du weiterhin Erwähnungen angeschaltet hast, werden diese Benachrichtigungen als Erwähnungen angezeigt.");
        }

        private void ButtonSettings_Click_4(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Du bekommst nur Benachrichtigungen, wenn jemand dich erwähnt ( @[DeinName] ).");
        }

        private void ButtonSettings_Click_5(object sender, RoutedEventArgs e)
        {
            ModernWpf.MessageBox.Show("Damit Du Benachrichtigungen empfängst, gehe bitte auf das Windows Aktions-Center (Windows logo Taste + A) und stelle den 'Fokus Assistent' auf aus.");
        }

        private void Emojis_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(Sound2);
            if (Emojis.IsChecked == true)
            {
                System.IO.File.WriteAllText(settingsPath + "emojis.txt", "true");
            }
            else
            {
                System.IO.File.WriteAllText(settingsPath + "emojis.txt", "false");

            }
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (crash == true)
            {
                System.Windows.Clipboard.SetText(ErrorInfo);
                ModernWpf.MessageBox.Show(ErrorInfo);
                Task.Delay(5000);
                Environment.Exit(0);
            }
        }

        private void FriendChat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (defaultSelection == false)
                HandleClickOnMsgFriendChat();
            else
                defaultSelection = false;
        }

        private void Channel1List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (defaultSelection == false)
                HandleClickOnMsgChannel1();
            else
                defaultSelection = false;
        }

        private void Channel1List_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HideTaskbarOverlayAndFlash(e);
        }

        private void FriendChat_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HideTaskbarOverlayAndFlash(e);

        }

        private void RequestVerification_Click(object sender, RoutedEventArgs e)
        {
            string macAndUser = GetMac() + Environment.UserName;
            string code;
            List<StoredUserEntity> storedUser;
            if (System.IO.File.Exists(settingsPath + "code.txt"))
            {
                code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                storedUser = tableStorageUsers.GetSpecificUser(name, macAndUser, code);
            }
            else
            {
                ModernWpf.MessageBox.Show("Fehler; es wurde kein Zugangs-Code gefunden, starte Secret Chat neu und gebe deinen Zugangs-Code ein.", "Ungültiger Authentifizierungscode", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (storedUser != null)
            {
                foreach (var u in storedUser)
                {
                    if (u.storedMac == macAndUser)
                    {
                        name = u.PartitionKey;
                        guid = u.UserID;
                    }
                }
            }
            else
            {
                ModernWpf.MessageBox.Show("Fehler; Dein Secret Chat Account konnte nicht gefunden werden", "Account wurde nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (System.IO.File.Exists(settingsPath + "code.txt"))
            {
                code = System.IO.File.ReadAllText(settingsPath + "code.txt");
                string Response = tableStorageUsers.InsertUser(name, macAndUser, "", "false", code, guid, null);
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
            imageElements[0].Attributes.GetNamedItem("src").NodeValue = settingsPath + "icon.png";
            //var newAttribute = toastXml.CreateAttribute("placement");
            //newAttribute.Value = "appLogoOverride";
            //imageElements[0].Attributes.SetNamedItem(newAttribute);



            var toast = new ToastNotification(toastXml);
            //toast.Failed += ToastFailed;
            ToastNotificationManager.CreateToastNotifier("Secret Chat").Show(toast);
            RequestVerification.IsEnabled = false;
        }

        private void Datenschutz_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Datenschutzerkl%C3%A4rung.md");
        }

        private void Nutzungsbedingungen_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat/blob/main/Nutzungsbedingungen.md");
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

        private void EmojiBtn_Click(object sender, RoutedEventArgs e)
        {
            CoreInputView.GetForCurrentView().TryShow(CoreInputViewKind.Emoji);
            EmojiBtnIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EmoticonHappy;
            chatbox.Focus();
        }

        private void EmojiBtnIcon_MouseEnter(object sender, MouseEventArgs e)
        {
            EmojiBtnIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EmoticonHappy;
        }

        private void EmojiBtnIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            EmojiBtnIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EmoticonHappyOutline;
        }

        private void StatusInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StatusInfo.IsFocused == true)
            {
                CancelInfoBtn.Visibility = Visibility.Visible;
                SaveInfoBtn.Visibility = Visibility.Visible;
            }
        }

        private void CancelInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            var storedUsers = tableStorageUsers.GetSpecificUser(name, macAndUser, code);
            StoredUserEntity User = storedUsers.ElementAt(0);
            StatusInfo.Text = User.Info;
            CancelInfoBtn.Visibility = Visibility.Hidden;
            SaveInfoBtn.Visibility = Visibility.Hidden;
        }

        private void SaveInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (StatusInfo.Text.Length < 140)
            {
                string Response = tableStorageUsers.InsertUser(name, macAndUser, null, null, code, guid, StatusInfo.Text);
                if (Response != "True")
                {
                    this.Hide();
                    System.Windows.MessageBox.Show(Response);
                    Environment.Exit(0);
                }
                CancelInfoBtn.Visibility = Visibility.Hidden;
                SaveInfoBtn.Visibility = Visibility.Hidden;
            }
            else
                ModernWpf.MessageBox.Show("Deine Info darf nicht über 140 Zeichen lang sein! Aktuelle Zeichen: " + StatusInfo.Text.Length);

        }

        private void SettingsMenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsMenuList.SelectedItem != null)
            {
                if (this.IsActive == true)
                {
                    var SlctdItm = SettingsMenuList.SelectedItem;
                    if (SlctdItm == MyAccountItem)
                    {
                        PersonalaziationList.Visibility = Visibility.Collapsed;
                        NotificationSettingsList.Visibility = Visibility.Collapsed;
                        OtherList.Visibility = Visibility.Collapsed;
                        MyAccountList.Visibility = Visibility.Visible;
                    }
                    else if (SlctdItm == PersonalizationItem)
                    {
                        MyAccountList.Visibility = Visibility.Collapsed;
                        NotificationSettingsList.Visibility = Visibility.Collapsed;
                        OtherList.Visibility = Visibility.Collapsed;
                        PersonalaziationList.Visibility = Visibility.Visible;
                    }
                    else if (SlctdItm == NotificationSettingsItem)
                    {
                        MyAccountList.Visibility = Visibility.Collapsed;
                        PersonalaziationList.Visibility = Visibility.Collapsed;
                        OtherList.Visibility = Visibility.Collapsed;
                        NotificationSettingsList.Visibility = Visibility.Visible;
                    }
                    else if (SlctdItm == OtherItem)
                    {
                        MyAccountList.Visibility = Visibility.Collapsed;
                        PersonalaziationList.Visibility = Visibility.Collapsed;
                        NotificationSettingsList.Visibility = Visibility.Collapsed;
                        OtherList.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            Sound2();
            foreach (var u in storedUsers)
            {
                if (u.PartitionKey == to)
                {
                    try
                    {
                        darkwindow.Close();
                    }
                    catch
                    {

                    }

                    darkwindow = new Window();
                    this.Opacity = 0.5;
                    this.Effect = new BlurEffect();
                    //darkwindow.Owner = this;
                    //darkwindow.ShowInTaskbar = false;
                    //darkwindow.Topmost = true;
                    UserProfileWindow profileWindow = new UserProfileWindow(u, darkwindow, this);
                    darkwindow.Focusable = true;
                    profileWindow.Show();
                    //darkwindow.WindowStyle = WindowStyle.None;
                    //darkwindow.WindowState = WindowState.Maximized;
                    //darkwindow.ShowDialog();
                    break;
                }
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            onlineUsersController.ChatVisibility =Visibility.Collapsed;
            //FriendChat.Visibility = Visibility.Collapsed;
            FriendChat.InvalidateVisual();

            Task.Delay(2000).Wait();
            onlineUsersController.ChatVisibility = Visibility.Visible;

            //FriendChat.Visibility = Visibility.Visible;

            FriendChat.InvalidateVisual();
            Task.Delay(2000).Wait();
            onlineUsersController.ChatVisibility = Visibility.Collapsed;

            //FriendChat.Visibility = Visibility.Collapsed;

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            onlineUsersController.ChatVisibility = Visibility.Visible;

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