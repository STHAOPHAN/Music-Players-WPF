using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppMusicPlayer.Models
{
    public class SongInfo
    {
        public string FilePath { get; set; }
        public string SongName { get; set; }
        public string Singer { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Album { get; set; }
    }
}
