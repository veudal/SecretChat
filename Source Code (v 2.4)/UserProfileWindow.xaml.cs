using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for UserProfileWindow.xaml
    /// </summary>
    public partial class UserProfileWindow : Window
    {
        bool mouseIsOnWindow = false;
        Window DarkWindow = null;
        MainWindow MainWindow = null;
        readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        readonly string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\secretchat\\";
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public UserProfileWindow(StoredUserEntity storedUser,Window darkwindow , MainWindow mainWindow)
        {
            DarkWindow = darkwindow;
            MainWindow = mainWindow;
            InitializeComponent();
            ShowInTaskbar = false;
            NameBlock.Text = storedUser.PartitionKey;
            SetPicture(storedUser);
            Info.Text = storedUser.Info;
            if(Info.Text.Length < 1)
            {
                Info.Text = "Keine Info angegeben.";
            }
            CenterWindowOnScreen();
        }
        private void SetPicture(StoredUserEntity storedUser)
        {
            if (File.Exists(path + "\\SecretChat\\ProfilePictures\\" + storedUser.UserID + ".png"))
            {
                BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\" + storedUser.UserID + ".png"));
                ProfilePicture.ImageSource = image;
            }
            else
            {
                BitmapImage image = new BitmapImage(new Uri(path + "\\SecretChat\\ProfilePictures\\default.png"));
                ProfilePicture.ImageSource = image;
            }
        }
        private void CenterWindowOnScreen()
        {
            //double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            //double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            //double windowWidth = this.Width;
            //double windowHeight = this.Height;
            //this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            //this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
            //this.Left = (MainWindow.ActualWidth / 2) - (MainWindow.Left / 2) -350; /*(MainWindow.Left + MainWindow.Left) * -1;*/
            //this.Top = (MainWindow.ActualHeight / 2) - (MainWindow.Top / 2) - 300;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            Topmost = true;
            this.Activate();
        }


        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            mouseIsOnWindow = true;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            mouseIsOnWindow = false;
        }


        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (mouseIsOnWindow == false)
            {
                try
                {
                    MainWindow.Opacity = 1;
                    MainWindow.Effect = null;
                    DarkWindow.Hide();
                    this.Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Fehler: " +  ex.Message);
                }
            }
            //else
            //{
            //    this.Show();
            //    this.Activate();
            //}

            
        }

        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            mouseIsOnWindow = true;
            var result = ModernWpf.MessageBox.Show("Wenn du Deinen Freund entfernst, wird er nicht weiter in deiner Freundes-Liste angezeigt. Der Chat bleibt weiterhin verfügbar. Um auf ihn zuzugreifen, füge ihn einfach wieder als Freund hinzu.", "Freund entfernen?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {

                File.AppendAllText(settingsPath + "ignoreList.txt", NameBlock.Text + Environment.NewLine);
                foreach (MessageEntity F in MainWindow.friendsModel.Friends.ToList())
                {
                    if (F.Friend == NameBlock.Text)
                    {
                        MainWindow.friendsModel.Friends.Remove(F);
                        MainWindow.Friendslist.Items.Refresh();
                    }
                }
                //MainWindow.Friendslist
                //ModernWpf.MessageBox.Show("Freund wurde erfolgreich entfernt", "Info" , MessageBoxButton.OK);
                try
                {
                    MainWindow.Opacity = 1;
                    MainWindow.Effect = null;
                    MainWindow.onlineUsersController.ChatVisibility = Visibility.Collapsed;
                    MainWindow.list.Visibility = Visibility.Visible;
                    MainWindow.InfoBar.Visibility = Visibility.Collapsed;
                    MainWindow.GeneralPoint.Visibility = Visibility.Hidden;
                    MainWindow.currentChannel = "<General>";
                    MainWindow.General.Foreground = (Brush)(new BrushConverter().ConvertFrom("White"));
                    MainWindow.General.FontWeight = FontWeights.Normal;
                    DarkWindow.Hide();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler: " + ex.Message);
                }
            }
            else
            {
                this.Activate();
                this.Show();
                mouseIsOnWindow = false;
            }

        }
    }
}
