using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LowLevelKeyCodeGetter.Utils
{
    /// <summary>
    /// https://www.pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public KBDLLHOOKSTRUCTFlags flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    /// <summary>
    /// https://www.pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
    /// </summary>
    [Flags]
    public enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    public class KeyEvent
    {
        public uint vkCode { get; set; }
        public uint scanCode { get; set; }
        public KBDLLHOOKSTRUCTFlags flags { get; set; }
        public uint time { get; set; }
        public uint dwExtraInfo { get; set; }

        public KeyEvent(KBDLLHOOKSTRUCT kbd)
        {
            vkCode = kbd.vkCode;
            scanCode = kbd.scanCode;
            flags = kbd.flags;
            time = kbd.time;
            dwExtraInfo = (uint)kbd.dwExtraInfo;
        }
        public override string ToString()
        {
            string txt = $"vkCode: {vkCode}\n";
            txt += $"scanCode: {scanCode}\n";
            txt += $"flags: {flags}\n";
            txt += $"time: {time}\n";
            txt += $"dwExtraInfo: {dwExtraInfo}";
            return txt;
        }
    }
}
