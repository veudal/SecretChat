using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for TrayContextMenu.xaml
    /// </summary>
    public partial class TrayContextMenu : Window
    {
        public string ernc = "n";
        public TrayContextMenu()
        {
            ShowInTaskbar = false;
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            MoveBottomRightEdgeOfWindowToMousePosition();
            Grid.Visibility = Visibility.Visible;
            this.Topmost = true;

        }

        private void MoveBottomRightEdgeOfWindowToMousePosition()
        {
            try
            {
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var mouse = transform.Transform(GetMousePosition());
                Left = Math.Round(mouse.X) - Math.Round(ActualWidth);
                Top = Math.Round(mouse.Y) - Math.Round(ActualHeight);
                this.Show();
            }
            catch (Exception e)
            {
                var st = new StackTrace(e, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();
                string ErrorInfo = "Fehler:  " + e.Message + "  Line: " + line + " in " + frame + "              Der Fehler wurde in Deine Zwischenablage gespeichert.";
                ModernWpf.MessageBox.Show(ErrorInfo);
            }



        }

        public Point GetMousePosition()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            ernc = "c";
            this.Hide();

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Tray.Hide();
            ernc = "e";
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            Tray.Hide();
            ernc = "r";
        }

        private void Tray_Activated(object sender, EventArgs e)
        {
            ernc = "n";
            MoveBottomRightEdgeOfWindowToMousePosition();

        }


    }


}
