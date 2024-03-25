using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;


namespace WpfAppMusicPlayer
{
    /// <summary>
    /// Interaction logic for KaraOke.xaml
    /// </summary>
    public partial class KaraOkeWindow : Window
    {
        private WaveIn waveIn;
        private WaveFileWriter writer;
        private DispatcherTimer timer;
        private TimeSpan currentTime;
        private MediaPlayer mediaPlayer;
        string beatFilePath;

        public KaraOkeWindow()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void ChooseBeatButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                beatFilePath = openFileDialog.FileName;
                tbBeatName.Text = Path.GetFileName(openFileDialog.FileName);
            }
        }
        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            mediaPlayer = new MediaPlayer();
            StopRecordingButton.IsEnabled = false;
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            currentTime = currentTime.Add(TimeSpan.FromSeconds(1));
            TimerTextBlock.Text = currentTime.ToString(@"hh\:mm\:ss");
        }

        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(beatFilePath))
            {
                System.Windows.Forms.MessageBox.Show("Please select a beat file before recording!", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (File.Exists(beatFilePath))
            {
                mediaPlayer.Open(new Uri(beatFilePath));
                mediaPlayer.Play();
            }

            StopRecordingButton.IsEnabled = true;
            currentTime = TimeSpan.Zero;
            timer.Start();
            waveIn = new WaveIn();
            waveIn.WaveFormat = new WaveFormat(44100, 1);
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();
        }

        private void StopRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            mediaPlayer.Stop();
            timer.Stop();
            TimerTextBlock.Text = "00:00:00";
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string outputFolderPath = folderBrowserDialog.SelectedPath;

                    string fileName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên tệp", "Tên tệp", "recorded_audio");
                    if (string.IsNullOrEmpty(fileName))
                    {
                        return;
                    }

                    string outputPath = Path.Combine(outputFolderPath, fileName + ".wav");

                    if (writer != null)
                    {
                        writer.Close();
                        writer.Dispose();
                        writer = null;
                    }

                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                    File.Create(outputPath).Close();
                    FileStream fs = File.Open("temp.wav", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    fs.Close();
                    MergeAudioFiles(beatFilePath, "temp.wav", outputPath);
                    StopRecordingButton.IsEnabled = false;
                    //File.Move("temp.wav", outputPath);
                }
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (writer == null)
            {
                writer = new WaveFileWriter("temp.wav", waveIn.WaveFormat);
            }

            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);

                // Adjust sample amplitude (volume)
                sample = (short)(sample * 5);
                e.Buffer[i] = (byte)(sample & 0xFF);
                e.Buffer[i + 1] = (byte)((sample >> 8) & 0xFF);
            }

            writer.Write(e.Buffer, 0, e.BytesRecorded);
            writer.Flush();
        }
        private void MergeAudioFiles(string sourceFile1, string sourceFile2, string outputFile)
        {
            // Chuyển đổi tệp MP3 sang WAV trực tiếp từ sourceFile1
            string tempWavFilePath = Path.ChangeExtension(sourceFile1, ".wav");
            ConvertMp3ToWav(sourceFile1, tempWavFilePath);

            using (var reader1 = new AudioFileReader(tempWavFilePath))
            using (var reader2 = new AudioFileReader(sourceFile2))
            {
                var mixer = new MixingSampleProvider(new[] { reader1.ToSampleProvider(), reader2.ToSampleProvider() });
                WaveFileWriter.CreateWaveFile16(outputFile, mixer);
            }

            // Xóa tệp WAV tạm thời
            File.Delete(tempWavFilePath);
        }
        private void ConvertMp3ToWav(string mp3FilePath, string wavFilePath)
        {
            using (var reader = new AudioFileReader(mp3FilePath))
            {
                // Sử dụng định dạng WaveFormat cố định, chẳng hạn như 44100 Hz, 16-bit, mono
                var targetWaveFormat = new WaveFormat(44100, 16, 1);

                // Chuyển đổi định dạng của đối tượng AudioFileReader
                using (var resampler = new MediaFoundationResampler(reader, targetWaveFormat))
                {
                    WaveFileWriter.CreateWaveFile(wavFilePath, resampler);
                }
            }
        }
    }
}
