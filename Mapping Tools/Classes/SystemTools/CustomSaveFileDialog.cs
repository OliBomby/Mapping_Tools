using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SystemTools
{
    public partial class CustomSaveFileDialog : UserControl
    {
        //https://stackoverflow.com/questions/9665579/setting-up-hook-on-windows-messages
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        const uint WINEVENT_OUTOFCONTEXT = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
           hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
           uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT rc);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hwndChild, IntPtr hwndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        private IntPtr hDlg;        // Save As dialog handle
        private IntPtr hHook;       // Event hook
        private IntPtr hCtrl;       // App. specific user control handle

        UserControl ctrl;           // App. specific user control

        //Static variable containing the instance object
        private static CustomSaveFileDialog customSaveFileDialog;
        private SaveFileDialog saveFileDialog1;

        //public property for the user
        //theSaveFileDialog has been added to the control in the designer from the Toolbox
        public SaveFileDialog Dlg { get { return saveFileDialog1; } }

        //Event hook delegate
        private static WinEventDelegate procDelegate = new WinEventDelegate(WinEventProc);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ctrl">The User Control to be displayed in the file dialog</param>
        public CustomSaveFileDialog(UserControl ctrl)
        {
            InitializeComponent();

            customSaveFileDialog = this;
            this.ctrl = ctrl;
            hCtrl = ctrl.Handle;

            //Setup Hook; for simplicity, hook all possible events from the current process
            hHook = SetWinEventHook(1, 0x7fffffff, IntPtr.Zero,
                    procDelegate, (uint)System.Diagnostics.Process.GetCurrentProcess().Id, 0, WINEVENT_OUTOFCONTEXT);
        }

        private void InitializeComponent()
        {
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Title = "Save As";
            this.saveFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog1_FileOk);
            // 
            // CustomSaveFileDialog
            // 
            this.Name = "CustomSaveFileDialog";
            this.ResumeLayout(false);

        }


        // Hook function
        static void WinEventProc(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            CustomSaveFileDialog csfdg = customSaveFileDialog;
            if (csfdg.hDlg == IntPtr.Zero)
                csfdg.hDlg = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "#32770", "Save As");

            if (hwnd == csfdg.hDlg)
            {
                IntPtr hParent = GetParent(csfdg.hCtrl);

                //this is done only once
                if (!(hParent == csfdg.hDlg))
                    SetParent(csfdg.hCtrl, csfdg.hDlg);   //Bind the user control to the Common Dialog

                RECT cliRect;
                GetClientRect(csfdg.hDlg, out cliRect);

                //Position the button in the file dialog
                MoveWindow(csfdg.hCtrl, cliRect.Left + 130, cliRect.Bottom - 55, 500, 60, true);
            }
        }

    }

}
