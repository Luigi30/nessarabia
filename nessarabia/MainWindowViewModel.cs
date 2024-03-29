﻿using nessarabia.gfx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace nessarabia
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private M6502 _processor;
        private PPU _ppu;
        private ObservableCollection<cpu.Disassembly.DisassembledOpcode> _disassembledOpcodes;

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public PPU Ppu
        {
            get { return _ppu; }
            set
            {
                if (value != _ppu)
                {
                    _ppu = value;
                    OnPropertyChanged("Ppu");
                }
            }
        }

        public ObservableCollection<cpu.Disassembly.DisassembledOpcode> DisassembledOpcodes
        {
            get { return _disassembledOpcodes; }
            set
            {
                if(value != _disassembledOpcodes)
                {
                    _disassembledOpcodes = value;
                    OnPropertyChanged("DisassembledOpcodes");
                }
            }
        }

        public M6502 Processor
        {
            get { return _processor; }
            set
            {
                if (value != _processor)
                {
                    _processor = value;
                    OnPropertyChanged("Processor");
                }
            }
        }
        #endregion
    }
}
