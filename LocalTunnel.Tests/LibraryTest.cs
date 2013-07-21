using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LocalTunnel.Library;
using System.Net;
using System.IO;

namespace LocalTunnel.Tests
{
    using NUnit.Framework;

    /// <summary>
    /// Tests the tunnel methods to assure it is connecting
    /// and forwarding the packets correctly
    /// </summary>
    [TestFixture]
    public class LibraryTest
    {

        /// <summary>
        /// Tests that the tunnel connection is created
        /// correctly and that the content return is the same
        /// as the web server being tunneled.
        /// 
        /// In the way since we are not providing any ssh-rsa keys
        /// it creates a pair on the fly testing in the process
        /// the correct generation of these.
        /// </summary>
        [Test]
        public void TunnelTest()
        {
            const int port = 65534;
            const string listenUrl = "http://127.0.0.1:65534/ping/";
            const string expectedResponse = "pong";

            // Create a local webserver
            using (HttpListener webserver = this.StartTestServer(listenUrl, expectedResponse))
            {

                // Create the tunnel
                Tunnel tunnel = new Tunnel(port);
                tunnel.Execute();

                // Check it is connected
                Assert.IsTrue(tunnel.IsConnected);
                Assert.IsFalse(tunnel.IsStopped);

                // Check the new host responds with the expected response.
                WebRequest request = WebRequest.Create(string.Format("http://{0}/ping/",tunnel.TunnelHost));
                string response = (new StreamReader(request.GetResponse().GetResponseStream())).ReadToEnd();
                Assert.AreEqual(response, expectedResponse);

                webserver.Close();
            }
        }

        /// <summary>
        /// Creates a simple server to test the tunnel
        /// </summary>
        /// <param name="listenUrl"></param>
        /// <param name="expectedResponse"></param>
        /// <returns></returns>
        private HttpListener StartTestServer(string listenUrl, string expectedResponse)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenUrl);
            listener.Start();

            listener.BeginGetContext(new AsyncCallback((result) =>
            {

                HttpListener listenr = (HttpListener)result.AsyncState;
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = expectedResponse;
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

            }), listener);

            listener.Start();

            return listener;
        }
    }
}
