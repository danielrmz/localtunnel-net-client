﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Renci.SshNet.Tests.Properties;
using Renci.SshNet.Common;
using System.Threading;

namespace Renci.SshNet.Tests.SshClientTests
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
    public partial class TestPortForwarding
    {
        [TestMethod]
        [ExpectedException(typeof(SshConnectionException))]
        public void Test_PortForwarding_Local_Without_Connecting()
        {
            using (var client = new SshClient(Resources.HOST, Resources.USERNAME, Resources.PASSWORD))
            {
                var port1 = client.AddForwardedPort<ForwardedPortLocal>("localhost", 8084, "www.renci.org", 80);
                port1.Exception += delegate(object sender, ExceptionEventArgs e)
                {
                    Assert.Fail(e.Exception.ToString());
                };
                port1.Start();

                System.Threading.Tasks.Parallel.For(0, 100,
                    //new ParallelOptions
                    //{
                    //    MaxDegreeOfParallelism = 20,
                    //},
                    (counter) =>
                    {
                        var start = DateTime.Now;
                        var req = HttpWebRequest.Create("http://localhost:8084");
                        using (var response = req.GetResponse())
                        {

                            var data = ReadStream(response.GetResponseStream());
                            var end = DateTime.Now;

                            Debug.WriteLine(string.Format("Request# {2}: Lenght: {0} Time: {1}", data.Length, (end - start), counter));
                        }
                    }
                );
            }
        }

        [TestMethod]
        public void Test_PortForwarding_Remote()
        {
            //  ******************************************************************
            //  ************* Tests are still in not finished ********************
            //  ******************************************************************

            using (var client = new SshClient(Resources.HOST, Resources.USERNAME, Resources.PASSWORD))
            {
                client.Connect();
                var port1 = client.AddForwardedPort<ForwardedPortRemote>(8082, "www.renci.org", 80);
                port1.Exception += delegate(object sender, ExceptionEventArgs e)
                {
                    Assert.Fail(e.Exception.ToString());
                };
                port1.Start();
                var boundport = port1.BoundPort;

                System.Threading.Tasks.Parallel.For(0, 5,
                    //new ParallelOptions
                    //{
                    //    MaxDegreeOfParallelism = 1,
                    //},
                    (counter) =>
                    {
                        var cmd = client.CreateCommand(string.Format("wget -O- http://localhost:{0}", boundport));
                        var result = cmd.Execute();
                        var end = DateTime.Now;
                        Debug.WriteLine(string.Format("Length: {0}", result.Length));
                    }
                );
                Thread.Sleep(1000 * 100);
                port1.Stop();
            }
        }

        [TestMethod]
        public void Test_PortForwarding_Local()
        {
            using (var client = new SshClient(Resources.HOST, Resources.USERNAME, Resources.PASSWORD))
            {
                client.Connect();
                var port1 = client.AddForwardedPort<ForwardedPortLocal>("localhost", 8084, "www.renci.org", 80);
                port1.Exception += delegate(object sender, ExceptionEventArgs e)
                {
                    Assert.Fail(e.Exception.ToString());
                };
                port1.Start();

                System.Threading.Tasks.Parallel.For(0, 100,
                    //new ParallelOptions
                    //{
                    //    MaxDegreeOfParallelism = 20,
                    //},
                    (counter) =>
                    {
                        var start = DateTime.Now;
                        var req = HttpWebRequest.Create("http://localhost:8084");
                        using (var response = req.GetResponse())
                        {

                            var data = ReadStream(response.GetResponseStream());
                            var end = DateTime.Now;

                            Debug.WriteLine(string.Format("Request# {2}: Length: {0} Time: {1}", data.Length, (end - start), counter));
                        }
                    }
                );
            }
        }
    }
}
