using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nessarabia.gfx
{
    public class Tile
    {
        /*
        A pixel can be colors 0 through 3.
        Each byte of tile data is divided into 2 nybbles of color data.
        */ 
        byte[] rawData = new byte[16]; //16 bytes of raw data, two 8x8 planes
        List<int> ColorIndexes = new List<int>();

        public Tile(byte[] data)
        {
            data.CopyTo(rawData, 0);
            BitArray colors = new BitArray(rawData);

            //First 8 bytes
            for(int i=0; i < (colors.Length / 2); i++)
            {
                if (colors[i])
                {
                    ColorIndexes.Add(1); 
                } else
                {
                    ColorIndexes.Add(0);
                }
            }
            
            //Second 8 bytes
            for(int i = (colors.Length / 2); i < colors.Length; i++)
            {
                if (colors[i])
                {
                    ColorIndexes[i - colors.Length / 2] += 2;
                }
            }
        }
    }
}
