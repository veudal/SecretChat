using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for ChangeTimerWindow.xaml
    /// </summary>
    public partial class ChangeTimerWindow : Window
    {
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            this.Top = (screenHeight / 2) - (windowHeight / 2) - 110;
        }
        public ChangeTimerWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            this.Topmost = true;
            Title = "Intervall ändern";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
            if (dorl == "light")
            {
                Grid.Background = (Brush)(new BrushConverter().ConvertFrom("#FFF7F7F7"));
                Text.Foreground = (Brush)(new BrushConverter().ConvertFrom("#FF0083FF"));
            }
        }

        private void TimerVertiffy_Click(object sender, RoutedEventArgs e)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            try
            {
                if (Convert.ToInt32(timerBox.Text) > 199)
                {
                    if (Convert.ToInt32(timerBox.Text) < 2001)
                    {
                        File.WriteAllText(path + "\\SecretChat\\timer.txt", timerBox.Text);
                        ModernWpf.MessageBox.Show("Erfolgreich geändert! Es wird nun alle " + timerBox.Text + " Millisekunden nach neuen Nachrichten und neuen aktiven/abwesenden Nutzern geprüft.");
                        this.Close();
                    }
                    else
                    {
                        ModernWpf.MessageBox.Show("Der Betrag darf nicht höcher als 2000 sein");
                    }
                }
                else
                {
                    ModernWpf.MessageBox.Show("Der Betrag muss mindestens 200 sein");
                }
            }
            catch
            {
                ModernWpf.MessageBox.Show("Bitte gebe eine Zahl an.");
            }
        }


        private void TimerBox_GotFocus(object sender, RoutedEventArgs e)
        {
            timerBox.Clear();
        }

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SagMeinenNamen/SecretChat");
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
