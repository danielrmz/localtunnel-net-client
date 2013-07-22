using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LocalTunnel.Library.V2
{
    public class Util
    {
        /// <summary>
        /// Socket joining implementation
        /// </summary>
        public void JoinSockets(/*a,b*/)
        {
            Action<object, object> _pipe = (from_, to) => {
                while (true)
                {
                    try
                    {
                        object data = null;// from_.recv(64 * 1024);
                        if (data == null)
                        {
                            break;
                        }
                        try
                        {
                            //to.sendall(data);
                        }
                        catch (Exception ex2)
                        {
                            //from_.close();
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // from_.close();
                        break;
                    }
                }

                try
                {
                    //to.close();
                }
                catch (Exception ex3)
                {
                }
            };
            
            //pool = eventlet.greenpool.GreenPool(size=2)
            //pool.spawn_n(_pipe, a, b)
            //pool.spawn_n(_pipe, b, a)
            //return pool
             
        }

        /// <summary>
        /// Semi-unique client identifier string
        /// </summary>
        /// <returns></returns>
        public static string ClientName()
        {
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string hostname = Dns.GetHostName();
            string platform = System.Environment.OSVersion.Platform.ToString();

            return string.Format("{0}@{1};{2}", userName, hostname, platform);
        }

        /// <summary>
        /// Returns address (ip, port) and hostname from anything like:
        /// localhost:8000
        /// 8000
        /// :8000
        /// myhost:80
        /// 0.0.0.0:8000
        /// </summary>
        public static string[] ParseAddress(string address, int default_port = 0, string default_ip = null)
        {
            string defaultIP = string.IsNullOrEmpty(default_ip) ? "0.0.0.0" : default_ip;
            try
            {
                int port = int.Parse(address);
                return new string[] { defaultIP, port.ToString() };
            } 
            catch (Exception ex)
            {
                Uri uri = new Uri(string.Format("tcp://{0}", address));
                try
                {
                    return new string[] { uri.Host, (uri.Port != null ? uri.Port.ToString() : default_port.ToString()) };
                }
                catch (Exception ex2)
                {
                }

                return new string[] { defaultIP, (uri.Port != null ? uri.Port.ToString() : default_port.ToString()) };
            } 

        }

        /// <summary>
        /// Discovers a backend port.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="frontend_port"></param>
        /// <returns></returns>
        public static int DiscoverBackendPort(string hostname, int frontend_port = 80)
        {
            string endpoint = string.Format("http://{0}/meta/backend", hostname);

            var response = Dex.Utilities.Web.DoGet(endpoint);

            var statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode == HttpStatusCode.OK)
            {
                return int.Parse(Dex.Utilities.Web.ParseResponseStream(response));
            }
            else
            {
                throw new Exception("Frontend failed to provide backend port");
            }
        }

        /// <summary>
        /// Provides the current version
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public static string LocalServerVersion(string hostname)
        {
            string endpoint = string.Format("http://{0}/meta/version", hostname);

            var response = Dex.Utilities.Web.DoGet(endpoint);

            var statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode == HttpStatusCode.OK)
            {
                return Dex.Utilities.Web.ParseResponseStream(response);
            }
            else
            {
                throw new Exception("Server failed to provide version");
            } 
        }

        /// <summary>
        /// Provides server metrics
        /// </summary>
        /// <param name="hostname"></param>
        public static void PrintServerMetrics(string hostname)
        {
            string endpoint = string.Format("http://{0}/meta/metrics", hostname);

            var response = Dex.Utilities.Web.DoGet(endpoint);

            var statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode == HttpStatusCode.OK)
            {
                // for metric in resp.json:
                //  print "%(name) -40s %(value)s" % metric
                var str = Dex.Utilities.Web.ParseResponseStream(response);
            }
            else
            {
                throw new Exception("Server failed to provide metrics");
            } 
             
        }

    }
}
