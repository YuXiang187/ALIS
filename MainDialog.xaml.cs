using System;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ALIS
{
    /// <summary>
    /// MainDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MainDialog : Window
    {
        private UdpClient udpClient;
        private IPEndPoint ep;
        private NotifyIcon notifyIcon;
        private DispatcherTimer hideTimer;
        public MainDialog()
        {
            InitializeComponent();
            InitializeTrayIcon();

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                Properties.Resources.warn.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            image.Source = imageSource;

            DoubleAnimation animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.3,
                Duration = TimeSpan.FromSeconds(0.6),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            image.BeginAnimation(OpacityProperty, animation);

            StartListening();
            InitializeHideTimer();
        }

        private void ReceiveWarn(string location, string ip)
        {
            if (Visibility != Visibility.Visible)
            {
                text.Text = $"{DateTime.Now:MM/dd HH:mm} {location}\n#{ip} 监测到人体活动，\n领导可能来巡查了！";
                Show();
            }
            hideTimer.Stop();
            hideTimer.Start();
        }

        private async void StartListening()
        {
            udpClient = new UdpClient(20002);
            ep = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 20001);
            byte[] data = Encoding.ASCII.GetBytes("pc"); // 发送pc信号
            _ = udpClient.Send(data, data.Length, ep);

            while (true)
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string message = Encoding.ASCII.GetString(result.Buffer);
                if (message == "warn")
                {
                    ReceiveWarn("未知区域", "255.255");
                }
                else if (message == "warn10")
                {
                    ReceiveWarn("高二(2)楼梯间", "1.10");
                }
                else if (message == "warn11")
                {
                    ReceiveWarn("高二(1)走廊间", "1.11");
                }
            }
        }

        private void InitializeHideTimer()
        {
            hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(4)
            };
            hideTimer.Tick += (sender, args) =>
            {
                hideTimer.Stop();
                Hide();
            };
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.warnico,
                Text = "YuXiang ALIS",
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };
            notifyIcon.ContextMenuStrip.Items.Add("退出").Click += (sender, args) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Top = 10;
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                using (Stream soundStream = Properties.Resources.warning)
                {
                    MemoryStream memoryStream = new MemoryStream();
                    soundStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    SoundPlayer soundPlayer = new SoundPlayer(memoryStream);
                    soundPlayer.Play();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}