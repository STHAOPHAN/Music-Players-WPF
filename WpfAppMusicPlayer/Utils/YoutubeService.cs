using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using NReco.VideoConverter;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace WpfAppMusicPlayer.Utils
{
    public class YoutubeService
    {
        private readonly YoutubeClient _youtubeClient;

        public YoutubeService()
        {
            _youtubeClient = new YoutubeClient();
        }
        public async Task<SearchListResponse> SearchOnYouTube(string query)
        {
            // Khởi tạo dịch vụ YouTube
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyCfjW0PoXB0QgJIPcpnoPKHLZ9oQqwKlmE",
                ApplicationName = "My First Project"
            });

            // Tạo truy vấn tìm kiếm
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 25;

            // Gọi API và nhận kết quả
            SearchListResponse searchListResponse = await searchListRequest.ExecuteAsync();

            // Xử lý kết quả tìm kiếm
            return searchListResponse;
        }

        public async Task DownloadVideoAsMp3(string videoId, string videoName)
        {
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(videoId);

                var streamInfoSet = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                var muxedStreams = streamInfoSet.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();
                if (muxedStreams == null)
                {
                    throw new Exception("Không thể tìm thấy stream audio.");
                }

                var fileName = $"{video.Title}.mp3";
                var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), fileName);
                await _youtubeClient.Videos.Streams.DownloadAsync(muxedStreams.First(), outputPath);

                var tempOutputPath = Path.GetTempFileName();

                var converter = new FFMpegConverter();
                converter.ConvertMedia(outputPath, tempOutputPath, "mp3");

                File.Delete(outputPath);

                File.Move(tempOutputPath, outputPath);
                TagLib.File file = TagLib.File.Create(outputPath);
                file.Tag.Title = videoName;
                file.Save();
                MessageBoxResult result = MessageBox.Show("Video đã được tải xuống và chuyển đổi thành công thành mp3.\nBạn muốn mở thư mục?", "Thông báo", MessageBoxButton.YesNo);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                        break;
                    case MessageBoxResult.No:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải xuống và chuyển đổi video: {ex.Message}");
            }
        }
    }
}
