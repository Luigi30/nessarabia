using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nessarabia.gfx
{
    class PatternTable
    {
        //ROM

        byte[] rawData = new byte[4096];
        public List<Tile> Tilemap;

        public PatternTable(byte[] ppuRam, int offset)
        {
            rawData = new byte[4096];
            ppuRam.Skip(offset).Take(4096).ToArray().CopyTo(rawData, 0);
            Tilemap = MakeTilemap(rawData);
        }

        private List<Tile> MakeTilemap(byte[] data)
        {
            var tilemap = new List<Tile>();
            for(int i=0; i < 256; i++)
            {
                tilemap.Add(new Tile(data.Skip(i * 16).Take(16).ToArray()));
            }
            return tilemap;
        }
    }
}
