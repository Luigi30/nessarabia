using nessarabia.gfx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace nessarabia
{
    public class PPUDisplay : INotifyPropertyChanged
    {
        public PPUDisplay()
        {
            DisplayCanvas = new WriteableBitmap(256, 240, 96, 96, PixelFormats.Bgr32, null);
            DisplayCanvas.Clear(Colors.Black);
        }

        public WriteableBitmap _displayCanvas;
        public WriteableBitmap DisplayCanvas
        {
            get { return _displayCanvas; }
            set
            {
                if (value != _displayCanvas)
                {
                    _displayCanvas = value;
                    OnPropertyChanged("DisplayCanvas");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

}
