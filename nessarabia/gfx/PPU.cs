using nessarabia.gfx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace nessarabia
{
    public class PPU
    {
        /* 
            Memory Map

            $0000-$0FFF = Pattern Table 0
            $1000-$1FFF = Pattern Table 1
            $2000-$23FF = Nametable 0
            $2400-$27FF = Nametable 1
            $2800-$2BFF = Nametable 2
            $2C00-$2FFF = Nametable 3
            $3000-$3EFF = mirror of $2000-$2EFF
            $3F00-$3F1F = Palette RAM
            $3F20-$3FFF = mirror of $3F00-$3F1F 
        */
        public PPU()
        {
            Display = new PPUDisplay();
            Palette = CreatePalette();

            OAM[0] = 0x22; //set the universal background color for testing
            
        }

        List<Color> Palette { get; }
        public byte[] RAM = new byte[16384];
        public byte[] OAM = new byte[256]; //object attribute memory

        PPUDisplay Display;

        PatternTable pt0;
        PatternTable pt1;

        public void UpdatePatternTables()
        {
            pt0 = new PatternTable(RAM, 0x0000);
            pt1 = new PatternTable(RAM, 0x1000);
        }

        List<Color> CreatePalette()
        {
            List<Color> paletteColors = new List<Color>();
            List<string> Palette = new List<String>() { "#7C7C7C", "#0000FC", "#0000BC", "#4428BC", "#940084", "#A80020", "#A81000", "#881400", "#503000", "#007800", "#006800", "#005800", "#004058", "#000000", "#000000", "#000000", "#BCBCBC", "#0078F8", "#0058F8", "#6844FC", "#D800CC", "#E40058", "#F83800", "#E45C10", "#AC7C00", "#00B800", "#00A800", "#00A844", "#008888", "#000000", "#000000", "#000000", "#F8F8F8", "#3CBCFC", "#6888FC", "#9878F8", "#F878F8", "#F85898", "#F87858", "#FCA044", "#F8B800", "#B8F818", "#58D854", "#58F898", "#00E8D8", "#787878", "#000000", "#000000", "#FCFCFC", "#A4E4FC", "#B8B8F8", "#D8B8F8", "#F8B8F8", "#F8A4C0", "#F0D0B0", "#FCE0A8", "#F8D878", "#D8F878", "#B8F8B8", "#B8F8D8", "#00FCFC", "#F8D8F8", "#000000", "#000000" };

            foreach (var color in Palette)
            {
                paletteColors.Add((Color)ColorConverter.ConvertFromString(color));
            }
            return paletteColors;
        }

    }
}
