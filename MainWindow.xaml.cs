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
using System.IO;
using System.IO.Ports;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace mass_k_vk
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] ports = SerialPort.GetPortNames();
        SerialPort serialPort2 = new SerialPort();
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId); // вот тут я забыл прописать аргументы

        private const int WH_KEYBOARD_LL = 13;

        private LowLevelKeyboardProcDelegate m_callback;
        private IntPtr m_hHook;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        const int WM_COMMAND = 0x0111;
        const int BN_CLICKED = 0;
        const int ButtonId = 0x79;
        const string fn = @"C:\Windows\system32\notepad.exe";
        public const int WM_SETTEXT = 0X000C;
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int iLeft;
            public int iTop;
            public int iRight;
            public int iBottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rectCaret;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetDlgItem(IntPtr hWnd, int nIDDlgItem);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, String lParam);
  
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetGUIThreadInfo(IntPtr hTreadID, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetActiveWindow(IntPtr idAttach);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetFocus();
        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentThreadId();

        private IntPtr LowLevelKeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
             if (nCode < 0)
                return CallNextHookEx(m_hHook, nCode, wParam, lParam);
            else {
                var khs = (KeyboardHookStruct)
                          Marshal.PtrToStructure(lParam,
                          typeof(KeyboardHookStruct));
               if (khs.VirtualKeyCode == 164) {
                    GetMass();
                    IntPtr val = new IntPtr(1);
                    return val;
                 }
                else
                    return CallNextHookEx(m_hHook, nCode, wParam, lParam);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardHookStruct
        {
            public readonly int VirtualKeyCode;
            public readonly int ScanCode;
            public readonly int Flags;
            public readonly int Time;
            public readonly IntPtr ExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProcDelegate(
         int nCode, IntPtr wParam, IntPtr lParam);

        public void SetHook()
        {
            m_callback = LowLevelKeyboardHookProc;
            m_hHook = SetWindowsHookEx(WH_KEYBOARD_LL,
             m_callback,
             GetModuleHandle(IntPtr.Zero), 0);
        }

        public void Unhook()
        {
            UnhookWindowsHookEx(m_hHook);
        }
        public MainWindow()
        {
       
            this.Topmost = true;
            InitializeComponent();
        }
        
        private void ReadMass_Click(object sender, RoutedEventArgs e)
        {
            GetMass();
        }

        private void WindowRS232_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string port in ports)
                MyCom.Items.Add(port);
            SetHook();
        }
   
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort2.IsOpen)
            {
                OpenPort.Content = "Открыть порт";
                serialPort2.Close();
                Unhook();
            }
            else
            {
                serialPort2.PortName = MyCom.Text;
                serialPort2.BaudRate = 9600;
                serialPort2.ReadTimeout = 5000;
                serialPort2.WriteTimeout = 500;
                serialPort2.Parity = 0;
                serialPort2.Open();
                if (serialPort2.IsOpen) OpenPort.Content = "Закрыть порт: "+ serialPort2.PortName;
            }
        }
private void GetMass() {
            bool _continue;
            string dats;
            _continue = true;
            GUITHREADINFO pt = new GUITHREADINFO();
            IntPtr handle = IntPtr.Zero;
            Process[] localAll = Process.GetProcesses();
            IntPtr PASCAL = GetForegroundWindow();
            IntPtr id = IntPtr.Zero;
            if (serialPort2.IsOpen)
            {
                while (_continue)
                {
                    try {

                        char[] buff = new char[8];
                        string message;
                        serialPort2.DiscardInBuffer();
                        message = serialPort2.ReadLine();
                        dats = message.Substring(6, 7).Replace(".", ",");
                        mass.Text = Convert.ToString(Convert.ToSingle(dats) * Convert.ToSingle(density.Text.Replace(".", ",")));
                    } catch {

                    } finally {
                        _continue = false;
                    }
                }
            }
            IntPtr t= GetForegroundWindow();
            int pr;
            IntPtr c =GetWindowThreadProcessId(t ,out pr);
            AttachThreadInput(GetCurrentThreadId(),c, true);
            GetGUIThreadInfo(c, ref pt);
            IntPtr HWNDFocus = GetFocus();
            SendMessage(HWNDFocus, 0x000C, 512, mass.Text);
            AttachThreadInput(GetCurrentThreadId(), c, false);
        }
        private void WindowRS232_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl) GetMass();
        }

        private void WindowRS232_Closed(object sender, EventArgs e)
        {
            serialPort2.Close();
            Unhook();
        }

        /// <summary>
        /// Some modules might be restricted from access for security purposes.
        /// This will catch that error and others.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static ProcessModule GetModule(Process p)
        {
            ProcessModule pm = null;
            try { pm = p.MainModule; }
            catch
            {
                return null;
            }
            return pm;
        }
    }
}

