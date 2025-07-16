using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpringBoxIII
{
    /// <summary>
    /// Rat.xaml 的交互逻辑
    /// </summary>
    public partial class Rat : UserControl
    {
        //获取鼠标位置
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);

        //改变鼠标位置
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        //获取按键状态
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private const int VK_LBUTTON = 0x01;  // 左键

        //定时器
        private DispatcherTimer _timer = new();

        private struct RatState
        {
            public bool state;
            public int ratId;
        }

        private bool _isAnimationCompleted = true;
        private bool _isEventCompleted = true;
        private int _moveSpeed = 0;
        private int _randomEvent = 0;
        private static int _totalRatsCount = 0;
        private readonly int _ratId = 0;
        private int _satiety = 0; // 饱腹度
        private static RatState _isMovedToCursor = new() { state = false, ratId = -1 };
        private static RatState _isMaskActive = new() { state = false, ratId = -1 };
        private static readonly bool[] _isAudioCompleted = [true, true];
        private Point targetPoint = new();

        private readonly WaveOutEvent[] _waveOut = new WaveOutEvent[2];
        private readonly AudioFileReader[] _audioFileReader = new AudioFileReader[2];

        public static event EventHandler? DisplayMask;
        public static event EventHandler? HideMask;
        public static event EventHandler? AddRat;
        public static event EventHandler? RemoveCheese;
        public static List<Point> TargetPoints = [];

        private void OnDisplayMask()
        {
            if (DisplayMask != null)
            {
                DisplayMask(this, EventArgs.Empty);
            }
        }

        private void OnHideMask()
        {
            if (HideMask != null)
            {
                HideMask(this, EventArgs.Empty);
            }
        }

        private void OnAddRat()
        {
            if (AddRat != null)
            {
                AddRat(this, EventArgs.Empty);
            }
        }

        private void OnRemoveCheese()
        {
            if (RemoveCheese != null)
            {
                RemoveCheese(this, EventArgs.Empty);
            }
        }

        public Rat()
        {
            _totalRatsCount++;
            _ratId = _totalRatsCount;
            InitializeComponent();
            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            for (int i = 0; i < 2; i++)
            {
                _audioFileReader[i] = new AudioFileReader(Static.AudioPath[i]);
                _waveOut[i] = new WaveOutEvent();
                _waveOut[i].Init(_audioFileReader[i]);
            }
            _waveOut[0].PlaybackStopped += (s, e) =>
            {
                _isAudioCompleted[0] = true;
                if (_isAudioCompleted[0])
                {
                    _isAudioCompleted[0] = false;
                    _audioFileReader[0].Position = 0;
                    _waveOut[0].Play();
                }
            };
            _waveOut[1].PlaybackStopped += (s, e) =>
            {
                _audioFileReader[1].Position = 0;
                _isAudioCompleted[1] = true;
            };
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            SetImageSource(Static.ImgPath[0]);

            if (_isAudioCompleted[0])
            {
                _isAudioCompleted[0] = false;
                _audioFileReader[0].Position = 0;
                _waveOut[0].Play();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            }
            for (int i = 0; i < 2; i++)
            {
                _waveOut[i].Stop();
                _waveOut[i].Dispose();
                _audioFileReader[i].Dispose();
            }
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
            const double Tolerance = 50.0; // 容差值
            double deltaX = Math.Abs(currentPosition.X - targetPosition.X);
            double deltaY = Math.Abs(currentPosition.Y - targetPosition.Y);
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
            //Trace.WriteLine("time:" + time);
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

            //Trace.WriteLine("isAnimationCompleted:" + _isAnimationCompleted);
            if (DataContext is RatViewModel viewModel)
            {
                Point imageCenter = new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img));
                viewModel.From = new Point(Canvas.GetLeft(Img), Canvas.GetTop(Img));
                Trace.WriteLine(viewModel.From.ToString());
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

        private void SetImageSource(string filePath)
        {
            // 创建 BitmapImage
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(System.IO.Path.GetFullPath(filePath), UriKind.Absolute);
            bitmap.EndInit();
            // 设置 Image 控件的 Source
            Img.Source = bitmap;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (IsLoaded)
            {
                if (TargetPoints.Count > 0 && _isAnimationCompleted && _satiety <= 5)
                {
                    _moveSpeed = 500;
                    targetPoint = TargetPoints[0];
                    Trace.WriteLine(targetPoint);
                    PlayMoveAnimation("MoveAnimation", targetPoint, async (s, e) =>
                    {
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        if (TargetPoints.Contains(targetPoint))
                        {
                            _ = TargetPoints.Remove(targetPoint); // 清除目标点
                            OnRemoveCheese();
                            _satiety++;
                            await Task.Delay(ran.Next(40, 400));
                        }
                        _isAnimationCompleted = true;
                        _isEventCompleted = true;
                    });
                }
                else if (_isEventCompleted && _isAnimationCompleted)
                {
                    // 产生随机事件
                    List<int> randomEvents = [1, 2, 3, 4];
                    List<int> weights = [2, 0, 0, 4];
                    WeightedRandom weightedRandom = new(randomEvents, weights);
                    _randomEvent = weightedRandom.GetRandomValue();
                    //Trace.WriteLine("randomEvent:" + _randomEvent);
                }
                if (_isMaskActive.state && _isMaskActive.ratId == _ratId)
                {
                    Point imageCenter = new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img));
                    //Mask.Visibility = Visibility.Visible;
                    GetCursorPos(out System.Drawing.Point screenMaskPoint);
                    var windowMaskPoint = PointFromScreen(new(screenMaskPoint.X, screenMaskPoint.Y));    // 转换为窗口坐标
                    Point point = new(windowMaskPoint.X, windowMaskPoint.Y);                       // 使用窗口坐标
                    if (IsNearTarget(new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img)), windowMaskPoint)
                        && (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0)
                    {
                        SetImageSource(@Static.ImgPath[0]);
                        OnHideMask();
                        _isMaskActive.ratId = -1;
                        _isMaskActive.state = false;
                    }
                }
                switch (_randomEvent)
                {
                    case 1:
                        _moveSpeed = 250;
                        if (_isAnimationCompleted)
                        {
                            _isEventCompleted = false;
                            Random ran = new(Guid.NewGuid().GetHashCode());
                            Img.Visibility = Visibility.Visible;
                            PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), async (s, e) =>
                            {
                                await Task.Delay(ran.Next(40, 400));
                                _isAnimationCompleted = true;
                                _isEventCompleted = true;
                            });
                        }
                        break;
                    case 2:
                        _moveSpeed = 500;
                        _isEventCompleted = false;
                        if (_isMovedToCursor.state && _isMovedToCursor.ratId == _ratId)
                        {
                            Point windowPoint = new((int)Canvas.GetLeft(Img), (int)Canvas.GetTop(Img));
                            var screenPoint = PointToScreen(windowPoint);                               // 转换为屏幕坐标
                            SetCursorPos((int)screenPoint.X + 50, (int)screenPoint.Y + 50);
                        }
                        else if (_isMovedToCursor.state && _isMovedToCursor.ratId != _ratId)
                        {
                            _isEventCompleted = true;
                        }
                        if (_isAnimationCompleted && !_isMovedToCursor.state)
                        {
                            GetCursorPos(out System.Drawing.Point screenPoint);
                            var windowPoint = PointFromScreen(new Point(screenPoint.X, screenPoint.Y));  // 转换为窗口坐标
                            PlayMoveAnimation("MoveAnimation", windowPoint, (s, e) =>
                            {
                                _isAnimationCompleted = true;
                            });
                            if (_isAudioCompleted[1])
                            {
                                _isAudioCompleted[1] = false;
                                _audioFileReader[1].Position = 0;
                                _waveOut[1].Play();
                            }
                            if (IsNearTarget(new(Canvas.GetLeft(Img), Canvas.GetTop(Img)), windowPoint))
                            {
                                _isMovedToCursor.ratId = _ratId;
                                _isMovedToCursor.state = true;
                            }
                        }
                        else if (_isAnimationCompleted && _isMovedToCursor.state && _isMovedToCursor.ratId == _ratId)
                        {
                            Random ran = new(Guid.NewGuid().GetHashCode());
                            PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), async (s, e) =>
                            {
                                await Task.Delay(ran.Next(35, 400));
                                _satiety--;
                                _isAnimationCompleted = true;
                                _isMovedToCursor.ratId = -1;
                                _isMovedToCursor.state = false;
                                _isEventCompleted = true;
                            });
                        }
                        break;
                    case 3:
                        if (!_isMaskActive.state)
                        {
                            SetImageSource(@Static.ImgPath[1]);
                            OnDisplayMask();
                            _isMaskActive.ratId = _ratId;
                            _isMaskActive.state = true;
                        }
                        break;
                    case 4:
                        if (_totalRatsCount < Static.MaxRatCount)
                        {
                            OnAddRat();
                        }
                        break;
                }
            }
        }
    }
}

