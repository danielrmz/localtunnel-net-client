localtunnel for windows
=======================

Expose instantly your local webserver to the internet! 
See [main project](https://github.com/progrium/localtunnel) for more info.

Build & Use
-------------

If you would like to run the latest build, download or git clone the latest version, 
verify the active configuration either if your OS is **x86** or **x64** and then build the app.

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

Changelog
---------
- 1.0.4.2 - Modified solution configurations for x64 and x86 builds. Removed temporarily installer projects from solution as they are not supported in VS2012. (issue #12)
- 1.0.4.1 - Fixed DataGridView error when stopping a tunnel. (issue #8)
- 1.0.4 - Removed dependency with System.Web by using JSON.net deserializer.
- 1.0.3 - Refactored 64 bit code to avoid using build events.
        Readded installation packages.
- 1.0.2 - Added support for 64-bit systems (issue #6)
        Removed temporarily installation packages. 

Known Bugs
----------
 * It sometimes disconnects from the server, but could be a timeout from it. This happens even though we send a keep alive packet every 5 seconds.

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

