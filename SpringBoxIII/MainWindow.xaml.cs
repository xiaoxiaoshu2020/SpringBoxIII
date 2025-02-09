using System.Diagnostics;
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
        private bool _isAnimationCompleted = true;
        private int _moveSpeed = 300;
        private Point _point = new Point(0, 0);



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
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }
        private double CalculateAngle(Point center, Point target)
        {
            // 计算两点之间的差值
            double deltaX = target.X - center.X;
            double deltaY = target.Y - center.Y;
            // 使用 Math.Atan2 计算角度（弧度）
            double angleRadians = Math.Atan2(deltaY, deltaX);
            // 将弧度转换为角度
            double angleDegrees = angleRadians * (180 / Math.PI);
            return angleDegrees;
        }
        private TimeSpan CalculatedDuration(int speed, int displacement)
        {
            int totalSeconds = displacement / speed;
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                seconds = 1;
            }
            return new(hours, minutes, seconds);
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Storyboard storyboard = (Storyboard)this.FindResource("MoveAnimation");
            storyboard.Completed += (s, e) => { _isAnimationCompleted = true; };
            Point imageCenter = new Point(
                Img.ActualWidth / 2 + Canvas.GetLeft(Img),
                Img.ActualHeight / 2 + Canvas.GetTop(Img)
            );
            if (DataContext is MainViewModel viewModel)
            {
                Trace.WriteLine("Top:" + Canvas.GetTop(Img));
                if (_isAnimationCompleted == true)
                {
                    Random ran = new Random();
                    viewModel.From = viewModel.To;
                    viewModel.To = new(ran.Next(0, (int)this.ActualWidth), ran.Next(0, (int)this.ActualHeight));
                    viewModel.DurationX = CalculatedDuration(_moveSpeed, (int)Math.Abs(viewModel.To.X - viewModel.From.X));
                    viewModel.DurationY = CalculatedDuration(_moveSpeed, (int)Math.Abs(viewModel.To.Y - viewModel.From.Y));
                    Trace.WriteLine("DurationX:" + viewModel.DurationX);
                    viewModel.Angle = CalculateAngle(imageCenter, viewModel.To) - 90;
                    storyboard.Begin();
                    _isAnimationCompleted = false;
                }
            }
        }
    }
}