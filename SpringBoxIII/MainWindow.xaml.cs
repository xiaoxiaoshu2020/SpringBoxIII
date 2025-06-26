using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Timers;
using System.Drawing;

namespace SpringBoxIII
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //点击穿透
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        //获取鼠标位置
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        //获取按键状态
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private const int VK_LBUTTON = 0x01;    // 左键
        private const int VK_F = 0x46;          // F键

        //定时器
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            Static.ReadConfig();
            InitializeComponent();
            //窗口点击穿透
            this.SourceInitialized += delegate
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle |
                WS_EX_TRANSPARENT);
            };

            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Rat rat = new();
            Canvas.Children.Add(rat);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            //置于最前
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Mask.Visibility = Visibility.Collapsed;
            Rat.DisplayMask += (s, e) =>
            {
                Mask.Visibility = Visibility.Visible;
            };
            Rat.HideMask += (s, e) =>
            {
                Mask.Visibility = Visibility.Collapsed;
            };
            Rat.AddRat += (s, e) =>
            {
                var rat = new Rat();
                Canvas.Children.Add(rat);
            };
            Rat.RemoveCheese += (s, e) =>
            {
                if (Canvas.Children.Count > 0)
                {
                    // 移除最后一个添加的Image
                    var lastImage = CheeseCanvas.Children[CheeseCanvas.Children.Count - 1] as System.Windows.Controls.Image;
                    if (lastImage != null)
                    {
                        CheeseCanvas.Children.Clear();
                        //MessageBox.Show("有可移除的奶酪图片。");
                    }
                    else
                    {
                        MessageBox.Show("没有可移除的奶酪图片。");
                    }
                }
            };

            //窗口全屏
            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                GetCursorPos(out System.Drawing.Point screenMaskPoint);
                var windowMaskPoint = PointFromScreen(new(screenMaskPoint.X, screenMaskPoint.Y));   // 转换为窗口坐标
                mainViewModel.point = new(windowMaskPoint.X, windowMaskPoint.Y);                    // 使用窗口坐标
            }
            if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0 && (GetAsyncKeyState(VK_F) & 0x8000) != 0)
            {
                GetCursorPos(out System.Drawing.Point screenMaskPoint);
                var windowMaskPoint = PointFromScreen(new(screenMaskPoint.X, screenMaskPoint.Y));    // 转换为窗口坐标
                System.Windows.Point point = new(windowMaskPoint.X, windowMaskPoint.Y);              // 使用窗口坐标
                System.Windows.Controls.Image newImage = new()
                {
                    // 设置图片源（替换为您的图片路径）
                    Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath("./Image/Cheese.png"), UriKind.Absolute)),
                    // 设置图片大小（可选）
                    Width = 100,
                    Height = 100
                };
                Rat.AimPoint = point; // 设置目标点
                // 设置图片位置
                Canvas.SetLeft(newImage, point.X - newImage.Width / 2);
                Canvas.SetTop(newImage, point.Y - newImage.Height / 2);

                // 将图片添加到Canvas
                CheeseCanvas.Children.Add(newImage);
            }
        }
    }
}