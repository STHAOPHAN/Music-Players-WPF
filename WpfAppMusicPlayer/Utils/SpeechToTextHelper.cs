using Google.Cloud.Speech.V1;
using System;
using System.Collections.Generic;
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
    }
}
