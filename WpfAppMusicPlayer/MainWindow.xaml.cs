﻿using System;
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

namespace WpfAppMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private List<SongInfo> songs = new List<SongInfo>();
        private List<SongInfo> topSongs = new List<SongInfo>();
        private List<Album> albums = new List<Album>();

        private int currentSongIndex = -1;
        private bool isPlaying = false; // Biến để theo dõi trạng thái phát nhạc (đang phát hay tạm dừng)
        private string currentSongplayingPath = "";

        public MainWindow()
        {

            InitializeComponent();
            LoadSongsFromFolder();
            // Đăng ký sự kiện ValueChanged của Slider
            sliderTimeMusic.ValueChanged += SliderTimeMusic_ValueChanged;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            // Đặt giá trị ban đầu cho TextBlock là "00:00"
            timeMusicPlay.Text = "00:00";

            topSongs = GetSongsBySinger("Sơn Tùng M-TP");
            FillSongItems(topSongs);
        }

        private void SongItem_MouseDown(object sender, MouseButtonEventArgs e)
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
                    var selectedSong = topSongs[currentSongIndex];
                    currentSongplayingPath = selectedSong.FilePath;
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
                }
            }
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

            if (songs.Count > 0)
            {
                songs.Clear();
            }
            foreach (string filePath in Directory.GetFiles(musicFolderPath, "*.mp3"))
            {
                TagLib.File file = TagLib.File.Create(filePath);
                // Lấy tiêu đề bài hát
                string songName = file.Tag.Title;
                // Lấy tên ca sĩ
                string singer = file.Tag.Artists[0];
                string album = file.Tag.Album;
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                TimeSpan durationSong = file.Properties.Duration;

                // Sử dụng totalSeconds để khởi tạo Duration cho SongInfo
                SongInfo songInfo = new SongInfo
                {
                    FilePath = filePath,
                    SongName = songName,
                    Singer = singer,
                    Duration = durationSong,
                    Album = album
                };

                songs.Add(songInfo);
            };
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (topSongs.Count > 0)
            {
                if (currentSongIndex == 0)
                {
                    currentSongIndex = topSongs.Count - 1;
                }
                else currentSongIndex--;

                var selectedSong = topSongs[currentSongIndex];
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                sliderTimeMusic.Maximum = topSongs[currentSongIndex].Duration.TotalSeconds;
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

        private List<SongInfo>? GetSongsBySinger(string singer)
        {
            // Tạo một danh sách mới để lưu các bài hát của ca sĩ
            List<SongInfo> topSongs = new List<SongInfo>();

            // Lặp qua danh sách các bài hát
            foreach (var song in songs)
            {
                // Kiểm tra nếu ca sĩ của bài hát trùng khớp với ca sĩ đưa vào và danh sách chưa đủ 7 bài hát
                if (song.Singer.Equals(singer) && topSongs.Count < 7)
                {
                    // Thêm bài hát vào danh sách topSongs
                    topSongs.Add(song);
                }
            }

            // Trả về danh sách top 7 bài hát của ca sĩ
            return topSongs;
        }
        private void FillSongItems(List<SongInfo> listSongs)
        {
            // Xóa tất cả các phần tử trong StackPanel trước khi điền lại
            listDailySinger.Children.Clear();
            MediaPlayer mediaLoad = new MediaPlayer();

            // Thêm TextBlock "Daily Singer"
            TextBlock textBlock1 = new TextBlock
            {
                Text = "Daily Singer",
                Foreground = Brushes.White,
                FontSize = 26,
                FontWeight = FontWeights.Bold
            };
            listDailySinger.Children.Add(textBlock1);

            // Thêm TextBlock "Sơn Tùng"
            TextBlock textBlock2 = new TextBlock
            {
                Text = listSongs[0].Singer,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 222)),
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 20)
            };
            listDailySinger.Children.Add(textBlock2);
            // Tiến hành điền các SongItem vào StackPanel
            int i = 0;
            foreach (var song in listSongs)
            {
                i++;
                // Tạo một UserControl SongItem mới
                var songItem = new SongItem
                {
                    Number = i.ToString("00"), // Đánh số thứ tự bài hát
                    Title = song.SongName, // Lấy tiêu đề bài hát từ danh sách songs
                    Time = song.Duration.ToString(@"mm\:ss") // Chuyển đổi thời lượng từ TimeSpan sang định dạng mm:ss
                };
                // Gán sự kiện MouseDown cho UserControl SongItem
                songItem.MouseDown += SongItem_MouseDown;
                // Thêm UserControl SongItem vào StackPanel
                listDailySinger.Children.Add(songItem);
                if(i > 6)
                {
                    break;
                }
            }
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Lấy giá trị mới của slider
            double volumeValue = sliderVolume.Value;

            // Thiết lập âm lượng cho đối tượng MediaPlayer
            mediaPlayer.Volume = volumeValue / 100.0; // MediaPlayer.Volume yêu cầu giá trị trong khoảng từ 0 đến 1

            // Hiển thị giá trị âm lượng (ví dụ: trong một TextBlock)
            // textBlockVolumeValue.Text = volumeValue.ToString(); // Nếu bạn muốn hiển thị giá trị âm lượng
        }
        private void NextSong()
        {
            if (topSongs.Count > 0)
            {
                if (currentSongIndex < topSongs.Count - 1)
                {
                    currentSongIndex++;
                }
                else
                {
                    currentSongIndex = 0; // Quay lại bài đầu tiên nếu đã phát hết danh sách bài hát
                }

                var selectedSong = topSongs[currentSongIndex];
                mediaPlayer.Open(new Uri(selectedSong.FilePath));
                // Cập nhật giá trị tối đa của Slider là thời gian tổng của bài hát
                sliderTimeMusic.Maximum = topSongs[currentSongIndex].Duration.TotalSeconds;
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

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            tbMainTitle.Text = "Search On Youtube";
            string searchQuery = txtSearchQuery.Text.Trim();
            YoutubeService youtubeService = new YoutubeService();
            SearchListResponse result = await youtubeService.SearchOnYouTube(searchQuery);

            listDailySinger.Children.Clear();
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

                    Button downloadButton = new Button
                    {
                        Content = "Download",
                        Tag = searchResult.Id.VideoId
                    };
                    downloadButton.Click += DownloadButton_Click;

                    Grid grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition());

                    // Tiêu đề
                    System.Windows.Controls.TextBox titleBlock = new System.Windows.Controls.TextBox
                    {
                        Text = searchResult.Snippet.Title,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 10, 0),
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        Background = Brushes.Transparent
                    };

                    Grid.SetColumn(openLinkButton, 0);
                    grid.Children.Add(openLinkButton);

                    Grid.SetColumn(downloadButton, 1);
                    grid.Children.Add(downloadButton);

                    listDailySinger.Children.Add(titleBlock);
                    listDailySinger.Children.Add(grid);
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
            string videoId = downloadButton.Tag.ToString();

            await DownloadVideoAsMp3(videoId);
        }

        private async Task DownloadVideoAsMp3(string videoId)
        {
            YoutubeService youtubeService = new YoutubeService();
            await youtubeService.DownloadVideoAsMp3(videoId);
        }

        private async void btnAIGenerated_Click(object sender, RoutedEventArgs e)
        {
            tbMainTitle.Text = "AI Genarated";
            listDailySinger.Children.Clear();
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

                        listDailySinger.Children.Add(textBlock);
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
            tbMainTitle.Text = "Songs";
            LoadSongsFromFolder();
            ViewSongsList(songs);
        }

        private void ViewSongsList(List<SongInfo> songs)
        {
            listDailySinger.Children.Clear();
            int i = 0;
            foreach (var song in songs)
            {
                i++;
                var songItem = new SongItem
                {
                    Number = i.ToString("00"),
                    Title = song.SongName,
                    Time = song.Duration.ToString(@"mm\:ss")
                };
                songItem.MouseDown += SongItem_MouseDown;
                listDailySinger.Children.Add(songItem);
            }
        }

        private void btnViewAlbums_Click(object sender, RoutedEventArgs e)
        {
            tbMainTitle.Text = "Albums";
            listDailySinger.Children.Clear();
            LoadSongsFromFolder();
            AddAlbumsList(songs);
            int i = 0;
            foreach (var album  in albums)
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
                listDailySinger.Children.Add(songItem);
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
            tbMainTitle.Text = "Home";
            LoadSongsFromFolder();
            topSongs = GetSongsBySinger("Sơn Tùng M-TP");
            FillSongItems(topSongs);
        }
    }
}
