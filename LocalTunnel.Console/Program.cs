/*
Copyright (C) 2011 by Daniel Ramirez (hello@danielrmz.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections;
using System.Web;
using System.IO;
using LocalTunnel.Library;

namespace LocalTunnel
{
    /// <summary>
    /// Just a simple wrapper for cli
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine("  localtunnel.exe [host:]port [/path/to/privatekey] - Will default to 127.0.0.1");
                Console.WriteLine("  localtunnel.exe host[:port] [/path/to/privatekey] - Will default to port 80");

                Console.WriteLine("");
            }
            else
            {
                string[] connectTo = args[0].Split(':');
                string hostname = connectTo[0];
                string port = connectTo.Length > 1 ? connectTo[1] : string.Empty;
                int localPort = 0;
                    
                // Try to guess if the first parameter is a host or a port
                if (string.IsNullOrEmpty(port))
                {
                    int.TryParse(hostname, out localPort);
                    if (localPort == 0)
                    {
                        // Was a host, otherwise we would have gotten a number
                        localPort = 80;
                    }
                    else
                    {
                        hostname = "127.0.0.1";
                    }
                }
                else
                {
                    int.TryParse(port, out localPort);
                }

                // TODO check correctness of hostname syntax.

                if (localPort == 0)
                {
                    Console.WriteLine("Introduce a valid port");
                    Console.WriteLine("");
                }
                else
                {
                    try
                    {
                        // Let the fun start!
                        Tunnel tunnel;
                        if (args.Length == 1)
                        {
                            tunnel = new Tunnel(localPort);
                        }
                        else 
                        {
                            string sshkeypath = args[1];
                            if (!File.Exists(sshkeypath))
                            {
                                Console.WriteLine("Private key file not found!");
                                Environment.Exit(1);
                            }
                            tunnel = new Tunnel(localPort, sshkeypath);
                        }

                        tunnel.OnSocketException = new Tunnel.EventSocketException((tun, msg) => {
                            tun.ReOpenTunnel();
                        });

                        tunnel.Execute();
                        Console.WriteLine(string.Format("LocalTunnel created, you can now access {0}:{1} through: http://{2}/", tunnel.LocalHost, tunnel.LocalPort, tunnel.TunnelHost) );
                        Console.WriteLine("[Press any key to terminate tunnel]");
                        Console.ReadKey();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Oops! there was an error while connecting. \nPlease check your port, and key files and try again.");
                    }
                }
            }
        }

    }
}
