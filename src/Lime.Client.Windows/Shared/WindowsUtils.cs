using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Lime.Client.Windows.Shared
{
    public static class WindowsUtils
    {
        #region GetLastInputInfo

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(out LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public static readonly int SizeOf =
                   Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public int dwTime;
        }

        /// <summary>
        /// Gets the user idle time in milisseconds
        /// http://dataerror.blogspot.com.br/2005/02/detect-windows-idle-time.html
        /// </summary>
        /// <returns></returns>
        public static long GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            int envTicks = Environment.TickCount;

            if (GetLastInputInfo(out lastInputInfo))
            {
                int lastInputTick = lastInputInfo.dwTime;
                return envTicks - lastInputTick;
            }
            else
            {
                return 0;
            }
        }

        #endregion


        #region FlashWindowEx

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        //Stop flashing. The system restores the window to its original state. 
        public const UInt32 FLASHW_STOP = 0;
        //Flash the window caption. 
        public const UInt32 FLASHW_CAPTION = 1;
        //Flash the taskbar button. 
        public const UInt32 FLASHW_TRAY = 2;
        //Flash both the window caption and taskbar button.
        //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
        public const UInt32 FLASHW_ALL = 3;
        //Flash continuously, until the FLASHW_STOP flag is set. 
        public const UInt32 FLASHW_TIMER = 4;
        //Flash continuously until the window comes to the foreground. 
        public const UInt32 FLASHW_TIMERNOFG = 12;


        /// <summary>
        /// http://stackoverflow.com/questions/73162/how-to-make-the-taskbar-blink-my-application-like-messenger-does-when-a-new-mess
        /// </summary>
        /// <param name="pwfi"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        public static void FlashWindow(Window window, FlashMode mode)
        {
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            switch (mode)
            {
                case FlashMode.Stop:
                    fInfo.dwFlags = FLASHW_STOP;
                    break;
                case FlashMode.Caption:
                    fInfo.dwFlags = FLASHW_CAPTION;
                    break;
                case FlashMode.Tray:
                    fInfo.dwFlags = FLASHW_TRAY;
                    break;
                case FlashMode.All:
                    fInfo.dwFlags = FLASHW_ALL;
                    break;
                case FlashMode.UntilStop:
                    fInfo.dwFlags = FLASHW_TIMER;
                    break;
                case FlashMode.UntilForeground:
                    fInfo.dwFlags = FLASHW_TIMERNOFG;
                    break;
            }

            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            FlashWindowEx(ref fInfo);
        }

        #endregion
    }

    [Flags]
    public enum FlashMode
    {
        Stop,
        Caption,
        Tray,
        All,
        UntilStop,
        UntilForeground
    }
}
