using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace LocalTunnel.UI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool firstInstance = false;
            Mutex mtx = new Mutex(true, "LT.Jumplist.Lock", out firstInstance);

            if(firstInstance) {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            } else {
                // Send commands to active window.
                HandleCmdLineArgs();
            }
        }

        public static void HandleCmdLineArgs() {
            string[] args = Environment.GetCommandLineArgs();

            if(args.Length > 1) {
                MessageHelper.SendMessage("Localtunnel Manager", MessageHelper.TunnelPort, new IntPtr(int.Parse(args[1])), IntPtr.Zero);
            }
        }
        
    }
}
