using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace LocalTunnel.UI
{
    internal class MessageHelper {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
           private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
  
           [DllImport("user32.dll")]
           public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
   
           [DllImport("user32.dll")]
           public static extern int RegisterWindowMessage(string msgName);
   

        public static int TunnelPort;

        static MessageHelper() {
            TunnelPort = RegisterWindowMessage("Win7.Jumplist.TunnelPort");
        }
   
           public static int RegisterMessage(string msgName)
           {
               return RegisterWindowMessage(msgName);
           }
   
           public static void SendMessage(string windowTitle, int msgId)
           {
               SendMessage(windowTitle, msgId, IntPtr.Zero, IntPtr.Zero);
           }
   
           public static bool SendMessage(string windowTitle, int msgId, IntPtr wParam, IntPtr lParam)
           {
               IntPtr WindowToFind = FindWindow(null, windowTitle);
               if (WindowToFind == IntPtr.Zero) return false;
   
               long result = SendMessage(WindowToFind, msgId, wParam, lParam);
   
               if (result == 0) return true;
               else return false;
           }
       


    }
}

