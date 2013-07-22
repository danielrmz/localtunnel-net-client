using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace LocalTunnel.Library.V2
{
    public class Protocol
    {
        public const string version = "LTP/0.2";

        private static Dictionary<string, string> errors = new Dictionary<string, string>() { 
            {"unavailable","This tunnel name is unavailable"},
            {"expired","This tunnel has expired"}
        }; 

        #region Initial protocol assertion

        /// <summary>
        /// Verifies the protocol version
        /// </summary>
        /// <returns></returns>
        public static bool AssertProtocol(object socket)
        {
            //protocol = socket.recv(len(version))
            var protocol = string.Empty;
            return protocol == version; 
        }

        #endregion

        #region Message IO

        /// <summary>
        /// receives a message through the socket
        /// </summary>
        public static Dictionary<string, object> RecvMessage(Socket socket)
        {
            try
            {
                //    header = socket.recv(4)
                //    length = struct.unpack(">I", header)[0]
                //    data = socket.recv(length)
                //    message = json.loads(data)
                Dictionary<string,object> message = null;
                return message;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Sends a message through the socket.
        /// </summary>
        public static void SendMessage(Socket socket, Dictionary<string, Dictionary<string, string>> message)
        {
            //data = json.dumps(message)
            //header = struct.pack(">I", len(data))
            //socket.sendall(''.join([header, data]))
        }

        /// <summary>
        /// Sends a message through the socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        public static void SendMessage(Socket socket, Dictionary<string, string> message)
        {
            //data = json.dumps(message)
            //header = struct.pack(">I", len(data))
            //socket.sendall(''.join([header, data]))
        }

        #endregion

        #region Message types

        /// <summary>
        /// Creates a control request
        /// </summary>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <param name="protect"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> control_request(string name, string client, bool protect = false, string domain = "")
        {
            Dictionary<string, string> request = new Dictionary<string, string>() { 
                { "name", name },
                { "client", client }
            };

            if (protect)
            {
                request.Add("protect", protect.ToString());
            }

            if (!string.IsNullOrEmpty(domain))
            {
                request.Add("domain", domain);
            }

            return new Dictionary<string, Dictionary<string, string>>() {
                {"control", request} 
            };
            
        }

        /// <summary>
        /// Returns reply message for control.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="concurrency"></param>
        /// <param name="banner"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> control_reply(string host, object concurrency, string banner = "")
        {
            Dictionary<string, string> reply = new Dictionary<string, string>() { 
                { "host", host },
                { "concurrency", concurrency.ToString() }
            };

            if (!string.IsNullOrEmpty(banner))
            {
                reply.Add("banner", banner);
            }

            return new Dictionary<string, Dictionary<string, string>>() {
                {"control", reply} 
            };
            
        }

        /// <summary>
        /// Ping
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> control_ping()
        {
            return new Dictionary<string, string>() { { "control", "ping" } };  
        }

        /// <summary>
        /// Pong
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> control_pong()
        {
            return new Dictionary<string, string>() { { "control", "pong" } };  
        }

        /// <summary>
        /// Returns a proxy request object.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> proxy_request(string name, string client)
        {
            return new Dictionary<string, Dictionary<string, string>>() { 
                { "proxy", 
                   new Dictionary<string,string>() 
                   { 
                        { "name", name },  
                        { "client", client }
                   } 
                } 
            }; 
            //return {'proxy': dict(name=name, client=client)}
        }

        /// <summary>
        /// Returns a proxy param
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, bool> proxy_reply()
        {
            return new Dictionary<string, bool>() { {"proxy", true} }; 
        }

        /// <summary>
        /// Replies with an error
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static Dictionary<string, string> error_reply(object error)
        {
            if (error is Exception)
            {
                return new Dictionary<string, string>() { 
                    {"error","exception"},
                    {"message", (error as Exception).Message}
                };
            }
            else
            {
                string message = errors.ContainsKey(error.ToString()) ? errors[error.ToString()] : "Error not found";

                return new Dictionary<string, string>() { 
                    {"error","error"},
                    {"message", message}
                };
            } 
        }

        #endregion
    }
}
