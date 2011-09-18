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
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine("  localtunnel.exe port [/path/to/privatekey]");
                Console.WriteLine("");
            }
            else
            {
                int localPort = 0;
                int.TryParse(args[0], out localPort);
                string sshkeypath = args[1];

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
                        if (args.Length == 2)
                        {
                            (new Tunnel(localPort, sshkeypath)).Execute();
                        }
                        else if (args.Length == 1)
                        {
                            (new Tunnel(localPort)).Execute();
                        }
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
