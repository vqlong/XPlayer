using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace XLib.UserControls
{
    /// <summary>
    /// Interaction logic for XPlayer.xaml
    /// </summary>
    public partial class XPlayer : Window, INotifyPropertyChanged
    {
        public XPlayer()
        {
            InitializeComponent();

            var uri = new Uri("pack://application:,,,/PresentationFramework.Royale;component/themes/Royale.NormalColor.xaml");
            var dictionary = new ResourceDictionary { Source = uri };
            foreach (var key in dictionary.Keys)
            {
                //Lấy ra style cho scrollbar và thêm vào resource của app
                if (dictionary[key] is Style style && style.TargetType == typeof(ScrollBar))
                {
                    //style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
                    Resources.Add(key, style);
                    break;
                }
            }

            //unzip file .gif từ resource
            //using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/XLib;component/Resources/Images/Equalizer.zip", UriKind.Absolute)).Stream;
            //var zipArchive = new ZipArchive(stream);
            //var gifPath = @$"{currentPath}\ResourceEqualizer";
            //zipArchive.ExtractToDirectory(gifPath, true);
            //GifFiles.AddRange(Directory.GetFiles(gifPath));

            //bổ sung thêm file .gif từ thư mục ngoài
            Directory.CreateDirectory(EqualizerPath);
            GifFiles.AddRange(Directory.GetFiles(EqualizerPath, "*.gif"));            

            LoadColorSetting();

            FileNames.CollectionChanged += (s, e) =>
            {
                if (lsvFileNames.HasItems)
                {
                    HasItems = true;
                    if (lsvFileNames.SelectedIndex <= 0) lsvFileNames.SelectedIndex = 0;
                }
                else HasItems = false;
            };

            OpenPlaylist(PlaylistPath + LastPlaylistName);
        }
        //Storyboard điều khiển animation cho equalizer của window table manager
        private ClockController? titleClockController;
        private ClockController? title1ClockController;
        private XPlayerStatus _status;
        private ClockState _clockState;
        private string _textPosition = "00:00:00";
        private string _playlistName = "New Playlist";
        private bool _isRepeat = false;
        private bool _isShuffle = false;
        private bool _hasItems = false;
        public XPlayerStatus Status { get => _status; set => OnPropertyChanged(ref _status, value); }
        public ClockState ClockState { get => _clockState; set => OnPropertyChanged(ref _clockState, value); }
        public string TextPosition { get => _textPosition; set => OnPropertyChanged(ref _textPosition, value); }
        public string PlaylistName { get => _playlistName; set => OnPropertyChanged(ref _playlistName, value); }
        public bool IsRepeat { get => _isRepeat; set => OnPropertyChanged(ref _isRepeat, value); }
        public bool IsShuffle { get => _isShuffle; set => OnPropertyChanged(ref _isShuffle, value); }
        public bool HasItems { get => _hasItems; set => OnPropertyChanged(ref _hasItems, value); }
        public Storyboard? StoryboardEqualizer { get; set; }
        public XSlider SeekBar => sliderSeekBar;
        public bool ShouldClose { get; set; }
        public string AppName { get; } = "Media Player 2023";
        public string LastPlaylistName { get; } = "LastPlaylist.xml";
        public string SettingFileName { get; } = "settings.xml";
        public string PlaylistPath { get; } = AppContext.BaseDirectory + @"Player\Playlists\";
        public string SettingPath { get; } = AppContext.BaseDirectory + @"Player\Setting\";
        public string EqualizerPath { get; } = AppContext.BaseDirectory + @"Player\Equalizer\";
        public string PlayingItem { get; private set; }
        public string[] AudioFormats { get; } = new[] { ".mp3", ".m4a", ".wav", ".wma", ".flac", ".webm" };
        public string[] VideoFormats { get; } = new[] { ".mp4", ".flv", ".avi", ".wmv", ".webm" };
        public ObservableCollection<string> FileNames { get; } = new ObservableCollection<string>();
        public List<string> GifFiles { get; } = new List<string>();
        public ListCollectionView BackgroundView { get; } = new ListCollectionView(BrushWrapper.Wrappers);
        public ListCollectionView BorderColorView { get; } = new ListCollectionView(BrushWrapper.Wrappers);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadColorSetting()
        {
            var settings = OpenFile<XPlayerSetting[]>(SettingPath + SettingFileName);
            if (settings != null && settings.Length > 0)
            {
                foreach(var setting in settings)
                {
                    var property = GetType().GetProperty(setting.Name)?.GetValue(this);
                    if (property is ListCollectionView view) SetColorViewCurrent(view, setting.Value);
                }
            }
        }
        private void SetColorViewCurrent(ListCollectionView view, string colorName)
        {
            for(int i = 0; i < view.Count; i++)
            {
                var item = view.GetItemAt(i);
                if (item != null && item.ToString() == colorName)
                {
                    view.MoveCurrentTo(item);
                    break;
                } 
            }
        }
        private void media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (media.NaturalDuration != Duration.Automatic)
                sliderSeekBar.Maximum = media.NaturalDuration.TimeSpan.TotalMilliseconds;
        }
        private bool isSeekingByMedia;
        private void MediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (media.HasVideo)
            {
                gifPlayer.Visibility = Visibility.Collapsed;
                equalizer.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (menuEqualizer.IsChecked)
                {
                    if (isPlayingGif)
                    {
                        gifPlayer.Visibility = Visibility.Visible;
                        equalizer.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        gifPlayer.Visibility = Visibility.Collapsed;
                        equalizer.Visibility = Visibility.Visible;
                    }
                    
                }
                else
                {
                    gifPlayer.Visibility = Visibility.Collapsed;
                    equalizer.Visibility = Visibility.Collapsed;
                }
            }

            if (sliderSeekBar.IsDragging == false)
            {
                isSeekingByMedia = true;
                //Đánh dấu sliderSeekBar đang được set value bởi media element để event ValueChanged không seek nữa 
                sliderSeekBar.Value = media.Position.TotalMilliseconds;
                isSeekingByMedia = false;
            }

            var totalDuration = media.NaturalDuration.ToString();
            if (media.NaturalDuration.HasTimeSpan)
            {
                totalDuration = media.NaturalDuration.TimeSpan.ToString("mm\\:ss");
            }

            TextPosition = $"{media.Position.ToString("mm\\:ss")} / {totalDuration}";

            ClockState = media.Clock.CurrentState;

            // Khi play hết bài
            // nếu RepeatBehavior = Forever thì sẽ không bao giờ Filling
            // nhưng khi RepeatBehavior = Forever mới được set nó sẽ không có tác dụng ngay mà vẫn dùng RepeatBehavior cũ cho đến khi play lại
            // tức là nó vẫn Filling trong lần đầu tiên
            // lằng nhằng quá éo dùng RepeatBehavior nữa
            if (media.Clock.CurrentState == ClockState.Filling)
            {
                if (IsRepeat)
                {
                    Play();
                }
                else
                {
                    Stop();
                    Status = XPlayerStatus.End;
                    //Nếu không repeat thì next sang bài tiếp (nếu repeat thì play liên tục 1 bài)
                    Next();
                }
            }
        }
        private void sliderSeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // khi stop sẽ không cho tua đi tua lại nữa
            if (Status == XPlayerStatus.Stop || Status == XPlayerStatus.End)
            {
                e.Handled = false;
                return;
            }

            //ValueChanged xảy ra khi thumb dragging hoặc click chuột lên track (IsMoveToPointEnabled="True")
            if (sender is XSlider slider)
            {
                if (isSeekingByMedia == false)
                {
                    seekMedia.Offset = new TimeSpan(0, 0, 0, 0, (int)slider.Value);
                    btnSeek.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnSeek));
                }
            }

        }
        private void btnRepeat_Checked(object sender, RoutedEventArgs e)
        {
            mediaTimeline.RepeatBehavior = RepeatBehavior.Forever;

            if (media.Position != TimeSpan.Zero)
            {
                //play lại để mediatimeline nhận gía trị mới của RepeatBehavior
                int currentPosition = (int)sliderSeekBar.Value;
                Play();
                Seek(currentPosition);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsRepeat"));
        }
        public void Seek(int milliseconds)
        {
            seekMedia.Offset = new TimeSpan(0, 0, 0, 0, milliseconds);
            btnSeek.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnSeek));
        }
        private void btnRepeat_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaTimeline.RepeatBehavior = new RepeatBehavior(1);

            if (media.Position != TimeSpan.Zero)
            {
                int currentPosition = (int)sliderSeekBar.Value;
                Play();
                Seek(currentPosition);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRepeat)));
        }
        private void InitializeTitleAnimation()
        {
            double distance = ActualWidth;
            if (txblTitle.ActualWidth >= ActualWidth) distance = txblTitle.ActualWidth + 40;

            DoubleAnimation titleAnimation = new DoubleAnimation 
            { From = 20, To = -1 * distance, Duration = TimeSpan.FromSeconds(12), AutoReverse = false, RepeatBehavior = RepeatBehavior.Forever };
            var clock = titleAnimation.CreateClock();
            txblTitle.ApplyAnimationClock(Canvas.LeftProperty, clock);
            titleClockController?.Remove();
            titleClockController = clock.Controller;

            DoubleAnimation title1Animation = new DoubleAnimation
            { From = distance, To = 20, Duration = TimeSpan.FromSeconds(12), AutoReverse = false, RepeatBehavior = RepeatBehavior.Forever };
            var clock1 = title1Animation.CreateClock();
            txblTitle1.ApplyAnimationClock(Canvas.LeftProperty, clock1);
            title1ClockController?.Remove();
            title1ClockController = clock1.Controller;
        }
        private void HideTitle()
        {
            TextBlock textBlock = (TextBlock)Template.FindName("txblTitle", this);
            if (textBlock != null) textBlock.Visibility = Visibility.Hidden;
        }
        private void ShowTitle()
        {
            TextBlock textBlock = (TextBlock)Template.FindName("txblTitle", this);
            if (textBlock != null) textBlock.Visibility = Visibility.Visible;
        }
        private void BeginAnimateTitle()
        {
            HideTitle();
            canvasTitle.Visibility = Visibility.Visible;
            titleClockController?.Begin();
            title1ClockController?.Begin();
        }
        private void StopAnimateTitle()
        {
            ShowTitle();
            canvasTitle.Visibility = Visibility.Hidden;
            titleClockController?.Stop();
            title1ClockController?.Stop();
        }
        private void ShowPlayButton()
        {
            btnPlay.Visibility = Visibility.Visible;
            btnPause.Visibility = Visibility.Collapsed;
            btnResume.Visibility = Visibility.Collapsed;
        }
        private void ShowPauseButton()
        {
            btnPlay.Visibility = Visibility.Collapsed;
            btnPause.Visibility = Visibility.Visible;
            btnResume.Visibility = Visibility.Collapsed;
        }
        private void ShowResumeButton()
        {
            btnPlay.Visibility = Visibility.Collapsed;
            btnPause.Visibility = Visibility.Collapsed;
            btnResume.Visibility = Visibility.Visible;
        }
        private void Button_ControlMedia(object sender, RoutedEventArgs e)
        {
            if (FileNames.Count <= 0 || lsvFileNames.SelectedIndex < 0)
            {
                //DialogBox.ShowError("Không có file nào được chọn.");
                return;
            }

            if (sender is Button button)
            {
                if (button.Name == "btnPlay")
                {
                    PlayingItem = (string)lsvFileNames.SelectedItem;

                    ShowPauseButton();

                    sliderSeekBar.IsEnabled = true;

                    UpdateLayout();

                    Status = XPlayerStatus.Play;

                    gifPlayer.Play();

                    StoryboardEqualizer?.Begin();

                    txblTitle.Text = lsvFileNames.SelectedItem.ToString();
                    
                    InitializeTitleAnimation();
                    HideTitle();
                    canvasTitle.Visibility = Visibility.Visible;

                    NextEqualizer();
                }
                else if (button.Name == "btnPause")
                {
                    if (Status == XPlayerStatus.Pause || Status == XPlayerStatus.Stop)
                    {
                        return;
                    }
                    else if (Status == XPlayerStatus.Play || Status == XPlayerStatus.Resume)
                    {
                        ShowResumeButton();

                        Status = XPlayerStatus.Pause;
                        gifPlayer.Pause();
                        StoryboardEqualizer?.Pause();
                        StopAnimateTitle();
                    }
                    
                }
                else if (button.Name == "btnResume")
                {

                    if (Status == XPlayerStatus.Pause)
                    {
                        ShowPauseButton();

                        Status = XPlayerStatus.Resume;
                        gifPlayer.Play();
                        StoryboardEqualizer?.Resume();
                        BeginAnimateTitle();
                    }

                }
                else if (button.Name == "btnStop")
                {
                    if (Status != XPlayerStatus.Stop)
                    {
                        ShowPlayButton();

                        sliderSeekBar.IsEnabled = false;

                        Status = XPlayerStatus.Stop;
                        gifPlayer.Stop();
                        StoryboardEqualizer?.Stop();
                        StopAnimateTitle();
                    }
                }
            }
        }
        private void gifPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            gifPlayer.Position = TimeSpan.FromSeconds(1);
            gifPlayer.Play();
        }
        private bool isPlayingGif;
        private int equalizerIndex = 0;
        private void MenuItem_NextEqualizer(object sender, RoutedEventArgs e)
        {
            NextEqualizer();
        }

        public void NextEqualizer()
        {
            if (media.HasVideo) return;

            equalizerIndex++;
            if (equalizerIndex > GifFiles.Count + 4) equalizerIndex = 0;

            if (equalizerIndex < GifFiles.Count)
            {
                isPlayingGif = true;
                gifPlayer.Visibility = Visibility.Visible;
                equalizer.Visibility = Visibility.Hidden;
                gifPlayer.Source = new Uri(GifFiles[equalizerIndex]);
                gifPlayer.Play();
            }
            else
            {
                isPlayingGif = false;
                gifPlayer.Stop();
                gifPlayer.Visibility = Visibility.Hidden;
                equalizer.Visibility = Visibility.Visible;
                if (equalizerIndex == GifFiles.Count + 0) equalizer.Mode = 0;
                else if (equalizerIndex == GifFiles.Count + 1) equalizer.Mode = 1;
                else if (equalizerIndex == GifFiles.Count + 2) equalizer.Mode = 2;
                else if (equalizerIndex == GifFiles.Count + 3) equalizer.Mode = 3;
                else if (equalizerIndex == GifFiles.Count + 4) equalizer.Mode = 4;
            }
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Next();
        }
        public void Next()
        {
            //Nếu không phải là bài hát cuối list thì có thể next bằng cách thay đổi SelectedIndex và begin lại storyboard
            if (lsvFileNames.SelectedIndex < lsvFileNames.Items.Count - 1)
            {
                if (btnShuffle.IsChecked == true) lsvFileNames.SelectedIndex = new Random(new Random().Next(999)).Next(lsvFileNames.Items.Count);
                else lsvFileNames.SelectedIndex += 1;

                //Scroll đến giữa scrollviewer
                //var offset = (lsvFilenames.SelectedIndex * 1.0 / lsvFilenames.Items.Count) * svFilenames.ExtentHeight - svFilenames.ActualHeight/2;
                //svFilenames.ScrollToVerticalOffset(offset);
                ScrollToSelectedItem(lsvFileNames);

                Play();
            }

        }
        //scroll đến SelectedItem của 1 selector nếu nó được bao bởi scrollviewer, SelectedItem được scroll đến giữa scrollviewer
        private void ScrollToSelectedItem(Selector selector)
        {
            if (selector.Parent is ScrollViewer scrollViewer)
            {
                //Scroll đến giữa scrollviewer
                var offset = (selector.SelectedIndex * 1.0 / selector.Items.Count) * scrollViewer.ExtentHeight - scrollViewer.ActualHeight / 2;
                scrollViewer.ScrollToVerticalOffset(offset);
            }

        }
        private void btnShuffle_Checked(object sender, RoutedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsShuffle"));
        }
        private void btnShuffle_Unchecked(object sender, RoutedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsShuffle"));
        }
        private void lsvFileNames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileNames.Count <= 0 || lsvFileNames.SelectedIndex < 0) return;
            
            Play();
        }
        private void lsvFileNames_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files) FileNames.Add(file);
        }
        private void MenuItem_ControlMedia(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item.Header.ToString() == "Play") Play();
                if (item.Header.ToString() == "Pause") Pause();
                if (item.Header.ToString() == "Resume") Resume();
                if (item.Header.ToString() == "Stop") Stop();
                if (item.Header.ToString() == "Next") Next();
            }
        }
        private void svFileNames_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            //child element trong scroll viewer bị chặn event MouseWheel, dùng PreviewMouseWheel để tunnel ngược về  
            if (sender is ScrollViewer scrollViewer) scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (string.IsNullOrEmpty(textBox.Text)) return;
                var filename = FileNames.Where(fn => fn.ToUnsigned().ToLower().Contains(textBox.Text.ToUnsigned().ToLower())).FirstOrDefault();
                //lsvFilenames.ScrollIntoView(filename);
                if (string.IsNullOrEmpty(filename)) return;
                lsvFileNames.SelectedIndex = FileNames.IndexOf(filename);
                ScrollToSelectedItem(lsvFileNames);
            }
        }
        private void TextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "Search from Playlist") textBox.Clear();
            }
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "") textBox.Text = "Search from Playlist";
            }
        }
        private void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Stop();
            string message = $"{e.ErrorException.Message}\n{e.ErrorException.InnerException}\n{e.ErrorException.Source}\n{e.ErrorException.StackTrace}";
            message.ToFileText();
            DialogBox.ShowError("MediaElement Failed.\n" + e.ErrorException.Message);
            return;
        }
        private void MenuItem_RemoveFromPlaylist(object sender, RoutedEventArgs e)
        {
            if (FileNames.Count > 0 && lsvFileNames.SelectedItem != null)
            {
                var selectedItem = (string)lsvFileNames.SelectedItem;
                FileNames.Remove(selectedItem);
                if (PlayingItem == selectedItem)
                {
                    Stop();
                    return;
                }
                var view = CollectionViewSource.GetDefaultView(lsvFileNames.ItemsSource);
                view.Refresh();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (ShouldClose)
            {
                //var gifPath = @$"{Environment.CurrentDirectory}\ResourceEqualizer";
                //if (Directory.Exists(gifPath)) Directory.Delete(gifPath, true);

                Directory.CreateDirectory(PlaylistPath);
                SavePlaylist(PlaylistPath + LastPlaylistName);

                Directory.CreateDirectory(SettingPath);
                var settings = new[]
                {
                    new XPlayerSetting(){ Name = nameof(BackgroundView), Value = BackgroundView.CurrentItem?.ToString() },
                    new XPlayerSetting(){ Name = nameof(BorderColorView), Value = BorderColorView.CurrentItem?.ToString() }
                };

                settings.ToXmlFile(SettingPath + SettingFileName);
                //SaveFile(settingPath + "\\settings_1.xml", settings);
            }
            else
            {
                e.Cancel = true;
                Hide();
            }
        }
        public bool IsClosed { get; private set; }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            btnStop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnStop));
            IsClosed = true;
        }
        public void Play(string fileName)
        {
            if (AddToPlaylist(new string[] { fileName })) 
            {
                lsvFileNames.SelectedIndex = 0;

                Play();
            }
            else
            {
                DialogBox.ShowError("Đường dẫn không tồn tại.");
            }

        }
        public void Play()
        {
            if (FileNames.Count <= 0 || lsvFileNames.SelectedIndex < 0) return;

            btnPlay.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPlay));
        }
        public void Stop()
        {
            btnStop.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnStop));

        }
        public void Pause()
        {
            btnPause.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnPause));
        }
        public void Resume()
        {
            btnResume.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btnResume));

        }
        public bool AddToPlaylist(string[] fileNames, bool isOverwrite = true)
        {
            if (isOverwrite) 
            { 
                FileNames.Clear();
                PlaylistName = "New Playlist";
            }

            var lastCount = FileNames.Count;

            foreach (var item in fileNames)
            {
                if (FileNames.Contains(item)) continue;

                if (File.Exists(item)) FileNames.Add(item);
            }

            if (FileNames.Count > lastCount) return true;
            else return false;
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged) InitializeTitleAnimation();
        }
        private void MenuItem_AddToPlaylist(object sender, RoutedEventArgs e)
        {
            var fileNames = OpenFileNames();
            if (!string.IsNullOrEmpty(fileNames[0]))
            {
                AddToPlaylist(fileNames, false);
            }
        }
        private string[] OpenFileNames()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = AppName;
            openFileDialog.Multiselect = true;

            string filter = "Audio files|";
            foreach (var f in AudioFormats) filter += "*" + f + ";";
            filter += "|Video files| ";
            foreach (var f in VideoFormats) filter += "*" + f + ";";
            openFileDialog.Filter = filter;

            //openFileDialog.Filter = "Mp3 files |*.mp3|Media files |*.mp3;*.mp4;*.wav;*.flv;*.avi;*.m4a|All files |*.*";

            if (Directory.Exists(txbDefaultPath.Text)) openFileDialog.InitialDirectory = txbDefaultPath.Text;

            if (openFileDialog.ShowDialog() == true)
                return openFileDialog.FileNames;
            else
                return new string[] { string.Empty };
            
        }
        private void MenuItem_OpenPlaylist(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = AppName;
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "XML file|*.xml|Text file|*.txt";

            Directory.CreateDirectory(PlaylistPath);
            openFileDialog.InitialDirectory = PlaylistPath;

            if (openFileDialog.ShowDialog() == true)
            {
                OpenPlaylist(openFileDialog.FileName);
            }
        }
        private bool OpenPlaylist(string playlistName)
        {
            var playlist = OpenFile<List<string>>(playlistName);
            if (playlist != null)
            {
                FileNames.Clear();
                playlist.ForEach(f => FileNames.Add(f));
                //lsvFileNames.SelectedIndex = 0;
                PlaylistName = System.IO.Path.GetFileName(playlistName);
                return true;
            }
            else
            {
                return false;
            }
        }
        private T? OpenFile<T>(string fileName)
        {
            if (File.Exists(fileName))
            {
                using FileStream fileStream = File.OpenRead(fileName);
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                var file = (T?)xmlSerializer.Deserialize(fileStream);
                return file;
            }
            else
            {
                //DialogBox.ShowError("File Not Found.");
                return default;
            }
        }
        private void MenuItem_SavePlaylist(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = AppName;
            saveFileDialog.FileName = "Playlist";
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.Filter = "XML file|*.xml|Text file|*.txt";

            Directory.CreateDirectory(PlaylistPath);
            saveFileDialog.InitialDirectory = PlaylistPath;

            if (saveFileDialog.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(saveFileDialog.FileName))
                {
                    DialogBox.ShowError("Tên playlist không được để trống.");
                }
                else
                {
                    SavePlaylist(saveFileDialog.FileName);
                    PlaylistName = saveFileDialog.SafeFileName;
                }
            }
        }
        private void SavePlaylist(string playlistName)
        {
            if (FileNames.Count == 0) return;
            SaveFile(playlistName, FileNames);
        }
        private void SaveFile<T>(string fileName, T file)
        {
            using FileStream fileStream = File.Create(fileName);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            xmlSerializer.Serialize(fileStream, file);
        }
        private void MenuItem_ClearPlaylist(object sender, RoutedEventArgs e)
        {
            Stop();
            FileNames.Clear();
        }
        private void MenuItem_Open(object sender, RoutedEventArgs e)
        {
            var fileNames = OpenFileNames();
            if (!string.IsNullOrEmpty(fileNames[0]))
            {
                AddToPlaylist(fileNames);
                //lsvFileNames.SelectedIndex = 0;
                Play();
            }
        }
        private void MenuItem_Play(object sender, RoutedEventArgs e)
        {
            Play();
        }

    }
}
