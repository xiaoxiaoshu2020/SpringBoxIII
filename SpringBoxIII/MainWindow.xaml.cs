using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        //定时器
        private DispatcherTimer _timer;

        //获取鼠标位置
        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        //测试用变量
        private int X = 0;
        private int Y = 0;
        private bool isThereAPoint = false;
        private Point point = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();
            //窗口点击穿透
            this.SourceInitialized += delegate
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle |
                WS_EX_TRANSPARENT);
            };

            // 初始化定时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += Timer_Tick;
            _timer.Start();

        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            //置于最前
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //窗口全屏
            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {


            // 移动窗口
            // System.Drawing.Point mp = new System.Drawing.Point();
            //GetCursorPos(out mp);
            //Canvas.SetLeft(Img, mp.X);
            //Canvas.SetTop(Img, mp.Y);

            if (isThereAPoint == false)
            {
                Random ran = new Random();
                isThereAPoint = true;
                point.X = ran.Next(77, 1000);
                point.Y = ran.Next(77, 1000);
            }

            else if (X == point.X && Y == point.Y)
            {
                isThereAPoint = false;
            }

            else if (Y != point.Y)
            {
                if (Y < point.Y)
                {
                    Y += 1;
                }
                else
                {
                    Y -= 1;
                }
            }
            else if (X != point.X)
            {
                if (X < point.X)
                {
                    X += 1;
                }
                else
                {
                    X -= 1;
                }
            }

            Canvas.SetTop(Img, Y);
            Canvas.SetLeft(Img, X);

        }
    }
}