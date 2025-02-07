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
        private bool isAnimationCompleted = true;
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
            Random ran = new Random();

            Storyboard storyboard = (Storyboard)this.FindResource("MoveAnimation");
            storyboard.Completed += (s, e) => { isAnimationCompleted = true; };
            if (isAnimationCompleted == true)
            {
                this.Resources["FromValueX"] = this.Resources["ToValueX"];
                this.Resources["FromValueY"] = this.Resources["ToValueY"];
                this.Resources["ToValueX"] = (double)ran.Next(0, (int)this.ActualWidth);
                this.Resources["ToValueY"] = (double)ran.Next(0, (int)this.ActualHeight);
                storyboard.Begin();
                isAnimationCompleted = false;
                //MessageBox.Show("Info");
                Trace.WriteLine(this.Resources["ToValueY"]);
            }
        }
    }
}