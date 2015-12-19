using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nessarabia
{
    public struct iNesHeader
    {
        //http://wiki.nesdev.com/w/index.php/INES

        public byte[] MagicString { get; }
        public byte PrgRomSize { get; } //16kb increments
        public byte ChrRomSize { get; } //8kb increments. if 0, CHR RAM instead.
        public byte Flags6 { get; }
        public byte Flags7 { get; }
        public byte PrgRamSize { get; } //8kb increments, 0 or 1 = 8KB
        public byte Flags9 { get; }
        public byte Flags10 { get; }

        public iNesHeader(FileStream rom)
        {
            var rawHeader = new byte[16];
            rom.Read(rawHeader, 0, 16);

            MagicString = new byte[4];
            MagicString[0] = rawHeader[0];
            MagicString[1] = rawHeader[1];
            MagicString[2] = rawHeader[2];
            MagicString[3] = rawHeader[3];

            PrgRomSize = rawHeader[4];
            ChrRomSize = rawHeader[5];
            Flags6 = rawHeader[6];
            Flags7 = rawHeader[7];
            PrgRamSize = rawHeader[8];
            Flags9 = rawHeader[9];
            Flags10 = rawHeader[10];
        }
    }

    class iNesRom
    {
        public byte[] PrgRom;
        public byte[] ChrRom;
        public byte[] PrgRam;

        public iNesRom(string path)
        {
            var rom = File.Open(path, FileMode.Open);
            var header = new iNesHeader(rom);

            PrgRom = new byte[header.PrgRomSize * 16384];
            ChrRom = new byte[header.ChrRomSize * 8192];

            if ((header.Flags6 & 0x04) == 0x04)
            {
                rom.Read(PrgRom, (16 + 512), (header.PrgRomSize * 16384));
                rom.Read(ChrRom, (16 + 512 + (header.PrgRomSize * 16384)), (header.ChrRomSize * 8192));
            }
            else
            {
                rom.Seek(16, SeekOrigin.Begin); //16-byte header...
                rom.Read(PrgRom, 0, (header.PrgRomSize * 16384)); //PRG ROM...
                rom.Read(ChrRom, 0, (header.ChrRomSize * 8192)); //...then CHR ROM.
            }
        }

    }
}
