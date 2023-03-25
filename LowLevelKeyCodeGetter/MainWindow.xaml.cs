// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;

// クリップボード操作
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// Windows API 
using System.Runtime.InteropServices;
using static LowLevelKeyCodeGetter.WindowsAPI;
using Microsoft.UI;
using Windows.Graphics;
using System.Threading.Tasks;

using LowLevelKeyCodeGetter.Utils;

namespace LowLevelKeyCodeGetter
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private List<KeyEvent> keyEvents;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "LowLevelKeyCodeGetter";

            keyEvents = new List<KeyEvent>();

            SetWindowSize(1250, 950);
        }

        private void SetWindowSize(int width, int height)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(myWndId);

            appWindow.Resize(new SizeInt32(width, height));
        }

        private void AddSystemLog(string log)
        {
            if (systemLogTextBox.Text == "") systemLogTextBox.Text = log;
            else systemLogTextBox.Text = systemLogTextBox.Text + "\n" + log;
        }

        private void AddKeyDownLog(string log)
        {
            keyDownLast3TextBox.Text = keyDownLast2TextBox.Text;
            keyDownLast2TextBox.Text = keyDownLast1TextBox.Text;
            keyDownLast1TextBox.Text = log;
        }
        private void AddKeyUpLog(string log)
        {
            keyUpLast3TextBox.Text = keyUpLast2TextBox.Text;
            keyUpLast2TextBox.Text = keyUpLast1TextBox.Text;
            keyUpLast1TextBox.Text = log;
        }
        private void ActivateKeyDownImage()
        {
            keyDownImage.Opacity = 1;
        }
        private void ActivateKeyUpImage()
        {
            keyUpImage.Opacity = 1;
        }
        private async void DeactivateKeyDownImageAsync()
        {
            await Task.Delay(100);
            keyDownImage.Opacity = 0.3;
        }
        private async void DeactivateKeyUpImageAsync()
        {
            await Task.Delay(100);
            keyUpImage.Opacity = 0.3;
        }

        private void ClearKeyLog(object sender, RoutedEventArgs e)
        {
            keyEvents = new List<KeyEvent>();
            InvertedListView.Items.Clear();

            keyLogTextBox.Text = "";

            keyDownLast3TextBox.Text = "";
            keyDownLast2TextBox.Text = "";
            keyDownLast1TextBox.Text = "";
            keyUpLast3TextBox.Text = "";
            keyUpLast2TextBox.Text = "";
            keyUpLast1TextBox.Text = "";
        }

        private void CopyKeyLog(object sender, RoutedEventArgs e)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(keyEvents, options);

            DataPackage data = new DataPackage();
            data.SetText(json);
            Clipboard.SetContent(data);
        }
            private void KeyDownHandler(KBDLLHOOKSTRUCT kbd)
        {
            AddKeyDownMessageToEnd(keyEvents.Count, kbd.vkCode);

            ActivateKeyDownImage();
            DeactivateKeyDownImageAsync();

            string log = kbdToString(kbd);
            AddKeyDownLog(log);

            keyEvents.Add(new KeyEvent(kbd));
        }

        private void KeyUpHandler(KBDLLHOOKSTRUCT kbd)
        {
            AddKeyUpMessageToEnd(keyEvents.Count, kbd.vkCode);

            ActivateKeyUpImage();
            DeactivateKeyUpImageAsync();

            string log = kbdToString(kbd);
            AddKeyUpLog(log);
            keyEvents.Add(new KeyEvent(kbd));
        }
        private string kbdToString(KBDLLHOOKSTRUCT kbd)
        {
            string txt = $"vkCode: {kbd.vkCode}\n";
            txt += $"scanCode: {kbd.scanCode}\n";
            txt += $"flags: {kbd.flags}\n";
            txt += $"time: {kbd.time}\n";
            txt += $"dwExtraInfo: {kbd.dwExtraInfo}";
            return txt;
        }

        private void AddKeyDownMessageToEnd(int num, uint vkCode)
        {
            InvertedListView.Items.Add(
                new Message(num, vkCode, "Down", DateTime.Now.ToString("yy/MM/dd HH:mm:ss.fff"), HorizontalAlignment.Left, "Green")
                );
        }
        private void AddKeyUpMessageToEnd(int num, uint vkCode)
        {
            InvertedListView.Items.Add(
                new Message(num, vkCode, "Up", DateTime.Now.ToString("yy/MM/dd HH:mm:ss.fff"), HorizontalAlignment.Right, "Blue")
                );
        }
        private void MessageClickHandler(object sender, RoutedEventArgs e)
        {
            //var dataContext = listViewItem.DataContext;
            var message = (sender as ListView).SelectedItem as Message;
            if (message != null)
            {
                keyLogTextBox.Text = $"{message}\n------------------\n{keyEvents[message.MsgNum]}";
            }
        }

        #region Windows API
        private void SetHook(object sender, RoutedEventArgs e)
        {
            IntPtr hookPtr = WindowsAPI.SetHook(KeyDownHandler, KeyUpHandler);
            if (hookPtr != IntPtr.Zero)
            {
                AddSystemLog("Hooked successfully.");
                setHookButton.IsEnabled = false;
                unHookButton.IsEnabled = true;
            }
            else
            {
                AddSystemLog("Failed to hook.");
            }
        }
        private void Unhook(object sender, RoutedEventArgs e)
        {
            bool hasUnhooked = WindowsAPI.Unhook();
            if (hasUnhooked)
            {
                AddSystemLog("Unhooked successfully.");
                setHookButton.IsEnabled = true;
                unHookButton.IsEnabled = false;
            }
            else
            {
                AddSystemLog("Failed to unhook.");
            }
        }
        #endregion
    }
    /// <summary>
    /// このプログラムから呼び出す Windows API を管理する自作クラス
    /// 記述の仕方は基本的に PINVOKE.NET に従う
    /// </summary>
    public static class WindowsAPI
    {
        /// <summary>
        /// Windows API メソッドである SetWindowsHookEx に渡す hookType を表す列挙体。
        /// これの値によって SetWindowsHookEx にインストールするフックプロシージャの性質を決める
        /// </summary>
        public enum HookType : int
        {
            /// <summary>
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. For more information, see the MessageProc hook procedure.
            /// </summary>
            WH_MSGFILTER = -1,
            /// <summary>
            /// Installs a hook procedure that records input messages posted to the system message queue. This hook is
            /// useful for recording macros. For more information, see the JournalRecordProc hook procedure.
            /// </summary>
            WH_JOURNALRECORD = 0,
            /// <summary>
            /// Installs a hook procedure that posts messages previously recorded by a WH_JOURNALRECORD hook procedure.
            /// For more information, see the JournalPlaybackProc hook procedure.
            /// </summary>
            WH_JOURNALPLAYBACK = 1,
            /// <summary>
            /// Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc
            /// hook procedure.
            /// </summary>
            WH_KEYBOARD = 2,
            /// <summary>
            /// Installs a hook procedure that monitors messages posted to a message queue. For more information, see the
            /// GetMsgProc hook procedure.
            /// </summary>
            WH_GETMESSAGE = 3,
            /// <summary>
            /// Installs a hook procedure that monitors messages before the system sends them to the destination window
            /// procedure. For more information, see the CallWndProc hook procedure.
            /// </summary>
            WH_CALLWNDPROC = 4,
            /// <summary>
            /// Installs a hook procedure that receives notifications useful to a CBT application. For more information,
            /// see the CBTProc hook procedure.
            /// </summary>
            WH_CBT = 5,
            /// <summary>
            /// Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box,
            /// message box, menu, or scroll bar. The hook procedure monitors these messages for all applications in the
            /// same desktop as the calling thread. For more information, see the SysMsgProc hook procedure.
            /// </summary>
            WH_SYSMSGFILTER = 6,
            /// <summary>
            /// Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook
            /// procedure.
            /// </summary>
            WH_MOUSE = 7,
            /// <summary>
            ///
            /// </summary>
            WH_HARDWARE = 8,
            /// <summary>
            /// Installs a hook procedure useful for debugging other hook procedures. For more information, see the
            /// DebugProc hook procedure.
            /// </summary>
            WH_DEBUG = 9,
            /// <summary>
            /// Installs a hook procedure that receives notifications useful to shell applications. For more information,
            /// see the ShellProc hook procedure.
            /// </summary>
            WH_SHELL = 10,
            /// <summary>
            /// Installs a hook procedure that will be called when the application's foreground thread is about to become
            /// idle. This hook is useful for performing low priority tasks during idle time. For more information, see the
            /// ForegroundIdleProc hook procedure.
            /// </summary>
            WH_FOREGROUNDIDLE = 11,
            /// <summary>
            /// Installs a hook procedure that monitors messages after they have been processed by the destination window
            /// procedure. For more information, see the CallWndRetProc hook procedure.
            /// </summary>
            WH_CALLWNDPROCRET = 12,
            /// <summary>
            /// Installs a hook procedure that monitors low-level keyboard input events. For more information, see the
            /// LowLevelKeyboardProc hook procedure.
            /// </summary>
            WH_KEYBOARD_LL = 13,
            /// <summary>
            /// Installs a hook procedure that monitors low-level mouse input events. For more information, see the
            /// LowLevelMouseProc hook procedure.
            /// </summary>
            WH_MOUSE_LL = 14
        }

        /// <summary>
        /// フックメソッドに渡すためのコールバックメソッドを表すデリゲート型
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nc-winuser-hookproc
        /// </summary>
        delegate IntPtr HookProc(int code, int wParam, int lParam);

        // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa
        // (winuser.h)
        // HHOOK SetWindowsHookExA(
        //        [in]
        //        int idHook,
        //        [in] HOOKPROC lpfn,
        //        [in] HINSTANCE hmod,
        //        [in] DWORD dwThreadId
        // );
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// フックプロシージャのポインタ
        /// </summary>
        static IntPtr hookPtr = IntPtr.Zero;

        /// <summary>
        /// HookType WH_KEYBOARD_LL でフックするコールバックメソッド
        /// 実体はシグネチャ HookProc であるが、リファレンス制作や可読性向上のためのプレースホルダーとして LowLevelKeyboardProc とされている
        /// https://learn.microsoft.com/ja-jp/previous-versions/windows/desktop/legacy/ms644985(v=vs.85)
        /// </summary>
        static IntPtr LowLevelKeyboardProc(int nCode, int wParam, int lParam)
        {
            // Windows によって以下のように定義されている
            // #define WM_KEYDOWN                      0x0100
            // #define WM_SYSKEYDOWN                   0x0104
            if (wParam == 0x0100 || wParam == 0x0104)
            {
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure((IntPtr)lParam, typeof(KBDLLHOOKSTRUCT));
                OnKeyDown.Invoke(kbd);
            }
            // Windows によって以下のように定義されている
            // #define WM_KEYUP                        0x0101
            // #define WM_SYSKEYUP                     0x0105
            else if (wParam == 0x0101 || wParam == 0x0105)
            {
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure((IntPtr)lParam, typeof(KBDLLHOOKSTRUCT));
                OnKeyUp.Invoke(kbd);
            }
            return CallNextHookEx(hookPtr, nCode, wParam, lParam);
        }
        /// <summary>
        /// LowLevelKeyboardProc を SetWindowsHookEx に直渡しすると、どこかのタイミングで落ちる。
        /// static なメソッドだから GC されないと思っていたが、どうも渡すタイミングで暗黙的にデリゲートに変換されて渡され（？ 要出典）、そのデリゲートがアンマネージドからしか参照されないので適当なタイミングで GC される…… っぽい？
        /// </summary>
        static HookProc _lowLevelKeyBoardProcHandler = LowLevelKeyboardProc;

        /// <summary>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callnexthookex
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam,
           int lParam);

        public static event Action<KBDLLHOOKSTRUCT> OnKeyDown;
        public static event Action<KBDLLHOOKSTRUCT> OnKeyUp;

        /// <summary>
        /// WH_KEYBOARD_LL イベントに対してフックをセットし、セットしたプロシージャのポインタを返す
        /// </summary>
        /// <param name="keyDownHandler"></param>
        /// <returns></returns>
        public static IntPtr SetHook(Action<KBDLLHOOKSTRUCT> keyDownHandler, Action<KBDLLHOOKSTRUCT> keyUpHandler)
        {
            if (hookPtr == IntPtr.Zero)
            {
                OnKeyDown = keyDownHandler;
                OnKeyUp = keyUpHandler;
                // Windows API メソッドにおいては IntPtr.Zero は null と等しいと解釈される
                // https://learn.microsoft.com/en-us/dotnet/api/system.intptr.zero?view=net-6.0
                hookPtr = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, _lowLevelKeyBoardProcHandler, IntPtr.Zero, 0);
            }
            return hookPtr;
        }

        public static bool Unhook()
        {
            // Unhook は成功したら True（ノンゼロ）を返す
            if (UnhookWindowsHookEx(hookPtr))
            {
                hookPtr = IntPtr.Zero;
                OnKeyDown = null;
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    public class Message
    {
        public int MsgNum { get; private set; }
        public uint MsgVKCode { get; private set; }
        private string msgText;
        public string MsgText
        {
            get
            {
                return $"{MsgNum}: vk[{MsgVKCode}] {msgText}";
            }
            private set
            {
                msgText = value;
            }
        }
        public string MsgDateTimeString { get; private set; }
        public HorizontalAlignment MsgAlignment { get; set; }
        public string MsgBgColor;
        public Message(int num, uint vkCode, string text, string dateTimeString, HorizontalAlignment align, string bgColor)
        {
            MsgNum = num;
            MsgVKCode = vkCode;
            MsgText = text;
            MsgDateTimeString = dateTimeString;
            MsgAlignment = align;
            MsgBgColor = bgColor;
        }

        public override string ToString()
        {
            return MsgText;
        }
    }
}
