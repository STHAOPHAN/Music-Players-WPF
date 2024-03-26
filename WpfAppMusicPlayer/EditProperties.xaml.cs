using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TagLib;
using WpfAppMusicPlayer.Models;

namespace WpfAppMusicPlayer
{
    /// <summary>
    /// Interaction logic for EditProperties.xaml
    /// </summary>
    public partial class EditProperties : Window
    {
        private SongInfo songInfo;

        public EditProperties(SongInfo song)
        {
            InitializeComponent();
            songInfo = song;
            ShowSongInfo(songInfo);
        }

        private void ShowSongInfo(SongInfo songInfo)
        {
            txtTitle.Text = songInfo.SongName;
            txtArtist.Text = songInfo.SingerName;
            txtAlbum.Text = songInfo.Album;
            txtGenres.Text = songInfo.Genres;
            txtDuration.Content = $"{songInfo.Duration}";
            txtFilePath.Text = songInfo.FilePath;
            if (songInfo.Picture != null && songInfo.Picture.Data.Data != null)
            {
                using (MemoryStream memoryStream = new MemoryStream(songInfo.Picture.Data.Data))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();

                    imgPicture.Source = bitmapImage;
                }
            }
            else
            {
                imgPicture.Source = new BitmapImage(new Uri("../../../../WpfAppMusicPlayer/Images/NoImageAvailable.jpg", UriKind.RelativeOrAbsolute));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            TagLib.File file = TagLib.File.Create(songInfo.FilePath);
            file.Tag.Title = txtTitle.Text;
            file.Tag.Artists = new string[1] {txtArtist.Text};
            file.Tag.Album = txtAlbum.Text;
            string[] genres = txtGenres.Text.Split(',') ;
            file.Tag.Genres = genres;
            if (imgPicture.Source != null)
            {
                BitmapSource bitmapSource = (BitmapSource)imgPicture.Source;

                byte[] imageBytes;
                using (MemoryStream stream = new MemoryStream())
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);
                    imageBytes = stream.ToArray();
                }
                Picture picture = new Picture(imageBytes);
                file.Tag.Pictures = new TagLib.IPicture[] { picture };
            }
            file.Save();
            this.Close();
        }

        private void btnChangeImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedImagePath = openFileDialog.FileName;
                imgPicture.Source = new BitmapImage(new Uri(selectedImagePath));
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
