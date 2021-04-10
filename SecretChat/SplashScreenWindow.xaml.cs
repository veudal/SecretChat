using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace SecretChat
{
    /// <summary>
    /// Interaction logic for SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public SplashScreenWindow(bool log)
        {
            InitializeComponent();
            ShowInTaskbar = false;
            Title = "Loading Secret Chat...";
            CenterWindowOnScreen();
            this.Topmost = true;
            Task.Run(Progress);
            if(log == true)
                Task.Run(Log);


        }
        private async Task Progress()
        {
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "";
                }));
                await Task.Delay(300);
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = ".";
                }));
                await Task.Delay(300);
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "..";
                }));
                await Task.Delay(300);

                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "...";
                }));
                await Task.Delay(300);
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "";
                }));
                await Task.Delay(300);
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = ".";
                }));
                await Task.Delay(300);
                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "..";
                }));
                await Task.Delay(300);

                await Dispatcher.BeginInvoke((Action)(() =>
                {
                    progressBlock.Text = "...";
                    Topmost = false;
                }));
                await Task.Delay(300);



        }
        private async Task Log()
        {
            List<string> logText = new List<string>();
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value = 5;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly...");
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value = 10;
                logView.Items.Add(@"Loaded 'C:\repos\SecretChat\SecretMess...");
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =  15 ;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...");
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =  20 ;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...");
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   27;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC... ");
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   35;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                           MSIL\System\v4.0_4.0.0.0__b77a5c561934e089\System.dll'.Skipped loading symbols. Module is optimized"); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   38;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                                        _32\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'.Skipped loading symbols. Module is optimized "); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   42;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                                          _MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll'.Skipped loading symbols. Module is optimize"); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   45;
                logView.Items.Add(@"Loaded 'C:\Program Files (x86)\Microsoft Visual...                                                         Studio\2019\Professional\Common7\IDE\PrivateAssemblies\Runtime\Microsoft.VisualStudio.Debugger.Runtime.Desktop.dll'."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   49;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                                          MSIL\System.Configuration\v4.0_4.0.0.0__b03f5f7f11d100a3a\System.Configuration.dll'.Skipped loading symbols."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =  54 ;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                                            MSIL\System.Xml\v4.0_4.0.0.0__b77a5c561934e089\System.Xml.dll'.Skipped loading symbols. "); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   60;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                                           _MSIL\mscorlib.resources\v4.0_4.0.0.0_de_b77a5c561934e089\mscorlib.resources.dll'.Module was built without symbols."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   65;
                logView.Items.Add(@"Loaded 'Microsoft.WindowsAzure.Storage'.Skipped...                   loading symbols. Module is optimized and the debugger option 'Just My Code' is enabled."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   70;
                logView.Items.Add(@"Loaded 'Newtonsoft.Json'.Skipped loading symbols...                   Module is optimized and the debugger option 'Just My Code' is enabled."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(50);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =  78 ;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                   MSIL\System.Numerics\v4.0_4.0.0.0__b77a5c561934e089\System.Numerics.dll'.Skipped loading symbols."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(50);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =  85 ;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                  _MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll'."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(50);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   88;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly\GAC...                  _32\System.Data\v4.0_4.0.0.0__b77a5c561934e089\System.Data.dll'.Skipped loading symbols."); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();

            }));
            await Task.Delay(50);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   92;
                logView.Items.Add(@"Loaded 'Anonymously Hosted DynamicMethods...                   Assembly'"); logView.Items.Refresh();
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(50);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   94;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly...                  \GAC_MSIL\System.Management\v4.0_4.0.0.0__b03f5f7f11d100a3a\System.Management.dll'.");

                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                bar.Value =   96;
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly...                                                       \GAC_MSIL\PresentationFramework.Aero2\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.Aero2.dll'.");

                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly...                                                       \GAC_MSIL\PresentationCore.resources\v4.0_4.0.0.0_de_31bf3856ad364e35\PresentationCore.resources.dll'.");
                bar.Value =  96 ;
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);

            await Dispatcher.BeginInvoke((Action)(() =>
            {
                logView.Items.Add(@"Loaded 'c:\program files (x86)\microsoft...                                                        visual studio\2019\professional\common7\ide\commonextensions\microsoft\xamldiagnostics\Framework\x86\Microsoft.VisualStudio.");
                bar.Value =  97 ;
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                logView.Items.Add(@"Loaded 'C:\WINDOWS\Microsoft.Net\assembly...                                                       \GAC_MSIL\SMDiagnostics\v4.0_4.0.0.0__b77a5c561934e089\SMDiagnostics.dll'.Skipped loading symbols. ");
                bar.Value =   97;
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));
            await Task.Delay(100);
            await Dispatcher.BeginInvoke((Action)(() =>
            {
                logView.Items.Add(@"Finishing up and cleaning setup...");
                bar.Value =  100 ;
                logView.SelectedIndex = logView.Items.Count - 1;
                logView.ScrollIntoView(logView.SelectedItem);
                logView.UnselectAll();
            }));

        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2) - 90;
            this.Top = (screenHeight / 2) - (windowHeight / 2)  - 110;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}

