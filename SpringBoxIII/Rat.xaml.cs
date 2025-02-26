using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace SpringBoxIII
{
    /// <summary>
    /// Mouse.xaml 的交互逻辑
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
        private DispatcherTimer? _timer;

        private struct ratState
        {
            public bool state;
            public int ratID;
        }

        private bool _isAnimationCompleted = true;
        private bool _isEventCompleted = true;
        private int _moveSpeed = 350;
        private int _randomEvent = 0;
        public static int _ratsCount = 0;
        private int _ratID = 0;
        private static ratState _isMovedToCursor = new() { state = false, ratID = -1 };
        private static ratState _isMaskOn = new() { state = false, ratID = -1 };
        private static bool[] _isAudioCompleted = [true, true];

        private WaveOutEvent[] _waveOut = new WaveOutEvent[2];
        private AudioFileReader[] _audioFileReader = new AudioFileReader[2];

        public Dictionary<int, Rat> ratsDictionary = new();

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
            ratsDictionary.Add(_ratID, this);
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
                _timer = null;
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
                    viewModel.Angle = CalculateAngle(imageCenter, viewModel.To) - 90;//!!!这里本来是-90，图片长宽原来是110
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
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(System.IO.Path.GetFullPath(filePath), UriKind.Absolute);
            bitmap.EndInit();
            // 设置 Image 控件的 Source
            Img.Source = bitmap;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (this.IsLoaded)
            {
                if (_isEventCompleted && _isAnimationCompleted)
                {
                    // 产生随机事件
                    List<int> randomEvents = [1, 2, 3, 4, 5];
                    List<int> weights = [60, 15, 10, 5, 10];
                    WeightedRandom weightedRandom = new(randomEvents, weights);
                    _randomEvent = weightedRandom.GetRandomValue();
                    Trace.WriteLine("randomEvent:" + _randomEvent);
                }
                if (_isMaskOn.state && _isMaskOn.ratID == _ratID)
                {
                    Point imageCenter = new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img));
                    //Mask.Visibility = Visibility.Visible;
                    GetCursorPos(out System.Drawing.Point screenMaskPoint);
                    var windowMaskPoint = PointFromScreen(new(screenMaskPoint.X, screenMaskPoint.Y));    // 转换为窗口坐标
                    Point point = new Point(windowMaskPoint.X, windowMaskPoint.Y);                       // 使用窗口坐标
                    if (IsNearTarget(new(Img.ActualWidth / 2 + Canvas.GetLeft(Img), Img.ActualHeight / 2 + Canvas.GetTop(Img)), windowMaskPoint)
                        && (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0)
                    {
                        SetImageSource(@Static.ImgPath[0]);
                        OnHideMask();
                        _isMaskOn.ratID = -1;
                        _isMaskOn.state = false;
                    }
                }
                if (_randomEvent == 1)
                {

                    _moveSpeed = 250;
                    if (_isAnimationCompleted)
                    {
                        _isEventCompleted = false;
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        Img.Visibility = Visibility.Visible;
                        PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), (s, e) =>
                        {
                            Task.Run(() =>
                            {
                                Task.Delay(ran.Next(35, 400)).Wait();
                                _isAnimationCompleted = true;
                                _isEventCompleted = true;
                            });
                        });
                    }
                }
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
                        _waveOut[1].Stop();
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
                        Task.Run(() =>
                        {
                            Random ran = new(Guid.NewGuid().GetHashCode());
                            Task.Delay(ran.Next(5000, 8000)).Wait();
                            if (!_isMovedToCursor.state)
                            {
                                _randomEvent = -1;
                                _isEventCompleted = true;
                            }
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
                        PlayMoveAnimation("MoveAnimation", new(ran.Next(0, (int)this.ActualWidth) + 10, ran.Next(0, (int)this.ActualHeight) + 10), (s, e) =>
                        {
                            Task.Run(() =>
                            {
                                Task.Delay(ran.Next(35, 400)).Wait();
                                _isAnimationCompleted = true;
                                _isMovedToCursor.ratID = -1;
                                _isMovedToCursor.state = false;
                                _isEventCompleted = true;
                            });
                        });
                    }
                }
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
                else if (_randomEvent == 4)
                {
                    if (_ratsCount < Static.MaxRatCount)
                    {
                        OnAddRat();
                    }
                }
                else if(_randomEvent == 5)
                {
                    _isEventCompleted = false;
                    Task.Run(() =>
                    {
                        Random ran = new(Guid.NewGuid().GetHashCode());
                        Task.Delay(ran.Next(500, 3000)).Wait();
                        _isEventCompleted = true;
                    });
                }
            }
        }
    }
}
