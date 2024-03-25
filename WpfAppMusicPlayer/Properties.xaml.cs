using Google.Protobuf;
using System;
using System.Collections.Generic;
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

namespace WpfAppMusicPlayer
{
    /// <summary>
    /// Interaction logic for Properties.xaml
    /// </summary>
    public partial class Properties : Window
    {
        public Properties(BitmapImage? image, string message)
        {
            InitializeComponent();
            DataContext = new PropertiesViewModel(image, message);
        }

        public class PropertiesViewModel
        {
            public BitmapImage? Image { get; }
            public string Message { get; }

            public PropertiesViewModel(BitmapImage? image, string message)
            {
                Image = image;
                Message = message;
            }
        }
    }
}
