using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GTXVM;
using System.Timers;
using System.Windows.Threading;
using GTXVM.Support;
using ChronosLib.WPFUtils;
using System.Diagnostics;

namespace GTXVMTestUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        //GTXScriptHost host;
        GTXScript curScript;
        DispatcherTimer clock;
        bool executing = false;

        public bool LibLoaded { get; set; }

        public MainWindow () {
            InitializeComponent ();
            List<GTXLibrary> libs = new List<GTXLibrary> ();
            //host = new GTXScriptHost (libs, null);
            clock = new DispatcherTimer (TimeSpan.FromSeconds (1.0 / 16), DispatcherPriority.Normal, Clock_Tick, this.Dispatcher);
        }

        private void Script_OnStateChange (object sender, EventArgs e) {
            GTXScript sc = (GTXScript) sender;
            textBlockScriptState.Text = sc.State.ToString ();
            if (sc.State == GTXScriptState.Invalid || sc.State == GTXScriptState.Runaway ||
                sc.State == GTXScriptState.DivisionByZero || sc.State == GTXScriptState.ModulusByZero ||
                sc.State == GTXScriptState.Terminated)
                StopAll ();

            UpdateData ();
        }

        private const int bytesPerLine = 16;
        private void UpdateData () {
            string [] stackBytes;
            {
                var stk = curScript.Stack; // Get the stack
                stackBytes = Utils.ByteArrayToHex (stk.Pop ((uint) stk.Count).Reverse ().ToArray ()).Split (' ');
            }
            listBoxStack.Items.Clear ();
            for (int i = 0; i < stackBytes.Length; i++)
                listBoxStack.Items.Add (stackBytes [i]);

            string ramString = Utils.ByteArrayToHex (curScript.GetFromMemory (0, curScript.MemorySize));
            textBlockRAMContents.Text = ramString;
            //StringBuilder builder = new StringBuilder (3 * bytesPerLine);

            //for (int i = 0, j; i < ramString.Length; i += bytesPerLine)
                
        }

        private void ExecScript (bool singleOp) {
            //host.Run (singleOp);
            curScript.Run (singleOp);
            UpdateData ();
        }

        private void Clock_Tick (object sender, EventArgs e) {
            ExecScript (false);
        }

        private void buttonExec_Click (object sender, RoutedEventArgs e) {
            if (!executing) {
                clock.Start ();
                this.buttonExec.Content = "Pause";
                this.buttonExecSingle.IsEnabled = false;
                this.buttonExecNextTic.IsEnabled = false;
                executing = true;
            } else if (executing) {
                clock.Stop ();
                this.buttonExec.Content = "Run";
                this.buttonExecSingle.IsEnabled = true;
                this.buttonExecNextTic.IsEnabled = true;
                executing = false;
            }
        }

        private void buttonExecNextTic_Click (object sender, RoutedEventArgs e) {
            ExecScript (false);
        }

        private void buttonExecSingle_Click (object sender, RoutedEventArgs e) {
            ExecScript (true);
        }

        private void menuItemExit_Click (object sender, RoutedEventArgs e) { this.Close (); }

        private void menuItemOpenLib_Click (object sender, RoutedEventArgs e) {
            object result = Dialogs.ShowOpenFileDialog ("GTXVM Files|*.GTX", false, "*.GTX");
            if (result != null) {
                Debug.Assert (result.GetType () == typeof (string));

            }   
        }

        private void buttonRestart_Click (object sender, RoutedEventArgs e) {
            clock.Stop ();
            executing = false;
            //host.Reset ();
            curScript.Reset ();
            this.buttonExec.IsEnabled = true;
            this.buttonExecSingle.IsEnabled = true;
            this.buttonExecNextTic.IsEnabled = true;
        }

        private void StopAll () {
            clock.Stop ();
            executing = false;
            this.buttonExec.IsEnabled = false;
            this.buttonExecSingle.IsEnabled = false;
            this.buttonExecNextTic.IsEnabled = false;
        }

        private void menuItemRunScript_Click (object sender, RoutedEventArgs e) {

        }

        private void menuItemRunNamedScript_Click (object sender, RoutedEventArgs e) {

        }

        private void menuItemStopScript_Click (object sender, RoutedEventArgs e) {

        }

        private void menuItemSelectScript_Click (object sender, RoutedEventArgs e) {

        }

        private void menuItemStopAll_Click (object sender, RoutedEventArgs e) {

        }
    }
}
