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
using System.Windows.Navigation;
using System.Windows.Shapes;

/*
- http://web.textfiles.com/games/ppu.txt

- One scanline is exactly 1364 cycles long. In comparison to the CPU's speed, one scanline is 1364/12 CPU cycles long.

- One frame is exactly 357368 cycles long, or exactly 262 scanlines long.

*/

namespace nessarabia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
