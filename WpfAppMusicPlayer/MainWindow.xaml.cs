using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfAppMusicPlayer.Models;
using WpfAppMusicPlayer.UserControls;
using MahApps.Metro.IconPacks;
using TagLib;
using WpfAppMusicPlayer.Utils;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Google.Apis.YouTube.v3.Data;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Globalization;
using NAudio.Wave;

namespace WpfAppMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private List<SongInfo> allListSongs = new List<SongInfo>(); // List chứa full bài hát
        private List<SongInfo> currentListSongs = new List<SongInfo>(); // List bài đang được phát
        private Queue<SongInfo> listeningHistory = new Queue<SongInfo>();
        private List<Album> albums = new List<Album>();
        private List<string> listSinger = new List<string>();
        private List<string> _suggestedSingers = new List<string>();

        private int currentSongIndex = -1;
        private bool isPlaying = false; // Biến để theo dõi trạng thái phát nhạc (đang phát hay tạm dừng)
        private string currentSongplayingPath = "";

        public MainWindow()
        {
            InitializeComponent();
            LoadSongsFromFolder();
            GetAllSinger();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            // Đặt giá trị ban đầu cho TextBlock là "00:00"
            timeMusicPlay.Text = "00:00";
            FillSongItems(allListSongs);
            LoadHistory();
            FillPopular(listeningHistory);
        }

        private void GetAllSinger()
        {
            HashSet<string> uniqueSingers = new HashSet<string>(); // HashSet để lưu trữ các giá trị duy nhất
            foreach (var song in allListSongs)
            {
                if (!uniqueSingers.Contains(song.SingerName)) // Kiểm tra xem ca sĩ đã tồn tại trong HashSet chưa
                {
                    uniqueSingers.Add(song.SingerName); // Nếu chưa tồn tại, thêm vào HashSet
                    listSinger.Add(song.SingerName); // Thêm vào danh sách ca sĩ
                }
            }
        }

        private void SongItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                SongItem songItem = (SongItem)sender;
                songItem.ContextMenu.IsOpen = true;
            } else
            {
                var songItem = sender as SongItem;
                if (songItem != null)
                {
                    // Tìm vị trí của bài hát trong danh sách
                    int songIndex = Convert.ToInt32(songItem.Number) - 1;
                    // Kiểm tra xem có phải bài hát đang phát hiện tại không
                    if (currentSongIndex == songIndex)
                    {
                        // Nếu là bài hát đang phát, chỉ cần tạm dừng hoặc tiếp tục phát
                        if (isPlaying)
                        {
                            mediaPlayer.Pause();
                            isPlaying = false;
                            playPauseButtonIcon.Kind = PackIconMaterialKind.Play;
                        }
                        else
                        {
                            mediaPlayer.Play();
                            isPlaying = true;
                            playPauseButtonIcon.Kind = PackIconMaterialKind.Pause;
                        }
                    }
                    else
                    {
                        // Nếu không phải là bài hát đang phát, chuyển sang bài hát mới
                        currentSongIndex = songIndex;
                        var selectedSong = currentListSongs[currentSongIndex];

                        var bitmapImage = new BitmapImage();
                        if (System.IO.File.Exists(selectedSong.ImgSinger))
                        {
                            bitmapImage = new BitmapImage(new Uri(selectedSong.ImgSinger, UriKind.RelativeOrAbsolute));
                        }
                        else
                        {
                            bitmapImage = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/NoImageAvailable.jpg", UriKind.RelativeOrAbsolute));
                        }
                        imgCurrentSinger.ImageSource = bitmapImage;
                        nameCurrentSong.Text = selectedSong.SongName;
                        nameCurrentSinger.Text = selectedSong.SingerName;
                        mediaPlayer.Open(new Uri(selectedSong.FilePath));
                        // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                        sliderTimeMusic.Maximum = selectedSong.Duration.TotalSeconds;
                        // Bắt đầu gọi UpdateSliderValue để cập nhật giá trị của Slider mỗi giây
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(1); // Cập nhật giá trị mỗi giây
                        timer.Tick += (timerSender, timerArgs) => UpdateSliderValue();
                        timer.Start();
                        mediaPlayer.Play();
                        isPlaying = true;
                        playPauseButtonIcon.Kind = PackIconMaterialKind.Pause;
                        AddSongToHistory(selectedSong);
                        FillPopular(listeningHistory);
                    }
                }
            }

        }
        private void PopularSong_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Lặp qua tất cả các phần tử trong StackPanel
            foreach (PopularSong song in listPopulars.Children)
            {
                // Đặt IsActive="False" cho tất cả các phần tử
                song.IsActive = false;
            }
            // Đặt IsActive="True" cho phần tử được double-click
            ((PopularSong)sender).IsActive = true;

            var songPopular = sender as PopularSong;
            if (songPopular != null)
            {
                SongInfo song = GetSongBySongName(songPopular.Title);

                var bitmapImage = new BitmapImage();
                if (System.IO.File.Exists(song.ImgSinger))
                {
                    bitmapImage = new BitmapImage(new Uri(song.ImgSinger, UriKind.RelativeOrAbsolute));
                }
                else
                {
                    bitmapImage = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/NoImageAvailable.jpg", UriKind.RelativeOrAbsolute));
                }
                imgCurrentSinger.ImageSource = bitmapImage;
                nameCurrentSong.Text = song.SongName;
                nameCurrentSinger.Text = song.SingerName;
                // Nếu không phải là bài hát đang phát, chuyển sang bài hát mới
                mediaPlayer.Open(new Uri(song.FilePath));
                // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                sliderTimeMusic.Maximum = song.Duration.TotalSeconds;
                // Bắt đầu gọi UpdateSliderValue để cập nhật giá trị của Slider mỗi giây
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1); // Cập nhật giá trị mỗi giây
                timer.Tick += (timerSender, timerArgs) => UpdateSliderValue();
                timer.Start();
                mediaPlayer.Play();
                isPlaying = true;
                playPauseButtonIcon.Kind = PackIconMaterialKind.Pause;
                AddSongToHistory(song);
            }
        }
        private SongInfo GetSongBySongName(string songName)
        {
            foreach (var song in allListSongs)
            {
                if(song.SongName == songName)
                {
                    return song;
                }
            }
            return null;
        }
        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            playPauseButtonIcon.Kind = PackIconMaterialKind.Play; // Đổi icon thành "Play"
            // Khi bài hát kết thúc, đặt biến isPlaying về false
            isPlaying = false;
            // Chuyển sang bài hát tiếp theo nếu có
            NextSong();
        }

        private void LoadSongsFromFolder()
        {
            string musicFolderPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

            if (allListSongs.Count > 0)
            {
                allListSongs.Clear();
            }
            foreach (string filePath in Directory.GetFiles(musicFolderPath, "*.mp3"))
            {
                TagLib.File file = TagLib.File.Create(filePath);
                string? songName = file.Tag.Title;
                string? singerName = "";
                if (file.Tag.Artists.Length > 0)
                {
                    singerName = file.Tag.Artists[0];
                }
                string? album = file.Tag.Album;
                string? fileName = Path.GetFileNameWithoutExtension(filePath);
                string? genres = string.Join(", ", file.Tag.Genres);
                TimeSpan durationSong = file.Properties.Duration;
                string? imgSinger = "..\\..\\..\\Images\\" + singerName + ".jpg";
                IPicture? picture = null;
                if (file.Tag.Pictures.Length > 0)
                {
                    picture = file.Tag.Pictures[0];
                }

                // Sử dụng totalSeconds để khởi tạo Duration cho SongInfo
                SongInfo songInfo = new SongInfo
                {
                    FilePath = filePath,
                    SongName = songName,
                    SingerName = singerName,
                    Duration = durationSong,
                    Album = album,
                    ImgSinger = imgSinger,
                    Genres = genres,
                    Picture = picture
                };

                allListSongs.Add(songInfo);
            };
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentListSongs.Count > 0)
            {
                if (currentSongIndex == 0)
                {
                    currentSongIndex = currentListSongs.Count - 1;
                }
                else currentSongIndex--;

                var selectedSong = currentListSongs[currentSongIndex];
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                sliderTimeMusic.Maximum = currentListSongs[currentSongIndex].Duration.TotalSeconds;
                // Bắt đầu gọi UpdateSliderValue để cập nhật giá trị của Slider mỗi giây
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1); // Cập nhật giá trị mỗi giây
                timer.Tick += (timerSender, timerArgs) => UpdateSliderValue();
                timer.Start();
                mediaPlayer.Play();
                isPlaying = true;
                playPauseButtonIcon.Kind = PackIconMaterialKind.Pause;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                // Nếu đang phát, thì chuyển sang biểu tượng tạm dừng
                playPauseButtonIcon.Kind = PackIconMaterialKind.Play; // Đổi icon thành "Pause"
                // Tạm dừng phát nhạc tại vị trí hiện tại
                isPlaying = false;
                mediaPlayer.Pause();
            }
            else
            {
                playPauseButtonIcon.Kind = PackIconMaterialKind.Pause; // Đổi icon thành "Play"
                // Tiếp tục phát nhạc từ vị trí tạm dừng
                isPlaying = true;
                mediaPlayer.Play();
            }
        }


        // Trong một phương thức hoặc sự kiện khác để cập nhật giá trị của Slider
        private void UpdateSliderValue()
        {
            // Nếu đang phát nhạc
            if (mediaPlayer != null && mediaPlayer.NaturalDuration.HasTimeSpan && mediaPlayer.Position.TotalSeconds != sliderTimeMusic.Value)
            {
                // Cập nhật giá trị của Slider theo thời gian hiện tại của bài hát
                sliderTimeMusic.Value = mediaPlayer.Position.TotalSeconds;

                // Cập nhật nội dung của TextBlock hiển thị thời gian
                TimeSpan currentTime = TimeSpan.FromSeconds(sliderTimeMusic.Value);
                timeMusicPlay.Text = currentTime.ToString(@"mm\:ss");
            }
        }

        private void SliderTimeMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Kiểm tra xem người dùng đang thay đổi giá trị của Slider bằng cách kéo thanh hay không
            if (e.Source is Slider slider && mediaPlayer != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                // Thiết lập thời gian phát của bài hát tới thời gian mới tương ứng với giá trị của Slider
                mediaPlayer.Position = TimeSpan.FromSeconds(slider.Value);

                // Cập nhật nội dung của TextBlock hiển thị thời gian
                TimeSpan currentTime = TimeSpan.FromSeconds(slider.Value);
                timeMusicPlay.Text = currentTime.ToString(@"mm\:ss");
            }
        }

        private List<SongInfo> GetSongsBySinger(string singer)
        {
            // Tạo một danh sách mới để lưu các bài hát của ca sĩ
            List<SongInfo> topSongs = new List<SongInfo>();

            // Lặp qua danh sách các bài hát
            foreach (var song in allListSongs)
            {
                // Kiểm tra nếu ca sĩ của bài hát trùng khớp với ca sĩ đưa vào và danh sách chưa đủ 7 bài hát
                if (song.SingerName.Equals(singer) && topSongs.Count < 7)
                {
                    // Thêm bài hát vào danh sách topSongs
                    topSongs.Add(song);
                }
            }

            return topSongs;
        }

        private void FillSongItems(List<SongInfo> listSongs)
        {
            // Xóa tất cả các phần tử trong StackPanel trước khi điền lại
            listSongBySinger.Children.Clear();
            MediaPlayer mediaLoad = new MediaPlayer();

            TextBlock textBlock1;
            if (listSongs.Count == allListSongs.Count)
            {
                textBlock1 = new TextBlock
                {
                    Text = "All Songs",
                    Foreground = Brushes.White,
                    FontSize = 26,
                    FontWeight = FontWeights.Bold
                };
                listSongBySinger.Children.Add(textBlock1);
            }
            else
            {
                textBlock1 = new TextBlock
                {
                    Text = listSongs[0].SingerName,
                    Foreground = Brushes.White,
                    FontSize = 26,
                    FontWeight = FontWeights.Bold
                };
                listSongBySinger.Children.Add(textBlock1);
            }

            // Tiến hành điền các SongItem vào StackPanel
            int i = 0;
            foreach (var song in listSongs)
            {
                i++;
                // Tạo một UserControl SongItem mới
                var songItem = new SongItem
                {
                    SongInfo = song,
                    Number = i.ToString("00"), // Đánh số thứ tự bài hát
                    Title = song.SongName, // Lấy tiêu đề bài hát từ danh sách songs
                    Time = song.Duration.ToString(@"mm\:ss") // Chuyển đổi thời lượng từ TimeSpan sang định dạng mm:ss
                };
                // Gán sự kiện MouseDown cho UserControl SongItem
                songItem.MouseDown += SongItem_MouseDown;
                // Thêm UserControl SongItem vào StackPanel

                songItem.ContextMenu = GetSongItemContextMenu(songItem.SongInfo);
                songItem.ContextMenu.Tag = songItem.SongInfo;
                listSongBySinger.Children.Add(songItem);

            }
            currentListSongs = listSongs;
        }

        private void FillPopular(Queue<SongInfo> listSongs)
        {
            // Xóa tất cả các phần tử trong StackPanel trước khi điền lại
            listPopulars.Children.Clear();
            for (int i = listSongs.Count - 1; i >= listSongs.Count - 6; i--)
            {
                var song = listSongs.ElementAt(i);
                // Chuyển đổi đường dẫn hình ảnh sang kiểu ImageSource
                BitmapImage bitmapImage = new BitmapImage();
                var popularSong = new PopularSong();

                bitmapImage.BeginInit();
                if (System.IO.File.Exists(song.ImgSinger))
                {
                    bitmapImage.UriSource = new Uri(song.ImgSinger, UriKind.RelativeOrAbsolute);
                }
                else
                {
                    bitmapImage.UriSource = new Uri("../../../../WpfAppMusicPlayer/Images/NoImageAvailable.jpg", UriKind.RelativeOrAbsolute);
                }
                bitmapImage.EndInit();
                popularSong = new PopularSong
                {
                    Title = song.SongName, // Lấy tiêu đề bài hát từ danh sách songs
                    Time = song.Duration.ToString(@"mm\:ss"), // Chuyển đổi thời lượng từ TimeSpan sang định dạng mm:ss
                    Image = bitmapImage
                };

                // Gán sự kiện MouseDown cho UserControl SongItem
                popularSong.MouseDoubleClick += PopularSong_DoubleClick;
                // Thêm UserControl SongItem vào StackPanel
                listPopulars.Children.Add(popularSong);
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Lấy giá trị mới của slider
            double volumeValue = sliderVolume.Value;

            // Thiết lập âm lượng cho đối tượng MediaPlayer
            mediaPlayer.Volume = volumeValue / 100.0; // MediaPlayer.Volume yêu cầu giá trị trong khoảng từ 0 đến 1
        }

        private void NextSong()
        {
            if (currentListSongs.Count > 0)
            {
                if (currentSongIndex < currentListSongs.Count - 1)
                {
                    currentSongIndex++;
                }
                else
                {
                    currentSongIndex = 0; // Quay lại bài đầu tiên nếu đã phát hết danh sách bài hát
                }

                var selectedSong = currentListSongs[currentSongIndex];
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                sliderTimeMusic.Maximum = currentListSongs[currentSongIndex].Duration.TotalSeconds;
                // Bắt đầu gọi UpdateSliderValue để cập nhật giá trị của Slider mỗi giây
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1); // Cập nhật giá trị mỗi giây
                timer.Tick += (timerSender, timerArgs) => UpdateSliderValue();
                timer.Start();
                mediaPlayer.Play();
                isPlaying = true;
                playPauseButtonIcon.Kind = PackIconMaterialKind.Pause;
            }
        }

        // Thêm một bài hát vào lịch sử và lưu vào file JSON
        public void AddSongToHistory(SongInfo song)
        {
            listeningHistory.Enqueue(song); // Thêm bài hát vào cuối hàng đợi
            while (listeningHistory.Count > 6)
            {
                listeningHistory.Dequeue(); // Nếu vượt quá số lượng tối đa, loại bỏ bài hát cũ nhất
            }
            SaveHistory(); // Lưu lại lịch sử mới vào file JSON
        }
        // Lưu lịch sử vào file JSON
        private void SaveHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(listeningHistory.ToArray(), Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText("listeningHistory.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi ghi dữ liệu vào tệp: " + ex.Message);
            }
        }
        // Load lịch sử từ file JSON
        private void LoadHistory()
        {
            if (System.IO.File.Exists("listeningHistory.json"))
            {
                string json = System.IO.File.ReadAllText("listeningHistory.json");
                if (!string.IsNullOrEmpty(json))
                {
                    SongInfo[] songsInHistory = JsonConvert.DeserializeObject<SongInfo[]>(json);

                    // Lọc ra các bài hát trong lịch sử mà có trong allListSongs
                    List<SongInfo> validSongs = songsInHistory.Where(song => allListSongs.Any(s => s.FilePath == song.FilePath)).ToList();

                    // Tạo Queue từ các bài hát hợp lệ
                    listeningHistory = new Queue<SongInfo>(validSongs);
                }
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            tbMainTitle.Text = "Search On YouTube";
            string searchQuery = txtSearchQuery.Text.Trim();
            YoutubeService youtubeService = new YoutubeService();
            SearchListResponse result = await youtubeService.SearchOnYouTube(searchQuery);
            listSongBySinger.Children.Clear();

            // Tạo Style cho Button
            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(2, 190, 104))));
            buttonStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            buttonStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
            buttonStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            buttonStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(5, 0, 15, 0))); // Thêm margin cho Button

            // Tạo Style cho TextBox
            Style textBoxStyle = new Style(typeof(System.Windows.Controls.TextBox));
            textBoxStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            textBoxStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            textBoxStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            textBoxStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
            textBoxStyle.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 15, 0, 0))); // Thêm margin cho TextBox

            foreach (var searchResult in result.Items)
            {
                if (searchResult.Id.Kind == "youtube#video")
                {
                    Button openLinkButton = new Button
                    {
                        Content = "Open YouTube",
                        Tag = searchResult.Id.VideoId
                    };
                    openLinkButton.Click += OpenLinkButton_Click;
                    openLinkButton.Style = buttonStyle;

                    Button downloadButton = new Button
                    {
                        Content = "Download",
                        Tag = searchResult
                    };
                    downloadButton.Click += DownloadButton_Click;
                    downloadButton.Style = buttonStyle;

                    System.Windows.Controls.TextBox titleBlock = new System.Windows.Controls.TextBox
                    {
                        Text = searchResult.Snippet.Title,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false
                    };
                    titleBlock.Style = textBoxStyle;

                    Grid grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    Grid.SetRow(titleBlock, 0);
                    grid.Children.Add(titleBlock);

                    Grid buttonGrid = new Grid();
                    buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    Grid.SetColumn(openLinkButton, 0);
                    buttonGrid.Children.Add(openLinkButton);

                    Grid.SetColumn(downloadButton, 1);
                    buttonGrid.Children.Add(downloadButton);

                    Grid.SetRow(buttonGrid, 1);
                    grid.Children.Add(buttonGrid);

                    listSongBySinger.Children.Add(grid);
                }
            }
        }

        private void OpenLinkButton_Click(object sender, RoutedEventArgs e)
        {
            Button openLinkButton = (Button)sender;
            string videoId = openLinkButton.Tag.ToString();

            string youtubeUrl = $"https://www.youtube.com/watch?v={videoId}";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = youtubeUrl,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening link: {ex.Message}");
            }
        }
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            Button downloadButton = (Button)sender;
            SearchResult searchResult = (SearchResult)downloadButton.Tag;

            await DownloadVideoAsMp3(searchResult.Id.VideoId, searchResult.Snippet.Title);
        }

        private async Task DownloadVideoAsMp3(string videoId, string title)
        {
            YoutubeService youtubeService = new YoutubeService();
            await youtubeService.DownloadVideoAsMp3(videoId, title);
        }

        private async void btnAIGenerated_Click(object sender, RoutedEventArgs e)
        {
            btnViewAlbums.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewAlbums.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewSongs.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewSongs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnHome.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnHome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnAIGenerated.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02be68")); // Đổi màu nền thành màu xanh
            btnAIGenerated.Foreground = Brushes.White; // Đổi màu văn bản thành màu trắng
            tbMainTitle.Text = "AI Genarated";
            listSongBySinger.Children.Clear();
            if (currentSongplayingPath != null)
            {
                var cloudStorageHelper = new CloudStorageHelper();
                string storageUri = await cloudStorageHelper.UploadAudioFileAsync(currentSongplayingPath);

                var speechToTextHelper = new SpeechToTextHelper();
                List<Tuple<double, string>> lyricsWithTimestamps = await speechToTextHelper.RecognizeSpeechFromStorageAsync(storageUri);

                if (lyricsWithTimestamps != null)
                {
                    foreach (var lyricTuple in lyricsWithTimestamps)
                    {
                        double startTime = lyricTuple.Item1;
                        string lyrics = lyricTuple.Item2;

                        TextBlock textBlock = new TextBlock
                        {
                            Text = $"{startTime:N2}s: {lyrics}",
                            Foreground = Brushes.White,
                            FontSize = 26,
                            FontWeight = FontWeights.Bold,
                            TextWrapping = TextWrapping.Wrap
                        };

                        listSongBySinger.Children.Add(textBlock);
                    }
                }
                /*                var requestBody = new
                                {
                                    apiKey = "mRvBBkPJuWbBDUZYrB6h561UxHH3",
                                    apiSecret = "iJv3JMUvIS",
                                    type = "upload",
                                    fileUrl = "https://www.youtube.com/watch?v=nfs8NYg7yQM&ab_channel=CharliePuth",
                                    fileName = "YOUR_FILE_NAME",
                                    language = "en-US",
                                    numSpeakers = "1"
                                };

                                string jsonBody = JsonConvert.SerializeObject(requestBody);


                                using (HttpClient client = new HttpClient())
                                {
                                    string url = "https://speechlogger.com/api/transcribe";

                                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                                    client.DefaultRequestHeaders.Add("Origin", "https://speechnotes.co/files/");

                                    try
                                    {
                                        HttpResponseMessage response = await client.PostAsync(url, content);

                                        string responseContent = await response.Content.ReadAsStringAsync();

                                        TextBlock textBlock = new TextBlock
                                        {
                                            Text = responseContent,
                                            Foreground = Brushes.White,
                                            FontSize = 26,
                                            FontWeight = FontWeights.Bold,
                                            TextWrapping = TextWrapping.Wrap
                                        };
                                        listDailySinger.Children.Add(textBlock);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error: {ex.Message}");
                                    }
                                }*/
            }
        }

        private void btnViewSongs_Click(object sender, RoutedEventArgs e)
        {

            btnAIGenerated.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnAIGenerated.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnHome.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnHome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewAlbums.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewAlbums.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewSongs.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02be68")); // Đổi màu nền thành màu xanh
            btnViewSongs.Foreground = Brushes.White; // Đổi màu văn bản thành màu trắng
            tbMainTitle.Text = "Songs";
            LoadSongsFromFolder();
            ViewSongsList(allListSongs);
        }

        private void ViewSongsList(List<SongInfo> songs)
        {
            listSongBySinger.Children.Clear();
            int i = 0;
            foreach (var song in songs)
            {
                i++;
                var songItem = new SongItem
                {
                    SongInfo = song,
                    Number = i.ToString("00"),
                    Title = song.SongName,
                    Time = song.Duration.ToString(@"mm\:ss")
                };
                songItem.ContextMenu = GetSongItemContextMenu(songItem.SongInfo);
                songItem.ContextMenu.Tag = songItem.SongInfo;

                songItem.MouseDown += SongItem_MouseDown;
                listSongBySinger.Children.Add(songItem);

            }
        }

        private ContextMenu GetSongItemContextMenu(SongInfo songInfo)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem editMenuItem = new MenuItem() { Header = "Edit" , Icon = new Image { Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/Icons/Edit.png", UriKind.RelativeOrAbsolute)) } };
            MenuItem addToAlbumMenuItem = new MenuItem() { Header = "Add to album ...", Icon = new Image { Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/Icons/Add.png", UriKind.RelativeOrAbsolute)) } };
            
            MenuItem openFolderMenuItem = new MenuItem() { Header = "Open Containing Folder", Icon = new Image { Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/Icons/OpenFolder.png", UriKind.RelativeOrAbsolute)) } };
            MenuItem propertiesMenuItem = new MenuItem() { Header = "Properties", Icon = new Image { Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/Icons/Properties.png", UriKind.RelativeOrAbsolute)) } };

            AddAlbumsList(allListSongs);
            foreach (var album in albums)
            {
                MenuItem albumItem = new MenuItem() { Header = album.Name, Icon = new Image { Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/Icons/Album.png", UriKind.RelativeOrAbsolute)) } };
                albumItem.Click += AddToAlbum_Click;
                albumItem.Tag = songInfo;
                addToAlbumMenuItem.Items.Add(albumItem);
            }

            editMenuItem.Click += EditMenuItem_Click;
            openFolderMenuItem.Click += OpenFolderMenuItem_Click;     
            propertiesMenuItem.Click += PropertiesMenuItem_Click;

            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(openFolderMenuItem);
            contextMenu.Items.Add(addToAlbumMenuItem);
            contextMenu.Items.Add(propertiesMenuItem);

            return contextMenu;
        }

        private void AddToAlbum_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý sự kiện khi click vào Edit
            MenuItem menuItem = (MenuItem)sender;
            SongInfo songInfo = (SongInfo)menuItem.Tag;

            TagLib.File file = TagLib.File.Create(songInfo.FilePath);
            if (file != null)
            {
                file.Tag.Album = menuItem.Header.ToString();
                file.Save();
            }
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý sự kiện khi click vào Edit
            MenuItem menuItem = (MenuItem)sender;

            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            SongInfo songInfo = (SongInfo)contextMenu.Tag;

            if (songInfo != null)
            {
                var editWindow = new EditProperties(songInfo);
                editWindow.ShowDialog();
            }
        }

        private void OpenFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            ContextMenu contextMenu = (ContextMenu)menuItem.Parent;

            SongInfo songInfo = (SongInfo)contextMenu.Tag;

            string songPath = songInfo.FilePath;

            Process.Start("explorer.exe", $"/select,\"{songPath}\"");
        }

        private void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý sự kiện khi click vào Properties
            MenuItem menuItem = sender as MenuItem;

            ContextMenu contextMenu = menuItem.Parent as ContextMenu;

            SongInfo songInfo = contextMenu.Tag as SongInfo;

            if (songInfo != null)
            {
                try
                {

                    var file = TagLib.File.Create(songInfo.FilePath);
                    string message = $"Title: {songInfo.SongName}\n" +
                        $"Artist: {songInfo.SingerName}\n" +
                        $"Album: {songInfo.Album}\n" +
                        $"Genres: {songInfo.Genres}\n" +
                        $"Duration: {songInfo.Duration}\n" +
                        $"File Path: {songInfo.FilePath}";
                    // Lấy hình ảnh đầu tiên từ danh sách hình ảnh nhúng trong file nhạc (nếu có)
                    if (songInfo.Picture != null)
                    {
                        var picture = file.Tag.Pictures[0];
                        MemoryStream stream = new MemoryStream(picture.Data.Data);
                        BitmapImage imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.StreamSource = stream;
                        imageSource.EndInit();
                        ShowPropertiesForm(imageSource, message);

                    }
                    else
                    {
                        ShowPropertiesForm(null, message);
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý nếu có lỗi xảy ra
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ShowPropertiesForm(BitmapImage? image, string message)
        {
            var window = new Properties(image, message);
            window.ShowDialog();
        }       

        private void btnViewAlbums_Click(object sender, RoutedEventArgs e)
        {
            btnAIGenerated.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnAIGenerated.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnHome.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnHome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewSongs.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewSongs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewAlbums.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02be68")); // Đổi màu nền thành màu xanh
            btnViewAlbums.Foreground = Brushes.White; // Đổi màu văn bản thành màu trắng
            tbMainTitle.Text = "Albums";
            listSongBySinger.Children.Clear();
            LoadSongsFromFolder();
            AddAlbumsList(allListSongs);
            int i = 0;
            foreach (var album in albums)
            {
                i++;
                var songItem = new SongItem
                {
                    Number = i.ToString("00"),
                    Title = album.Name,
                    Time = $"{album.Songs.Count} bài hát"
                };
                songItem.Tag = album.Songs;
                songItem.MouseDown += AlbumItem_MouseDown;
                listSongBySinger.Children.Add(songItem);
            }
        }

        private void AddAlbumsList(List<SongInfo> songs)
        {
            if (albums.Count > 0)
            {
                albums.Clear();
            }
            foreach (var song in songs)
            {
                if (song.Album != null)
                {
                    Album album = albums.Find(name => name.Name == song.Album);
                    if (album != null)
                    {
                        album.Songs.Add(song);
                    }
                    else
                    {
                        albums.Add(new Album
                        {
                            Name = song.Album,
                            Songs = new List<SongInfo> { song }
                        });
                    }
                }
                else
                {
                    Album album = albums.Find(name => name.Name == "Unknown");
                    if (album != null)
                    {
                        album.Songs.Add(song);
                    }
                    else
                    {
                        albums.Add(new Album
                        {
                            Name = "Unknown",
                            Songs = new List<SongInfo> { song }
                        });
                    }
                }
            }
        }

        private void AlbumItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var songs = (List<SongInfo>)((SongItem)sender).Tag;

            ViewSongsList(songs);
        }
        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            btnAIGenerated.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnAIGenerated.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewAlbums.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewAlbums.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnViewSongs.Background = Brushes.White; // Đặt lại màu nền của btnViewAlbums
            btnViewSongs.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9ae5c3"));
            btnHome.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#02be68")); // Đổi màu nền thành màu xanh
            btnHome.Foreground = Brushes.White; // Đổi màu văn bản thành màu trắng
            tbMainTitle.Text = "Home";
            FillSongItems(allListSongs);
            LoadHistory();
            FillPopular(listeningHistory);
        }

        // Hàm loại bỏ dấu tiếng Việt
        private string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (c == 'đ' || c == 'Đ')
                {
                    stringBuilder.Append('d');
                }
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private void txtSearchSinger_TextChanged(object sender, TextChangedEventArgs e)
        {
            lstSuggestions.Visibility = Visibility.Visible;
            string searchText = txtSearchSinger.Text.ToLower();
            List<string> newSuggestions = new List<string>(); // Tạo danh sách mới để lưu các gợi ý mới

            // Lặp qua danh sách ca sĩ và thêm các gợi ý phù hợp vào danh sách mới
            foreach (string singer in listSinger)
            {
                if (RemoveDiacritics(singer.ToLower()).Contains(searchText))
                {
                    newSuggestions.Add(singer);
                }
            }

            // Gán danh sách mới cho danh sách gợi ý
            lstSuggestions.ItemsSource = newSuggestions;

            if (_suggestedSingers.Count == 0 && string.IsNullOrEmpty(searchText))
            {
                lstSuggestions.Visibility = Visibility.Collapsed;
                FillSongItems(allListSongs);
            }
            else
            {
                lstSuggestions.Visibility = Visibility.Visible;
            }
        }


        private void lstSuggestions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string selectedSinger = (string)lstSuggestions.SelectedItem;
            if (selectedSinger != null)
            {
                // Xử lý khi chọn ca sĩ, ví dụ: hiển thị danh sách bài hát của ca sĩ đó
                FillSongItems(GetSongsBySinger(selectedSinger));
                lstSuggestions.Visibility = Visibility.Collapsed;
			}
		}
		
        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtSearchQuery.Text = "Listening Now.....";

                // Sử dụng NAudio để ghi âm từ microphone và lưu vào một MemoryStream
                using (MemoryStream stream = new MemoryStream())
                {
                    using (WaveInEvent waveIn = new WaveInEvent())
                    {
                        // Cấu hình WaveInEvent để ghi âm từ default audio device
                        waveIn.DeviceNumber = 0;
                        waveIn.WaveFormat = new WaveFormat(16000, 1); // Sample rate 16000Hz, 16-bit, mono

                        // Xử lý sự kiện khi có dữ liệu âm thanh ghi từ microphone
                        waveIn.DataAvailable += (s, args) =>
                        {
                            stream.Write(args.Buffer, 0, args.BytesRecorded);
                        };

                        // Bắt đầu ghi âm
                        waveIn.StartRecording();

                        // Chờ một khoảng thời gian (ví dụ: 5 giây) sau đó dừng ghi âm
                        await Task.Delay(TimeSpan.FromSeconds(5)); // Đợi 5 giây
                        waveIn.StopRecording();
                    }

                    // Đặt con trỏ về đầu của MemoryStream để đọc dữ liệu âm thanh
                    stream.Seek(0, SeekOrigin.Begin);

                    // Khởi tạo một instance của SpeechToTextHelper
                    var speechToTextHelper = new SpeechToTextHelper();

                    // Gọi phương thức RecognizeSpeechFromAudioStreamAsync để nhận dạng giọng nói từ stream audio
                    string transcript = await speechToTextHelper.RecognizeSpeechFromAudioStreamAsync(stream);

                    // Hiển thị kết quả nhận dạng trong textBox1
                    if (!string.IsNullOrEmpty(transcript))
                    {
                        txtSearchQuery.Text = transcript;
                    }
                    else
                    {
                        txtSearchQuery.Text = "No recognition result.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recognizing speech: {ex.Message}");
            }
        }

        private void btnKaraOke_Click(object sender, RoutedEventArgs e)
        {
            KaraOkeWindow karaOkeWindow = new KaraOkeWindow();
            karaOkeWindow.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ButtonSmall_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra trạng thái hiện tại của cửa sổ
            if (WindowState == WindowState.Normal)
            {
                // Nếu cửa sổ đang ở trạng thái bình thường, chuyển sang trạng thái phóng to
                WindowState = WindowState.Maximized;
            }
            else
            {
                // Nếu cửa sổ đang ở trạng thái phóng to, chuyển sang trạng thái bình thường
                WindowState = WindowState.Normal;
            }
        }

        private void ButtonDesktop_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
