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
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        //改变鼠标位置
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        private bool _isAnimationCompleted = true;
        private bool _isMovedToCursor = false;
        private bool _isEventCompleted = true;
        private int _moveSpeed = 350;
        private int randomEvent = 0;
        //private Point _point = new Point(0, 0);



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
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Img.Visibility = Visibility.Collapsed;
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
        private static double CalculateAngle(Point center, Point target)
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
        private static bool IsNearTarget(Point currentPosition, Point targetPosition)
        {
            const double Tolerance = 100.0; // 容差值
            double deltaX = Math.Abs(currentPosition.X - targetPosition.X);
            double deltaY = Math.Abs(currentPosition.Y - targetPosition.Y);
            Trace.WriteLine("deltaX:" + deltaX);
            Trace.WriteLine("deltaY:" + deltaY);
            return deltaX < Tolerance && deltaY < Tolerance;
        }
        private static TimeSpan CalculatedDuration(double speed, double displacement)
        {
            double totalSeconds = displacement / speed;
            if (totalSeconds < 0.1)
            {
                totalSeconds = 0.1;
            }
            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            Trace.WriteLine("time:" + time);
            return time;
        }
        /// <summary>
        /// 播放移动动画
        /// </summary>
        /// <param name="animationName">要播放的Storyboard的名称</param>
        /// <param name="To">从当前坐标要到达的目标点</param>
        /// <param name="handler">当动画结束时调用的函数</param>
        private void PlayMoveAnimation(string animationName, Point To, Action<object?, EventArgs?> handler)
        {
            ArgumentNullException.ThrowIfNull(animationName);

            Img.Visibility = Visibility.Visible;
            //Trace.WriteLine("isAnimationCompleted:" + _isAnimationCompleted);
            if (DataContext is MainViewModel viewModel)
            {
                Point imageCenter = new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img));
                viewModel.From = viewModel.To;
                viewModel.To = To;
                if (viewModel.To != viewModel.From)
                {
                    int displacementX = (int)Math.Abs(viewModel.To.X - viewModel.From.X);
                    int displacementY = (int)Math.Abs(viewModel.To.Y - viewModel.From.Y);
                    if (displacementX < displacementY)
                    {
                        viewModel.Duration = CalculatedDuration(_moveSpeed, (int)Math.Abs(viewModel.To.Y - viewModel.From.Y));
                    }
                    else
                    {
                        viewModel.Duration = CalculatedDuration(_moveSpeed, (int)Math.Abs(viewModel.To.X - viewModel.From.X));
                    }
                    viewModel.Angle = CalculateAngle(imageCenter, viewModel.To) - 90;
                    _isAnimationCompleted = false;
                    Storyboard storyboard = (Storyboard)this.FindResource(animationName);
                    EventHandler wrappedHandler = null!;
                    wrappedHandler = (s, e) =>
                    {
                        // 执行传入的 handler
                        handler(s, e);

                        // 完成后取消订阅
                        storyboard.Completed -= wrappedHandler;
                    };
                    storyboard.Completed += wrappedHandler;
                    storyboard.Begin();
                }
            }
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (_isEventCompleted && _isAnimationCompleted)
                {
                    // 产生随机事件
                    List<int> randomEvents = [1, 2];
                    List<int> weights = [5, 5];
                    WeightedRandom weightedRandom = new(randomEvents, weights);
                    randomEvent = weightedRandom.GetRandomValue();
                    Trace.WriteLine("randomEvent:" + randomEvent);
                }
                if (randomEvent == 1)
                {
                    
                    _moveSpeed = 250;
                    if (_isAnimationCompleted)
                    {
                        _isEventCompleted = false;
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        Img.Visibility = Visibility.Visible;
                        PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), (s, e) =>
                        {
                            Task.Delay(ran.Next(35, 400)).Wait();
                            _isAnimationCompleted = true;
                            _isEventCompleted = true;
                        });
                    }
                }
                else if (randomEvent == 2)
                {
                    _moveSpeed = 500;
                    _isEventCompleted = false;
                    //Trace.WriteLine("isMovedToCursor:" + _isMovedToCursor);
                    if (_isMovedToCursor)
                    {
                        Point windowPoint = new((int)Canvas.GetLeft(Img), (int)Canvas.GetTop(Img));
                        var screenPoint = PointToScreen(windowPoint); // 转换为屏幕坐标
                        SetCursorPos((int)screenPoint.X + 50, (int)screenPoint.Y + 50);
                    }
                    if (_isAnimationCompleted && !_isMovedToCursor)
                    {
                        GetCursorPos(out System.Drawing.Point screenPoint);
                        var windowPoint = PointFromScreen(new Point(screenPoint.X, screenPoint.Y)); // 转换为窗口坐标
                        PlayMoveAnimation("MoveAnimation", windowPoint, (s, e) =>
                        {
                            _isAnimationCompleted = true;
                        });
                        //ToDo:修复坐标检测算法 

                        if (IsNearTarget(new(Canvas.GetLeft(Img), Canvas.GetTop(Img)),windowPoint))
                        {
                            _isMovedToCursor = true;
                        }
                    }
                    else if (_isAnimationCompleted && _isMovedToCursor)
                    {
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), (s, e) =>
                        {
                            _isAnimationCompleted = true;
                            _isMovedToCursor = false;
                            _isEventCompleted = true;
                        });
                        //Task.Delay(ran.Next(35, 400)).Wait();
                    }
                }
            }
        }
    }
}