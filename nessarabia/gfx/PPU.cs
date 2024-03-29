﻿using nessarabia.gfx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static System.Windows.Media.Imaging.WriteableBitmapExtensions;

namespace nessarabia.gfx
{
    public class OAMDMAEventArgs : EventArgs
    {
        private readonly int _pageToTransfer;

        public OAMDMAEventArgs(int pageToTransfer)
        {
            _pageToTransfer = pageToTransfer;
        }

        public int PageToTransfer
        {
            get { return _pageToTransfer; }
        }
    }

    public class UpdateDisplayEventArgs : EventArgs
    {
        private readonly int _pixelClocks;

        public UpdateDisplayEventArgs(int pixelClocks)
        {
            _pixelClocks = pixelClocks;
        }

        public int PixelClocks
        {
            get { return _pixelClocks; }
        }
    }

    public partial class PPU
    {
        private readonly SynchronizationContext _syncContext;

        /* 
        http://www.fceux.com/web/help/fceux.html?PPU.html

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

        1 CPU clock     = 3 PPU clocks
        1 PPU clock     = 1 pixel
        341 PPU clocks  = 1 scanline
        262 scanlines   = 1 frame
        1 frame         = 89342 PPU clocks
        */

        const int CLOCKS_PER_SCANLINE = 341;

        public byte[] RAM = new byte[16384];
        public byte[] OAM = new byte[256]; //object attribute memory

        public PPUDisplay PpuDisplay
        {
            get { return _ppuDisplay; }
            set
            {
                if (value != _ppuDisplay)
                {
                    _ppuDisplay = value;
                    OnPropertyChanged("PpuDisplay");
                }
            }
        }
        private PPUDisplay _ppuDisplay;

        private WriteableBitmap displayBuffer;

        List<Color> Palette { get; }

        PatternTable pt0;
        PatternTable pt1;

        int cycles;
        bool oddFrame; //pre-render line is one dot shorter on odd frame counts

        public event OAMDMATransferRequestedHandler OAMDMATransferRequested;
        public delegate void OAMDMATransferRequestedHandler(object sender, OAMDMAEventArgs e);

        public event PpuRegistersChangedHandler PpuRegistersChanged;
        public delegate void PpuRegistersChangedHandler(object sender, EventArgs e);

        #region registers
        byte _ppuCtrl;      //$2000
        byte _ppuMask;      //$2001
        byte _ppuStatus;    //$2002
        byte _oamAddr;      //$2003
        byte _oamData;      //$2004
        byte _ppuScroll;    //$2005
        byte _ppuAddr;      //$2006
        byte _ppuData;      //$2007
        byte _oamDma;       //$4014

        byte tempPpuAddrHi;
        byte tempPpuAddrLo;
        ushort ppuAddress;

        public byte PPUCTRL { set
            {
                if (value != _ppuCtrl)
                {
                    _ppuCtrl = value;
                    OnPropertyChanged("PPUCTRL");
                }
            }
        }

        public byte PPUMASK
        {
            set
            {
                if (value != _ppuMask)
                {
                    _ppuMask = value;
                    OnPropertyChanged("PPUMASK");
                }
            }
        }

        public byte PPUSTATUS
        {
            get
            {
                return _ppuStatus;
            }
        }

        public byte OAMADDR
        {
            set
            {
                if (value != _oamAddr)
                {
                    _oamAddr = value;
                    OnPropertyChanged("OAMADDR");
                }
            }
        }

        public byte OAMDATA
        {
            get
            {
                return _oamData;
            }
            set
            {
                if(value != _oamData)
                {
                    _oamData = value;
                    OnPropertyChanged("OAMDATA");
                }
            }
        }

        public byte PPUSCROLL
        {
            set
            {
                if(value != _ppuScroll)
                {
                    _ppuScroll = value;
                    OnPropertyChanged("PPUSCROLL");
                }
            }
        }

        public byte PPUADDR
        {
            set
            {
                if(_ppuAddr != value)
                {
                    _ppuAddr = value;
                    if (tempPpuAddrLo == 0x00)
                    {
                        tempPpuAddrLo = value;
                        ppuAddress = (ushort)(tempPpuAddrHi << 8);
                        ppuAddress += tempPpuAddrLo;

                        tempPpuAddrHi = 0x00;
                        tempPpuAddrLo = 0x00;
                    }
                    else
                    {
                        tempPpuAddrHi = value;
                    }
                }
            }
        }

        public byte PPUDATA
        {
            get
            {
                byte data = RAM[_ppuAddr];
                if ((_ppuCtrl & 0x02) == 0x02)
                {
                    _ppuAddr += 32;
                }
                else
                {
                    _ppuAddr += 1;
                }
                return data;
            }
            set
            {
                if(_ppuData != value)
                {
                    _ppuData = value;
                    RAM[_ppuAddr] = value;
                    if ((_ppuCtrl & 0x02) == 0x02)
                    {
                        _ppuAddr += 32;
                    }
                    else
                    {
                        _ppuAddr += 1;
                    }
                }
            }
        }

        public byte OAMDMA
        {
            get
            {
                return _oamDma;
            }
            set
            {
                if (value != _oamDma)
                {
                    _oamDma = value;
                    OAMDMATransferRequested(this, new OAMDMAEventArgs(_oamDma));
                }
            }
        }
        #endregion

        public PPU()
        {
            _syncContext = SynchronizationContext.Current;

            PpuDisplay = new PPUDisplay();
            Palette = CreatePalette();

            RAM[0x3F00] = 0x22; //set the universal background color for testing
            RAM[0x3F01] = 0x16;
            RAM[0x3F02] = 0x27;
            RAM[0x3F03] = 0x18;

            for(int i = 0; i < 30; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    RAM[0x2000 + (i * 32) + j] = (byte)i;
                }

            }

        }

        public void onNewFrame(object sender, EventArgs e)
        {
            if(displayBuffer != null)
            {
                WriteableBitmap image = displayBuffer.Crop(0, 0, 256, 240).Flip(FlipMode.Vertical);
                byte[] buffer = new byte[256 * 240 * 4];
                int stride = image.PixelWidth * (image.Format.BitsPerPixel / 8);
                image.CopyPixels(new Int32Rect(0, 0, 256, 240), buffer, stride, 0);
                _syncContext.Post(o => PpuDisplay.DisplayCanvas.WritePixels(new Int32Rect(0, 0, 256, 240), buffer, stride, 0), null);
            }

            displayBuffer = new WriteableBitmap(340, 262, 96, 96, PixelFormats.Bgr32, null);
            displayBuffer.Clear(Colors.Black);
        }

        public void UpdateDisplay(object sender, UpdateDisplayEventArgs e)
        {
            /* Set up the various pointers we need */
            ushort nameTableAddress = (ushort)(0x2000 + (0x0400 * (_ppuCtrl & 0x01) + (_ppuCtrl & 0x02)));
            ushort backgroundTableAddress;
            ushort spriteTableAddress;

            PatternTable bgPattern;
            PatternTable spritePattern;

            if ((_ppuCtrl & 0x10) == 0x10)
            {
                backgroundTableAddress = 0x1000;
                bgPattern = pt1;
            }
            else
            {
                backgroundTableAddress = 0x0000;
                bgPattern = pt0;
            }

            if ((_ppuCtrl & 0x08) == 0x08)
            {
                spriteTableAddress = 0x1000;
                spritePattern = pt1;
            }
            else
            {
                spriteTableAddress = 0x0000;
                spritePattern = pt0;
            }

            for (int i = 0; i < e.PixelClocks; i++)
            {
                int scanline = (cycles / CLOCKS_PER_SCANLINE); //TODO: dummy scanline -1
                int pixelPosition = (cycles % CLOCKS_PER_SCANLINE);

                //Draw the background if enabled.
                //if ((_ppuMask & 0x08) == 0x08)
                //{
                if(scanline < 240)
                {
                    if (pixelPosition > 3 && pixelPosition < 260)
                    {
                        int adjustedPixelPosition = pixelPosition - 4; //subtract 4 to give us the corrected relative X coordinate
                        int tilemapXCoord = (adjustedPixelPosition / 8); //X coordinate of the tilemap
                        int tilemapYCoord = (scanline / 8);
                        //int tileYCoord = (scanline % 8); //how many lines into the tile we are
                        //int tileNumber = RAM[0x2000 + (tilemapYCoord * 32)];
                        var tile = bgPattern.Tilemap[RAM[nameTableAddress + (tilemapYCoord * 32)]]; //get the tile
                        //var tileCoordOffset = (adjustedPixelPosition % 8) + ((scanline % 8) * 8); //get the pixel we need
                        var colorIndex = tile.ColorIndexes[(adjustedPixelPosition % 8) + ((scanline % 8) * 8)]; //get its color index
                        //var paletteColor = RAM[0x3F00 + colorIndex]; //what color does that signify?
                        //var pixelColor = Palette[RAM[0x3F00 + colorIndex]]; //get the RGB value
                        displayBuffer.SetPixel(adjustedPixelPosition, scanline, Palette[RAM[0x3F00 + colorIndex]]); //and write it to the buffer
                    }
                }
                   
                //} 
                
                //do nothing on scanline 240 
                
                if(scanline == 241)
                {
                    if(pixelPosition == 1)
                    {
                        _ppuStatus = (byte)(_ppuStatus | 0x80);
                    }
                }              

                cycles++; //cycles 256-341 don't draw graphics

                if (cycles == 89342)
                {
                    cycles = 0;
                    //pull latest PPU registers and update
                    
                }
            }
        }

        public delegate void UpdatePpuRegistersFromMemoryCallback();
        public void UpdatePpuRegistersFromMemory()
        {
            UpdatePpuRegistersFromMemory(null, null);
        }

        public void UpdatePpuRegistersFromMemory(object sender, EventArgs e)
        {
            byte[] ppuRegisters = GetPpuRegistersFromMemory(e);
            PPUCTRL = ppuRegisters[0];
            PPUMASK = ppuRegisters[1];
            //PPUSTATUS is read-only
            OAMADDR = ppuRegisters[3];
            OAMDATA = ppuRegisters[4];
            PPUSCROLL = ppuRegisters[5];
            PPUADDR = ppuRegisters[6];
            PPUDATA = ppuRegisters[7];
        }

        public byte[] GetPpuRegistersFromMemory(EventArgs e)
        {
            //TODO: if e != null, check for updated register
            var memPtr = Interop.getMemoryRange(0x2000, 0x08);
            byte[] ppuRegisters = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                ppuRegisters[i] = Marshal.ReadByte(memPtr, i);
            }
            Interop.freeBuffer(memPtr);
            return ppuRegisters;
        }

        public void UpdatePatternTables()
        {
            pt0 = new PatternTable(RAM, 0x0000);
            pt1 = new PatternTable(RAM, 0x1000);

            //DumpTilemap();
        }

        public void DumpTilemap()
        {
            for(int i = 0; i < 16; i++)
            {
                for(int j = 0; j < 16; j++)
                {
                    PlaceTile(pt0.Tilemap[(i * 16) + j], (j * 8), (i * 8));
                }
            }
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

        public WriteableBitmap RenderTile(Tile tile, byte[] palette)
        {
            var tileData = new byte[tile.ColorIndexes.ToArray().Length * 4];
            var tileBitmap = new WriteableBitmap(8, 8, 96, 96, PixelFormats.Bgr32, null);

            for (int i = 0; i < tile.ColorIndexes.ToArray().Length; i++)
            {
                tileData[(i * 4) + 0] = Palette[palette[tile.ColorIndexes[i]]].B;
                tileData[(i * 4) + 1] = Palette[palette[tile.ColorIndexes[i]]].G;
                tileData[(i * 4) + 2] = Palette[palette[tile.ColorIndexes[i]]].R;
                tileData[(i * 4) + 3] = 0x00;
            }

            int stride = 8 * (PpuDisplay.DisplayCanvas.Format.BitsPerPixel / 8);
            tileBitmap.WritePixels(new Int32Rect(0, 0, 8, 8), tileData, stride, 0, 0);
            return tileBitmap.Flip(FlipMode.Vertical);
        }

        public void PlaceTile(Tile tile, int x, int y)
        {
            var palette = new byte[4];
            RAM.Skip(0x3F00).Take(4).ToArray().CopyTo(palette, 0);
            var image = RenderTile(tile, palette);

            int stride = 8 * (PpuDisplay.DisplayCanvas.Format.BitsPerPixel / 8);
            byte[] data = new byte[stride * 8];
            image.CopyPixels(data, stride, 0);
            PpuDisplay.DisplayCanvas.WritePixels(new Int32Rect(0, 0, 8, 8), data, stride, x, y);
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