using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppMusicPlayer.Utils
{
    public class SpeechToTextHelper
    {
        private readonly SpeechClient _speechClient;

        public SpeechToTextHelper()
        {
            _speechClient = SpeechClient.Create();
        }

        public async Task<List<Tuple<double, string>>> RecognizeSpeechFromStorageAsync(string storageUri)
        {
            try
            {
                var audio = RecognitionAudio.FromStorageUri(storageUri);
                var config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Mp3,
                    SampleRateHertz = 16000,
                    LanguageCode = LanguageCodes.Vietnamese.Vietnam,
                    EnableWordTimeOffsets = true,
                    EnableAutomaticPunctuation = true
                };

                var operation = await _speechClient.LongRunningRecognizeAsync(config, audio);
                var response = await operation.PollUntilCompletedAsync();

                var result = new List<Tuple<double, string>>();
                foreach (var result1 in response.Result.Results)
                {
                    foreach (var alternative in result1.Alternatives)
                    {
                        var words = alternative.Words;
                        var startTime = words.First().StartTime.Nanos * 1e-9;
                        var transcript = string.Join(" ", words.Select(word => word.Word));
                        result.Add(new Tuple<double, string>(startTime, transcript));
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recognizing speech from Cloud Storage: {ex.Message}");
                return null;
            }
        }

        public async Task<string> RecognizeSpeechFromAudioStreamAsync(Stream audioStream)
        {
            try
            {
                // Khởi tạo client của Google Cloud Speech-to-Text API
                var speechClient = SpeechClient.Create();

                // Khởi tạo recognition config cho tiếng Việt
                var config = new RecognitionConfig
                {
                    Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                    SampleRateHertz = 16000,
                    LanguageCode = "vi-VN",
                    EnableWordTimeOffsets = true,
                    EnableAutomaticPunctuation = true
                };

                // Khởi tạo recognition audio từ stream audio
                var audio = RecognitionAudio.FromStream(audioStream);

                // Thực hiện nhận dạng giọng nói từ audio bằng Google Cloud Speech-to-Text API
                var response = await speechClient.RecognizeAsync(config, audio);

                // Lấy kết quả nhận dạng và trả về văn bản
                string transcript = "";
                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        transcript += $"{alternative.Transcript}\n";
                    }
                }
                return transcript;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recognizing speech: {ex.Message}");
                return null;
            }
        }
    }
}
