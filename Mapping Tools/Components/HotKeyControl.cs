using System;
using System.Drawing;
using System.Windows.Forms;

public class HotKeyControl : Control {
    public HotKeyControl() {
        this.SetStyle(ControlStyles.UserPaint, false);
        this.SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
        this.BackColor = Color.FromKnownColor(KnownColor.Window);
    }

    public Keys HotKey {
        get {
            if (this.IsHandleCreated) {
                var key = (uint)SendMessage(this.Handle, 0x402, IntPtr.Zero, IntPtr.Zero);
                hotKey = (Keys)(key & 0xff);
                if ((key & 0x100) != 0) hotKey |= Keys.Shift;
                if ((key & 0x200) != 0) hotKey |= Keys.Control;
                if ((key & 0x400) != 0) hotKey |= Keys.Alt;
            }
            return hotKey;
        }
        set {
            hotKey = value;
            if (this.IsHandleCreated) {
                var key = (int)hotKey & 0xff;
                if ((hotKey & Keys.Shift) != 0) key |= 0x100;
                if ((hotKey & Keys.Control) != 0) key |= 0x200;
                if ((hotKey & Keys.Alt) != 0) key |= 0x400;
                SendMessage(this.Handle, 0x401, (IntPtr)key, IntPtr.Zero);
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        HotKey = hotKey;
    }

    protected override CreateParams CreateParams {
        get {
            var cp = base.CreateParams;
            cp.ClassName = "msctls_hotkey32";
            return cp;
        }
    }

    private Keys hotKey;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
}