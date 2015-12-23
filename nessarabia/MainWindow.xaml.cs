using nessarabia.cpu;
using nessarabia.gfx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        Thread M6502WorkerThread;
        MainWindowViewModel vm = new MainWindowViewModel();

        public MainWindow()
        {
            //Set up background thread
            M6502WorkerThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                vm.Processor.Run();
            });

            //Set up viewmodel
            vm.Processor = new M6502();
            vm.Ppu = new PPU();
            vm.DisassembledOpcodes = new ObservableCollection<Disassembly.DisassembledOpcode>();

            //Set up events
            vm.Processor.ProcessorStepCompleted += new M6502.ProcessorStepCompletedEventHandler(AfterProcessorStepCompleted);
            vm.Processor.UpdateDisplay += new M6502.UpdateDisplayEventHandler(vm.Ppu.UpdateDisplay);
            vm.Processor.ExecutionStopped += new M6502.ExecutionStoppedEventHandler(onExecutionStopped);
            vm.Ppu.OAMDMATransferRequested += new PPU.OAMDMATransferRequestedHandler(PerformOAMDMATransfer);
            vm.Processor.NewFrameHasBegun += new M6502.NewFrameHasBegunHandler(vm.Ppu.onNewFrame);
            TextCompositionManager.AddTextInputHandler(this, new TextCompositionEventHandler(OnTextComposition));


            InitializeComponent();
            binaryLoadedStatus.SetBinding(ContentProperty, new Binding("LoadSuccess"));
            DataContext = vm;
        }

        private void disableReadoutControls()
        {
            BindingOperations.ClearBinding(txtAccumulator, TextBox.TextProperty);
            BindingOperations.ClearBinding(txtIndexX, TextBox.TextProperty);
            BindingOperations.ClearBinding(txtIndexY, TextBox.TextProperty);
            BindingOperations.ClearBinding(txtFlags, TextBox.TextProperty);
            BindingOperations.ClearBinding(txtProgramCounter, TextBox.TextProperty);
            BindingOperations.ClearBinding(txtStackPointer, TextBox.TextProperty);
            BindingOperations.ClearBinding(lbDisassembly, ListBox.ItemsSourceProperty);
        }

        private void enableReadoutControls()
        {
            BindingOperations.SetBinding(txtAccumulator, TextBox.TextProperty, new Binding("Processor.Accumulator") { StringFormat = "{0:X2}" });
            BindingOperations.SetBinding(txtIndexX, TextBox.TextProperty, new Binding("Processor.IndexX") { StringFormat = "{0:X2}" });
            BindingOperations.SetBinding(txtIndexY, TextBox.TextProperty, new Binding("Processor.IndexY") { StringFormat = "{0:X2}" });
            BindingOperations.SetBinding(txtFlags, TextBox.TextProperty, new Binding("Processor.Flags"));
            BindingOperations.SetBinding(txtProgramCounter, TextBox.TextProperty, new Binding("Processor.ProgramCounter") { StringFormat = "{0:X4}" });
            BindingOperations.SetBinding(txtStackPointer, TextBox.TextProperty, new Binding("Processor.StackPointer") { StringFormat = "{0:X2}" });
            BindingOperations.SetBinding(lbDisassembly, ListBox.ItemsSourceProperty, new Binding("DisassembledOpcodes"));
        }

        private void btnLoadBinary_Click(object sender, RoutedEventArgs e)
        {
            //Interop.loadBinary(monitorRomPath, 0xFF00);
            //Interop.loadBinary(basicRomPath, 0xE000);
            //videoRom = File.ReadAllBytes(characterRomPath);
            //decodeGraphics();

            var rom = new iNesRom(@"C:\Users\Luigi\Documents\Visual Studio 2015\Projects\nessarabia\Super Mario Bros. (W) [!].nes");
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(rom.PrgRom.Length);
            Marshal.Copy(rom.PrgRom, 0, unmanagedPointer, rom.PrgRom.Length);
            Interop.loadBinaryData(unmanagedPointer, rom.PrgRom.Length, 0x8000);
            Marshal.FreeHGlobal(unmanagedPointer);

            rom.ChrRom.CopyTo(vm.Ppu.RAM, 0);
            vm.Ppu.UpdatePatternTables();

            Interop.resetProcessor();
            vm.Processor.UpdateProperties(Interop.getProcessorStatus());

            ushort length = 0xFFFE;
            IntPtr memoryValuesPtr = Interop.getMemoryRange(0x0000, length);
            byte[] result = new byte[length + 1];
            Marshal.Copy(memoryValuesPtr, result, 0, length);

            Disassembly disassembly = new Disassembly(result);
            disassembly.Begin(0xFF00);

            while (disassembly.NextInstructionAddress < 0xFFFE)
            {
                vm.DisassembledOpcodes.Add(disassembly.ToDisassembledOpcode());
                disassembly.Next();
            }

            btnRun.IsEnabled = true;
            btnSingleStep.IsEnabled = true;

        }

        private void UpdateDisassemblySelection(ushort address)
        {
            //update disassembly
            for (int i = 0; i < lbDisassembly.Items.Count; i++)
            {
                var item = (Disassembly.DisassembledOpcode)lbDisassembly.Items[i];
                if (address == item.Address)
                {
                    lbDisassembly.Dispatcher.Invoke(new Action(() => { lbDisassembly.SelectedIndex = i; }));
                }
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            btnLoadBinary.IsEnabled = false;
            btnRun.IsEnabled = false;
            btnSingleStep.IsEnabled = false;
            tbDebugConsole.IsEnabled = false;
            disableReadoutControls();
            M6502WorkerThread.Start();
        }

        private void btnBreak_Click(object sender, RoutedEventArgs e)
        {
            enableReadoutControls();
            btnRun.IsEnabled = true;
            btnSingleStep.IsEnabled = true;
        }

        public void PerformOAMDMATransfer(object sender, OAMDMAEventArgs e)
        {
            ushort baseAddress = (ushort)(e.PageToTransfer * 100);
            IntPtr memory = Interop.getMemoryRange(baseAddress, 0xFF);
            for (int i = 0; i < 256; i++)
            {
                vm.Ppu.RAM[baseAddress + i] = Marshal.ReadByte(memory, i);
            }
            if(vm.Processor.Cycles % 2 == 1)
            {
                vm.Processor.Cycles += 513;
            } else
            {
                vm.Processor.Cycles += 512;
            }
        }

        private void tbDebugEntry_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tbDebugConsole.Text += ">" + tbDebugEntry.Text + "\r\n";
                Debug.WriteLine(tbDebugEntry.Text);
                var split = tbDebugEntry.Text.Split();
                if (split[0].Equals("bpset"))
                {
                    var address = ushort.Parse(split[1], System.Globalization.NumberStyles.HexNumber);
                    vm.Processor.breakpointAddresses.Add(address);
                    tbDebugConsole.Text += String.Format("Breakpoint added: ${0}\r\n", address.ToString("X4"));

                }
                else if (split[0].Equals("bpunset"))
                {
                    var address = ushort.Parse(split[1], System.Globalization.NumberStyles.HexNumber);
                    if (vm.Processor.breakpointAddresses.Contains(address))
                    {
                        vm.Processor.breakpointAddresses.Remove(address);
                        tbDebugConsole.Text += String.Format("Breakpoint removed: ${0}\r\n", address.ToString("X4"));
                    }
                    else
                    {
                        tbDebugConsole.Text += String.Format("Breakpoint ${0} not found\r\n", address.ToString("X4"));
                    }
                }
                //TODO: bpdisable, bpenable
                tbDebugEntry.Clear();
            }
        }
    }
}
