using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppMusicPlayer.Models
{
    public class Album
    {
        public string Name { get; set; }
        public List<SongInfo> Songs { get; set; }
    }
}
