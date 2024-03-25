using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace WpfAppMusicPlayer.Models
{
    public class SongInfo
    {
        public string FilePath { get; set; }
        public string? SongName { get; set; }
        public string? SingerName { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Album { get; set; }
        public string? ImgSinger { get; set; }
        public string? Genres { get; set; }
        public IPicture? Picture { get; set; }
    }
}
