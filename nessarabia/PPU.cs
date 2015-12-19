using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nessarabia
{
    class PPU
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
            $3F20-$3F3F = mirror of $3F00-$3F1F 
        */

        public byte[] RAM = new byte[8192];
        public byte[] OAM = new byte[256]; //object attribute memory
    }
}
