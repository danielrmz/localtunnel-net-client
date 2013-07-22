using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace LocalTunnel.Library.V2
{
    public class Tunnel
    {
        /// <summary>
        /// Opens a proxy backend.
        /// </summary>
        /// <param name="backend"></param>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <param name="use_ssl"></param>
        /// <param name="ssl_opts"></param>
        public void OpenProxyBackend(string backend, int backendPort, string target, int targetPort, string name, string client, bool useSsl = false, object sslOpts = null)
        {

            Socket proxy = SocketWrapper.Connect(backend, backendPort); // proxy = eventlet.connect(backend)
            SocketWrapper.Send(proxy, Protocol.version); // proxy.sendall(protocol.version)

            if (useSsl)
            {
                // ssl_opts = ssl_opts or {}
                // proxy = eventlet.wrap_ssl(proxy, server_side=False, **ssl_opts)
            }
             
            Protocol.SendMessage(proxy, Protocol.proxy_request(name, client));

            var reply = Protocol.RecvMessage(proxy);

            if (reply != null && reply.ContainsKey("proxy"))
            {
                try
                {
                    Socket local = SocketWrapper.Connect(target, targetPort); // local = eventlet.connect(target)
                    Util.JoinSockets(proxy, local); // util.join_sockets(proxy, local); 
                }
                catch (Exception ex)
                {
                    proxy.Close();
                }
            }
            else if (reply != null && reply.ContainsKey("error"))
            {
                throw new Exception(string.Format("ERROR: {0}", reply["error"]));
            }
            else
            {
                //pass
            }
        }

        /// <summary>
        /// Starts the client
        /// </summary>
        /// <param name="kwargs"></param>
        public void StartClient(Dictionary<string, object> kwargs)
        {
            string host = kwargs["host"].ToString();
            bool use_ssl = false;
            string backend_port = kwargs.ContainsKey("backend_port") ? kwargs["backend_port"].ToString() : string.Empty;
            object ssl_opts = null;

            if (string.IsNullOrEmpty(backend_port))
            {
                try
                {
                    backend_port = Util.DiscoverBackendPort(host).ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("ERROR: Unable to connect to service. {0}", ex.Message));
                }
            }

            var hostInfo = Dns.GetHostEntry(host.Split(':')[0]);
            string frontend_ip = hostInfo.AddressList.FirstOrDefault().ToString();
            string[] frontend_parsed = Util.ParseAddress(host, default_ip: frontend_ip);
            string frontend_address = frontend_parsed[0], frontend_hostname = frontend_parsed[1];
            string[] backend = new string[] { frontend_address, backend_port };
            string name = kwargs["name"].ToString();
            string client = Util.ClientName();
            string[] target = Util.ParseAddress(kwargs["target"].ToString());

            try
            {
                Socket control = SocketWrapper.Connect(frontend_address, int.Parse(backend_port)); // control = eventlet.connect(backend)                

                if(use_ssl) {
                    // control = eventlet.wrap_ssl(control, server_side=False, **ssl_opts)
                }

                SocketWrapper.Send(control, Protocol.version); //control.sendall(Protocol.version)
                
                Protocol.SendMessage(control, Protocol.control_request(name, client));

                var reply = Protocol.RecvMessage(control);
                if (reply != null && reply.ContainsKey("control"))
                {
                    var reply2 = reply["control"];
                    Action maintain_proxy_backend_pool = () =>
                    {
                        // pool = eventlet.greenpool.GreenPool(reply['concurrency'])
                        while (true)
                        {
                            // pool.spawn_n(open_proxy_backend, backend, target, name, client, use_ssl, ssl_opts)
                        }
                    };

                    // proxying = eventlet.spawn(maintain_proxy_backend_pool)
                    
                    Console.WriteLine(string.Format("{0}", reply["banner"]));
                    Console.WriteLine(string.Format("Port {0} is now accessible from http://{1} ...\n", target[1], reply["host"])); 

                    try
                    {
                        while (true)
                        {
                            var message = Protocol.RecvMessage(control);
                            if (!message.ContainsKey("control")
                                || (message.ContainsKey("control") && message["control"].ToString() != Protocol.control_ping()["control"]))
                            {
                                throw new Exception("Not responding");
                            }

                            Protocol.SendMessage(control, Protocol.control_pong());
                        }
                    }
                    catch (Exception ex3)
                    {
                        // proxying.kill();
                    }
                }
                else if (reply != null && reply.ContainsKey("error"))
                {
                    throw new Exception(string.Format("ERROR: {0}", reply["message"]));
                }
                else
                {
                    throw new Exception("ERROR: Unexpected server reply. Make sure you have the latest version of the client.");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Runs the client with the default parameters.
        /// </summary>
        public void Run()
        {
            var ids = Guid.NewGuid().ToString().Split('-');

            Dictionary<string, object> args = new Dictionary<string, object>() { 
                {"host", "v2.localtunnel.com"},
                {"name", ids[ids.Length - 1]},
                {"concurrency", 3},
                {"target", "80"}
            };

            this.StartClient(args);
            /*
             parser = argparse.ArgumentParser(
                        description='Open a public HTTP tunnel to a local server')
            parser.add_argument('-s', dest='host', metavar='address',
                        default='v2.localtunnel.com',
                        help='localtunnel server address (default: v2.localtunnel.com)')
            parser.add_argument('--version', action='store_true',
                        help='show version information for client and server')
            parser.add_argument('-m', action='store_true',
                        help='show server metrics and exit')


            if '--version' in sys.argv:
                args = parser.parse_args()
                print "client: {}".format(__version__)
                try:
                    server_version = util.lookup_server_version(args.host)
                except:
                    server_version = '??'
                print "server: {} ({})".format(server_version, args.host)
                sys.exit(0)
            elif '-m' in sys.argv:
                args = parser.parse_args()
                util.print_server_metrics(args.host)
                sys.exit(0)

            parser.add_argument('-n', dest='name', metavar='name',
                        default=str(uuid.uuid4()).split('-')[-1],
                        help='name of the tunnel (default: randomly generate)')
            parser.add_argument('-c', dest='concurrency', type=int,
                        metavar='concurrency', default=3,
                        help='number of concurrent backend connections')
            parser.add_argument('target', metavar='target', type=str,
                        help='local target port or address of server to tunnel to')
            args = parser.parse_args()


            start_client(**vars(args))
             */
        }

    }
}
