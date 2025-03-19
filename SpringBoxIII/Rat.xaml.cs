using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NAudio.Wave;
using System.IO;

namespace SpringBoxIII
{
    /// <summary>
    /// Rat.xaml 的交互逻辑
    /// </summary>
    /// 
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
            public int ratID;
        }

        private bool _isAnimationCompleted = true;
        private bool _isEventCompleted = true;
        private int _moveSpeed = 0;
        private int _randomEvent = 0;
        private static int _ratsCount = 0;
        private readonly int _ratID = 0;
        private static RatState _isMovedToCursor = new() { state = false, ratID = -1 };
        private static RatState _isMaskOn = new() { state = false, ratID = -1 };
        private static readonly bool[] _isAudioCompleted = [true, true];
        private bool _isCopied = false;

        private readonly WaveOutEvent[] _waveOut = new WaveOutEvent[2];
        private readonly AudioFileReader[] _audioFileReader = new AudioFileReader[2];

        public static event EventHandler? DisplayMask;
        public static event EventHandler? HideMask;
        public static event EventHandler? AddRat;

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

        public Rat()
        {
            _ratsCount++;
            _ratID = _ratsCount;
            InitializeComponent();
            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _audioFileReader[0] = new AudioFileReader(Static.AudioPath[0]);
            _audioFileReader[1] = new AudioFileReader(Static.AudioPath[1]);

            _waveOut[0] = new WaveOutEvent();
            _waveOut[0].Init(_audioFileReader[0]);
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
            _waveOut[1] = new WaveOutEvent();
            _waveOut[1].Init(_audioFileReader[1]);
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
            Img.Visibility = Visibility.Collapsed;

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

            _waveOut[0].Stop();
            _waveOut[0].Dispose();
            _audioFileReader[0].Dispose();
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

            Img.Visibility = Visibility.Visible;
            //Trace.WriteLine("isAnimationCompleted:" + _isAnimationCompleted);
            if (DataContext is RatViewModel viewModel)
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

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (this.IsLoaded)
            {
                if (_isEventCompleted && _isAnimationCompleted)
                {
                    // 产生随机事件
                    List<int> randomEvents = [1, 2, 3, 4, 5, 6];
                    List<int> weights = [10, 0, 0, 0, 0, 0];
                    WeightedRandom weightedRandom = new(randomEvents, weights);
                    _randomEvent = weightedRandom.GetRandomValue();
                    //Trace.WriteLine("randomEvent:" + _randomEvent);
                }
                if (_isMaskOn.state && _isMaskOn.ratID == _ratID)
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
                        _isMaskOn.ratID = -1;
                        _isMaskOn.state = false;
                    }
                }
                //随机移动
                if (_randomEvent == 1)
                {

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
                }
                //叼走鼠标
                else if (_randomEvent == 2)
                {
                    _moveSpeed = 500;
                    _isEventCompleted = false;
                    if (_isMovedToCursor.state && _isMovedToCursor.ratID == _ratID)
                    {
                        Point windowPoint = new((int)Canvas.GetLeft(Img), (int)Canvas.GetTop(Img));
                        var screenPoint = PointToScreen(windowPoint);                               // 转换为屏幕坐标
                        SetCursorPos((int)screenPoint.X + 50, (int)screenPoint.Y + 50);
                    }
                    else if (_isMovedToCursor.state && _isMovedToCursor.ratID != _ratID)
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
                            _isMovedToCursor.ratID = _ratID;
                            _isMovedToCursor.state = true;
                        }
                    }
                    else if (_isAnimationCompleted && _isMovedToCursor.state && _isMovedToCursor.ratID == _ratID)
                    {
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), async (s, e) =>
                        {
                            await Task.Delay(ran.Next(35, 400));
                            _isAnimationCompleted = true;
                            _isMovedToCursor.ratID = -1;
                            _isMovedToCursor.state = false;
                            _isEventCompleted = true;
                        });
                    }
                }
                //遮罩
                else if (_randomEvent == 3)
                {
                    if (!_isMaskOn.state)
                    {
                        SetImageSource(@Static.ImgPath[1]);
                        OnDisplayMask();
                        _isMaskOn.ratID = _ratID;
                        _isMaskOn.state = true;
                    }
                }
                //创建新耗子
                else if (_randomEvent == 4)
                {
                    if (_ratsCount < Static.MaxRatCount)
                    {
                        OnAddRat();
                    }
                }
                //待机
                else if (_randomEvent == 5)
                {
                    _isEventCompleted = false;
                    Random ran = new(Guid.NewGuid().GetHashCode());
                    _timer.Stop();
                    await Task.Delay(ran.Next(500, 3000));
                    _timer.Start();
                    _isEventCompleted = true;
                }
                else if (_randomEvent == 6)
                {
                    _isEventCompleted = false;
                    Img.Visibility = Visibility.Collapsed;
                    // 获取桌面路径
                    string desktopPath = /*Environment.GetFolderPath(Environment.SpecialFolder.Desktop)*/"E:/";

                    // 新文件夹的名称
                    string newFolderName = "Rat'sHome";

                    // 创建新文件夹的完整路径
                    string newFolderPath = System.IO.Path.Combine(desktopPath, newFolderName);

                    // 检查文件夹是否已经存在，如果不存在则创建
                    if (!Directory.Exists(newFolderPath))
                    {
                        Directory.CreateDirectory(newFolderPath);
                    }
                    string destinationFilePath = Path.Combine(newFolderPath, Path.GetFileNameWithoutExtension(Static.ImgPath[0]) + _ratID + ".png");
                    if (!Directory.Exists(destinationFilePath) && !_isCopied)
                    {
                        try
                        {
                            File.Copy(Static.ImgPath[0], destinationFilePath, overwrite: true);
                        }
                        catch (IOException)
                        {
                        }
                        _isCopied = true;
                    }
                    else if (!Directory.Exists(destinationFilePath) && _isCopied)
                    {
                        Img.Visibility = Visibility.Visible;
                    }
                    _timer.Stop();
                    await Task.Delay(5000);
                    _timer.Start();
                    //await Task.Delay(5000);
                    File.Delete(destinationFilePath);
                    _timer.Stop();
                    Random ran = new(Guid.NewGuid().GetHashCode());
                    await Task.Delay(ran.Next(30, 4000));
                    _timer.Start();
                    _isCopied = false;
                    _isEventCompleted = true;
                }
            }
        }
    }
}

