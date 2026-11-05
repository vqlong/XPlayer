using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing.IndexedProperties;
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

namespace XLib.UserControls
{
    /// <summary>
    /// Interaction logic for Equalizer.xaml
    /// </summary>
    public partial class Equalizer : UserControl
    {
        public Equalizer()
        {
            InitializeComponent();

            _storyboard = (Storyboard)TryFindResource("Storyboard.Equalizer4");
        }

        Storyboard _storyboard;
        public static readonly DependencyProperty StatusProperty =
                DependencyProperty.Register("Status", typeof(XPlayerStatus), typeof(Equalizer), new PropertyMetadata(XPlayerStatus.Stop, StatusChangedCallback));
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(int), typeof(Equalizer), new PropertyMetadata(4, ModeChangedCallback));
        public static readonly DependencyProperty ShowModeProperty =
            DependencyProperty.Register("ShowMode", typeof(bool), typeof(Equalizer), new PropertyMetadata(false));
        public static readonly DependencyProperty EqualizerWidthProperty =
            DependencyProperty.Register("EqualizerWidth", typeof(double), typeof(Equalizer), new PropertyMetadata(200.0));
        public static readonly DependencyProperty EqualizerHeightProperty =
            DependencyProperty.Register("EqualizerHeight", typeof(double), typeof(Equalizer), new PropertyMetadata(100.0));
        public XPlayerStatus Status { get => (XPlayerStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }
        public int Mode { get => (int)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
        public bool ShowMode { get => (bool)GetValue(ShowModeProperty); set => SetValue(ShowModeProperty, value); }
        public double EqualizerWidth { get => (double)GetValue(EqualizerWidthProperty); set => SetValue(EqualizerWidthProperty, value); }
        public double EqualizerHeight { get => (double)GetValue(EqualizerHeightProperty); set => SetValue(EqualizerHeightProperty, value); }

        private static void StatusChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var equalizer = (Equalizer)d;
            var status = (XPlayerStatus)e.NewValue;
            if (status == XPlayerStatus.Play) equalizer._storyboard.Begin();
            if (status == XPlayerStatus.Pause) equalizer._storyboard.Pause();
            if (status == XPlayerStatus.Resume) equalizer._storyboard.Resume();
            if (status == XPlayerStatus.Stop) equalizer._storyboard.Stop();
            if (status == XPlayerStatus.End) equalizer._storyboard.Stop();
            // DialogBox.ShowInfo($"Name: {equalizer.Name}\nStatus: {equalizer.Status}\nNewValue: {status}");
        }       
        private static void ModeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var equalizer = (Equalizer)d;

            equalizer._storyboard.Stop();

            var mode = (int)e.NewValue;
            if(mode == 0 || mode == 1 || mode == 2)
            {
                equalizer.grid012.Visibility = Visibility.Visible;
                equalizer.grid3.Visibility = Visibility.Hidden;
                equalizer.ellipse4.Visibility = Visibility.Hidden;
                if (mode == 0) equalizer._storyboard = (Storyboard)equalizer.TryFindResource("Storyboard.Equalizer0");
                if (mode == 1) equalizer._storyboard = (Storyboard)equalizer.TryFindResource("Storyboard.Equalizer1");
                if (mode == 2) equalizer._storyboard = (Storyboard)equalizer.TryFindResource("Storyboard.Equalizer2");
            }
            else if (mode == 3)
            {
                equalizer.grid012.Visibility = Visibility.Hidden;
                equalizer.grid3.Visibility = Visibility.Visible;
                equalizer.ellipse4.Visibility = Visibility.Hidden;
                equalizer._storyboard = (Storyboard)equalizer.TryFindResource("Storyboard.Equalizer3");
            }
            else if (mode == 4)
            {
                equalizer.grid012.Visibility = Visibility.Hidden;
                equalizer.grid3.Visibility = Visibility.Hidden;
                equalizer.ellipse4.Visibility = Visibility.Visible;
                equalizer._storyboard = (Storyboard)equalizer.TryFindResource("Storyboard.Equalizer4");
            }
            
            if (equalizer.Status == XPlayerStatus.Play || equalizer.Status == XPlayerStatus.Resume) equalizer._storyboard.Begin();
        }       
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(Status == XPlayerStatus.Play || Status == XPlayerStatus.Resume || Status == XPlayerStatus.Pause)
            {
                var currentStatus = Status;
                SetCurrentValue(StatusProperty, XPlayerStatus.Stop);
                SetCurrentValue(StatusProperty, XPlayerStatus.Play);
                SetCurrentValue(StatusProperty, currentStatus);
            }
        }

        public override string ToString()
        {
            return $"{Name} {Status} {Mode}";
        }
    }
}
