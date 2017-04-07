using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace ZombieNation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double STRIPE_HEIGHT = 25;
        const double STRIPE_FPS = 25;
        const double STRIPE_INTERVAL_MS = 1000 / STRIPE_FPS;

     //   const double VIDEO_FPS = 25d;
        //const double VIDEO_FRAME_INTERVAL_MS = 1000 / VIDEO_FPS;

        const double SPEED = 4;

        const double VIDEO_FRAME_INTERVAL_MS = 80;

        public const double BEAT_INTERVAL_SECS = 0.428571429;


        private Random _random = new Random();
        private List<Rectangle> _stripes;
        private DispatcherTimer _stripeChangeTimer;

        private bool MultipleStripes = true;

        public double ImageLoadPercent
        {
            get { return (double)GetValue(ImageLoadPercentProperty); }
            set { SetValue(ImageLoadPercentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageLoadPercent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageLoadPercentProperty =
            DependencyProperty.Register("ImageLoadPercent", typeof(double), typeof(MainWindow),
            new PropertyMetadata(0.0d, new PropertyChangedCallback(ImageLoadPercentChangedCallback)));

        private static void ImageLoadPercentChangedCallback(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MainWindow c = obj as MainWindow;
            if (c != null)
            {
                c.ImageLoadPercentChanged();
            }
        }

        public static readonly RoutedCommand GoCommand = new RoutedCommand("Go", typeof(MainWindow),
          new InputGestureCollection { new KeyGesture(Key.F1) });

        public static readonly RoutedCommand HighHatCommand = new RoutedCommand("HighHat", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F2) });

        public static readonly RoutedCommand ChangeLogoCyanCommand = new RoutedCommand("ChangeLogoCyan", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F3) });

        public static readonly RoutedCommand ChangeLogoRedCommand = new RoutedCommand("ChangeLogoRed", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F4) });

        public static readonly RoutedCommand ChangeLogoYellowCommand = new RoutedCommand("ChangeLogoYellow", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F5) });

        public static readonly RoutedCommand ChangeLogoGreenCommand = new RoutedCommand("ChangeLogoGreen", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F6) });

        public static readonly RoutedCommand ChangeLogoOrangeCommand = new RoutedCommand("ChangeLogoOrange", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F7) });

        public static readonly RoutedCommand ShootArrowsCommand = new RoutedCommand("ShootArrows", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F8) });

        public static readonly RoutedCommand AddInterferenceCommand = new RoutedCommand("AddInterference", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F9) });

        public static readonly RoutedCommand SaveCommand = new RoutedCommand("Save", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F10) });

        public void ImageLoadPercentChanged()
        {
            const int LOADING_PIXEL_HEIGHT = 4;
            double value = this.ImageLoadPercent;

            int width = (int)cvsPaper.ActualWidth;
            int height = (int)cvsPaper.ActualHeight;

            int pixels = (width * height) / LOADING_PIXEL_HEIGHT;
            int pixelsLoaded = (int)((double)pixels * value);
            int numOfRowsLoaded = pixelsLoaded / width;
            int incompleteRowPixels = pixelsLoaded % width;

            if (value == 1.0)
            {
                rtLoadedBlock.Height = 600;
                rtToBeLoadedLine.Width = 0;
            }
            else
            {
                rtLoadedBlock.Height = numOfRowsLoaded * LOADING_PIXEL_HEIGHT;
                rtToBeLoadedLine.Width = (width - incompleteRowPixels);
            }
            if (rtToBeLoadedLine.Width != 0)
                rtToBeLoadedLine.Height = LOADING_PIXEL_HEIGHT;
            else
                rtToBeLoadedLine.Height = 0;

            rtToBeLoadedBlock.Height = (height - numOfRowsLoaded) * LOADING_PIXEL_HEIGHT;
        }



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            // Set durations
            Storyboard sbLoadingImage = (Storyboard)FindResource("sbLoadingImage");
            foreach (var ani in sbLoadingImage.Children.OfType<DoubleAnimation>())
            {
                ani.Duration = TimeSpan.FromSeconds(16d * 4d * BEAT_INTERVAL_SECS * SPEED);
            }
            Storyboard sbExpandImage = (Storyboard)FindResource("sbExpandImage");
            foreach (var ani in sbExpandImage.Children.OfType<DoubleAnimation>())
            {
                ani.Duration = TimeSpan.FromSeconds(8d * 4d * BEAT_INTERVAL_SECS * SPEED);
            }
            Storyboard sbShootArrows = (Storyboard)FindResource("sbShootArrows");
            foreach (var ani in sbShootArrows.Children.OfType<DoubleAnimation>())
            {
                ani.Duration = TimeSpan.FromSeconds(BEAT_INTERVAL_SECS * SPEED);
            }

        }

        int _currentFrame = 0;
        Stopwatch _sw = new Stopwatch();
        public void SetupRecording()
        {
            // Setup frame capture
            DispatcherTimer frameTimer = new DispatcherTimer();
            frameTimer.Interval = TimeSpan.FromMilliseconds(VIDEO_FRAME_INTERVAL_MS);
            frameTimer.Tick += frameTimer_Tick;
            _sw.Start();
            frameTimer.Start();

            //Timer t = new Timer(new TimerCallback(SaveFrameDispatch),
            //    null, 0, (long)VIDEO_FRAME_INTERVAL_MS);
          

            // Setup frame recording
            //Thread t = new Thread(new ThreadStart(() => {
            //    while (true)
            //    {
            //        RenderTargetBitmap rtb = null;
            //        if (_frames.TryDequeue(out rtb))
            //        {
            //            string filename = string.Format("output_{0:0000}.bmp", _currentFrame);
            //            VidUtil.SaveRTBAsPNG(rtb, filename);
            //            _currentFrame++;
            //        }
            //        Thread.Sleep(50);
            //    }
            //}));
            //t.IsBackground = true;
            //t.Priority = ThreadPriority.Lowest;
            //t.Start();
        }

        List<MemoryStream> _frameStreams = new List<MemoryStream>();
        ConcurrentQueue<RenderTargetBitmap> _frames = new ConcurrentQueue<RenderTargetBitmap>();
        void frameTimer_Tick(object sender, EventArgs e)
        {
            SaveFrame();
        }

        //private void SaveFrameDispatch(object o)
        //{
        //    this.Dispatcher.BeginInvoke((Action)delegate()
        //    {
        //        SaveFrame();
        //    }, DispatcherPriority.Render, null);
        //}

        private void SaveFrame()
        {
            _sw.Stop();
            Debug.WriteLine("Frame {0} - {1}", _currentFrame, _sw.Elapsed);
            _sw.Restart();
          //  cvsCanvas.UpdateLayout();
            RenderTargetBitmap rtb = VidUtil.SaveCanvas(this, cvsCanvas, 96);

            string filename = string.Format("output_{0:0000}.jpg", _currentFrame);

            MemoryStream ms = new MemoryStream();
            VidUtil.SaveRTBAsPNG(rtb, ms);
            _frameStreams.Add(ms);

            _currentFrame++;
        }

        private void btnGo_Click_1(object sender, RoutedEventArgs e)
        {
            // ChangeStripesColours();
        }

        #region Stripes

        private void CreateStripes()
        {
            _stripes = new List<Rectangle>();
            for (double y = 0d; y < 600d; y += STRIPE_HEIGHT)
            {
                Color c = GetRandom8BitColour();
                SolidColorBrush brush = new SolidColorBrush(c);
                Rectangle r = new Rectangle();
                r.Width = 800d;
                r.Height = STRIPE_HEIGHT;
                r.Fill = brush;
                Canvas.SetTop(r, y);
                _stripes.Add(r);
                cvsCanvas.Children.Add(r);
            }
        }

        private void SetStripesChangeTimer()
        {
            _stripeChangeTimer = new DispatcherTimer();
            _stripeChangeTimer.Interval = TimeSpan.FromMilliseconds(STRIPE_INTERVAL_MS * SPEED);
            _stripeChangeTimer.Tick += _stripeChangeTimer_Tick;
            _stripeChangeTimer.Start();
        }

        void _stripeChangeTimer_Tick(object sender, EventArgs e)
        {
            ChangeStripesColours();
        }

        private void ChangeStripesColours()
        {
            Color c = GetRandom8BitColour();
            foreach (Rectangle stripe in _stripes)
            {
                if (MultipleStripes)
                    c = GetRandom8BitColour();
                SolidColorBrush brush = new SolidColorBrush(c);
                stripe.Fill = brush;
            }
        }

        #endregion

        private Color GetRandom8BitColour()
        {
            Color[] colors = new Color[]
            {
                Color.FromRgb(0x00, 0x00, 0x00),
                Color.FromRgb(0x00, 0x00, 0xFF),
                Color.FromRgb(0xFF, 0x00, 0x00),
                Color.FromRgb(0xFF, 0x00, 0xFF),
                Color.FromRgb(0x00, 0xFF, 0x00),
                Color.FromRgb(0x00, 0xFF, 0xFF),
                Color.FromRgb(0xFF, 0xFF, 0x00),
                Color.FromRgb(0xFF, 0xFF, 0xFF),
                Color.FromRgb(0x00, 0x00, 0xCD),
                Color.FromRgb(0xCD, 0x00, 0x00),
                Color.FromRgb(0xCD, 0x00, 0xCD),
                Color.FromRgb(0x00, 0xCD, 0x00),
                Color.FromRgb(0x00, 0xCD, 0xCD),
                Color.FromRgb(0xCD, 0xCD, 0x00),
                Color.FromRgb(0xCD, 0xCD, 0xCD)
            };
            return colors[_random.Next(colors.Length)];
        }

        private void ImageLoaded(object sender, EventArgs e)
        {
            // Change to Full ZN CD
            imgCD.Source = new BitmapImage(new Uri("Images/zn-full.png", UriKind.Relative));

            // Switch strip changing mode to single colour for all stripes
            this.MultipleStripes = false;

            // Raise interval for stripes changing
            _stripeChangeTimer.Interval = TimeSpan.FromTicks(_stripeChangeTimer.Interval.Ticks * 2);

            Storyboard sbExpandImage = (Storyboard)FindResource("sbExpandImage");
            sbExpandImage.Completed += sbExpandImage_Completed;
            sbExpandImage.Begin(this);

        }

        void sbExpandImage_Completed(object sender, EventArgs e)
        {
            StartColourCycleOne();
        }

        public void StartColourCycleOne()
        {
            // Change to grey lines CD
            imgCD.Source = new BitmapImage(new Uri("Images/zn-full-grey-lines-dark.png", UriKind.Relative));

            // Show separate logo
            imgLogo.Visibility = System.Windows.Visibility.Visible;

            // Start high hat lights!
            StartHighHatLights(() =>
            {
                // Then launch arrows
                ShootArrows();
            });

            // Cycle colours
            CycleLogoColours(7, () =>
            {
                // do nothing after
            });
        }

        #region High Hat Lights

        public void StartHighHatLights(Action doAfterDelegate = null)
        {
            double[] beat_intervals = new double[] {
                // BAR
                BEAT_INTERVAL_SECS,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS * 2
                // BAR 
                ,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS * 2
                // BAR 
                ,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2)
                // BAR 
                ,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS * 2
                // BAR 
                ,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS * 2
                // BAR 
                ,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS + (BEAT_INTERVAL_SECS / 2),
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS / 2,
                BEAT_INTERVAL_SECS / 2 
            };

            Queue<double> beat_interval_queue = new Queue<double>(beat_intervals);
            DispatcherTimer highHatTimer = new DispatcherTimer();
            TimeSpan ts = TimeSpan.FromSeconds(beat_interval_queue.Dequeue() * SPEED);
            highHatTimer.Interval = ts;
            highHatTimer.Tick += (o, e) =>
            {
                if (beat_interval_queue.Count != 0)
                {
                    ts = TimeSpan.FromSeconds(beat_interval_queue.Dequeue() * SPEED);
                    highHatTimer.Interval = ts;
                    SetOffHighHatLights();
                }
                else
                {
                    highHatTimer.Stop();
                    if (doAfterDelegate != null)
                    {
                        doAfterDelegate();
                    }
                }
            };
            highHatTimer.Start();
        }


        private void SetOffHighHatLights()
        {
            // Highlight logo lines
            imgCD.Source = new BitmapImage(new Uri("Images/zn-full-grey-lines.png", UriKind.Relative));

            DispatcherTimer highHatTimer2 = new DispatcherTimer();
            highHatTimer2.Interval = TimeSpan.FromMilliseconds(40d * SPEED);
            highHatTimer2.Tick += highHatTimer2_Tick;
            highHatTimer2.Start();
        }

        void highHatTimer2_Tick(object sender, EventArgs e)
        {
            DispatcherTimer highHatTimer2 = sender as DispatcherTimer;
            highHatTimer2.Stop();

            // Restore original lines
            imgCD.Source = new BitmapImage(new Uri("Images/zn-full-grey-lines-dark.png", UriKind.Relative));

            // Show green lines (first set)
            imgGlBottom.Visibility = imgGlTop.Visibility = System.Windows.Visibility.Visible;

            DispatcherTimer highHatTimer3 = new DispatcherTimer();
            highHatTimer3.Interval = TimeSpan.FromMilliseconds(40d * SPEED);
            highHatTimer3.Tick += highHatTimer3_Tick;
            highHatTimer3.Start();

        }

        void highHatTimer3_Tick(object sender, EventArgs e)
        {
            DispatcherTimer highHatTimer3 = sender as DispatcherTimer;
            highHatTimer3.Stop();

            // Hide green lines (first set)
            imgGlBottom.Visibility = imgGlTop.Visibility = System.Windows.Visibility.Hidden;

            // Show green lines (second set)
            imgGlFarBottom.Visibility = imgGlFarTop.Visibility = System.Windows.Visibility.Visible;

            DispatcherTimer highHatTimer4 = new DispatcherTimer();
            highHatTimer4.Interval = TimeSpan.FromMilliseconds(40d * SPEED);
            highHatTimer4.Tick += highHatTimer4_Tick;
            highHatTimer4.Start();

        }

        void highHatTimer4_Tick(object sender, EventArgs e)
        {
            DispatcherTimer highHatTimer4 = sender as DispatcherTimer;
            highHatTimer4.Stop();

            // Hide green lines (second set)
            imgGlFarBottom.Visibility = imgGlFarTop.Visibility = System.Windows.Visibility.Hidden;
        }

        private void HighHatCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            SetOffHighHatLights();
        }

        #endregion

        private void ChangeLogoCyanCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeLogoColour("cyan");
        }

        private void ChangeLogoRedCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeLogoColour("red");
        }

        private void ChangeLogoYellowCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeLogoColour("yellow");
        }

        private void ChangeLogoOrangeCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeLogoColour("orange");
        }

        private void ChangeLogoGreenCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ChangeLogoColour("green");
        }

        private void ShootArrowsCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            ShootArrows();
        }

        public void ChangeLogoColour(string colour)
        {
            imgLogo.Source = new BitmapImage(new Uri(string.Format("Images/zn-logo-{0}.png", colour), UriKind.Relative));
        }

        public void ShootArrows()
        {
            imgArrowTopLeft.Visibility = System.Windows.Visibility.Visible;
            imgArrowTopRight.Visibility = System.Windows.Visibility.Visible;
            imgArrowBottomLeft.Visibility = System.Windows.Visibility.Visible;
            imgArrowBottomRight.Visibility = System.Windows.Visibility.Visible;

            imgCD.Source = new BitmapImage(new Uri("Images/zn-plain.png", UriKind.Relative));

            Storyboard sbShootArrows = (Storyboard)FindResource("sbShootArrows");
            sbShootArrows.Completed += sbShootArrows_Completed;
            sbShootArrows.Begin();

            ToggleLogoColourYellowOrange();

        }

        void ToggleLogoColourYellowOrange()
        {
            ChangeLogoColour("yellow");
            DispatcherTimer glowTextTimer = new DispatcherTimer();
            glowTextTimer.Interval = TimeSpan.FromSeconds(BEAT_INTERVAL_SECS / 2.0 * SPEED);
            glowTextTimer.Tick += delegate(object obj, EventArgs e)
            {
                ChangeLogoColour("orange");
                glowTextTimer.Stop();
            };
            glowTextTimer.Start();
        }

        int arrowsCount = 0;
        const int MAX_ARROWS = 4 * 32;
        const int ARROWS_COLOUR_TOGGLE = 4 * 16;
        void sbShootArrows_Completed(object sender, EventArgs e)
        {
            arrowsCount++;
            if (arrowsCount < MAX_ARROWS)
            {
                // More arrows!
                Storyboard sbShootArrows = (Storyboard)FindResource("sbShootArrows");
                sbShootArrows.Begin();
            }
            else
            {
                // Arrows stopped; start random interference!
                StartInterference();
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds((double)_random.Next(200, 1700) * SPEED);
                timer.Tick += (o, ev) =>
                {
                    timer.Interval = TimeSpan.FromMilliseconds((double)_random.Next(200, 1700) * SPEED);
                    StartInterference();
                };
                timer.Start();

            }
            if (arrowsCount < ARROWS_COLOUR_TOGGLE)
            {
                ToggleLogoColourYellowOrange();
            }
            else if (arrowsCount == ARROWS_COLOUR_TOGGLE)
            {
                CycleLogoColours(32);
            }
        }

        private List<string> _logoColours = new List<string>() 
        {
            "violet", "yellow", "green", "red", "orange", "green", "red", "cyan",
            "violet", "blue", "green", "violet", "yellow", "blue", "green"
        };
        private Queue<string> _logoQueue;

        private void CycleLogoColours(int count, Action afterCycleDelegate = null)
        {
            // Queue colours
            _logoQueue = new Queue<string>();
            int index = 0;
            while (_logoQueue.Count <= count)
            {
                if (index > _logoColours.Count - 1)
                    index = 0;
                _logoQueue.Enqueue(_logoColours[index]);
                index++;
            }

            // swtich to first colour
            string curColour = _logoQueue.Dequeue();
            ChangeLogoColour(curColour);

            // Set up cycle timer
            DispatcherTimer cycleTimer = new DispatcherTimer();
            cycleTimer.Interval = TimeSpan.FromSeconds(BEAT_INTERVAL_SECS * 4d * SPEED);
            cycleTimer.Tick += delegate(object obj, EventArgs e)
            {
                if (_logoQueue.Count != 0)
                {
                    curColour = _logoQueue.Dequeue();
                    ChangeLogoColour(curColour);
                }
                else
                {
                    cycleTimer.Stop();
                    if (afterCycleDelegate != null)
                        afterCycleDelegate();
                }
            };
            cycleTimer.Start();
        }

        public void StartInterference()
        {
            StartInterference(_random.Next(1, 7));
        }

        private void StartInterference(int level)
        {
            int count = 0;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50d * SPEED);
            timer.Tick += (o, e) =>
            {
                if (count < level)
                {
                    AddInterference();
                    count++;
                }
                else
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }


        private void AddInterference()
        {
            List<Rectangle> _interferenceRectangles = new List<Rectangle>();

            // Create random rectangles
            const int MAX_RECTANGLES = 8;
            int numRects = _random.Next(1, MAX_RECTANGLES + 1);
            for (int i = 0; i < numRects; i++)
            {
                Rectangle r = new Rectangle();
                r.Width = 800;
                Canvas.SetZIndex(r, 50);
                Canvas.SetTop(r, _random.Next(600));
                r.Height = _random.Next(1, 60);
                r.Fill = new SolidColorBrush(GetRandom8BitColour());
                _interferenceRectangles.Add(r);
            }

            // Add rectangles to display
            foreach (var r in _interferenceRectangles)
                cvsPaper.Children.Add(r);

            // Remove them in a short period of time after
            DispatcherTimer rectRemoveTimer = new DispatcherTimer();
            rectRemoveTimer.Interval = TimeSpan.FromMilliseconds((double)_random.Next(25, 120) * SPEED);
            rectRemoveTimer.Tick += (o, e) =>
            {
                foreach (var r in _interferenceRectangles)
                    cvsPaper.Children.Remove(r);
                _interferenceRectangles.Clear();
            };
            rectRemoveTimer.Start();
        }

        private void AddInterferenceCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            StartInterference();
        }

        private void SaveCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            int i = 0;
            foreach(var ms in _frameStreams) 
            {
                i++;
                string filename = string.Format("output_{0:0000}.jpg", i);
                using (var stm = System.IO.File.Create(filename))
                {
                    ms.Position = 0;
                    ms.CopyTo(stm);
                }
            }
        }

        private void GoCommand_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            Storyboard sbLoadingImage = (Storyboard)FindResource("sbLoadingImage");
            sbLoadingImage.Completed += ImageLoaded;
            sbLoadingImage.Begin();

            //     SetupRecording();
            CreateStripes();
            SetStripesChangeTimer();
        }


    }
}
