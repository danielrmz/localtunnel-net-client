localtunnel for windows
=======================

Install & Use
-------------

Download the installer msi from the Downloads section, and run the Setup Wizard. 
This will install LocalTunnel.NET UI & CLI in your file system, and create a 
shortcut in your start menu. 

If you would like to run the latest build, you could download or git clone the latest 
version and then:

* From the command line, go to LocalTunnel.Console/bin/4.0/Release/ and run:
   `localtunnel.exe host[:port] [/path/to/private.key]`
or `localtunnel.exe [host:]port [/path/to/private.key]` 

* UI-based, open LocalTunnel.UI/bin/4.0/Release/LocalTunnel.UI.exe and enter the required info.


UI Features
-----------
 * Custom service host setting
 * Win7 Jumplists for quick tunneling
 * Public key autogeneration
 * Specify a different host address than 127.0.0.1

Known Bugs
----------
 * It sometimes disconnects from the server, but I believe its a timeout from it, although
   it is sending keep alives every 5 seconds. 

Authors
-------
Daniel Ramirez - hello@danielrmz.com


License
------- 
MIT License
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

