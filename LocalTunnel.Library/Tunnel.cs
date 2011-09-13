﻿/*
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
using System.IO;
using LocalTunnel.Models;
using System.Web.Script.Serialization;
using Tamir.SharpSsh.jsch;

namespace LocalTunnel.Library
{
    /// <summary>
    /// Core class for Localtunnel service.
    /// Based on the Ruby client implementation.
    /// </summary>
    public class Tunnel
    {
        #region Properties

        /// <summary>
        /// Service host, the gateway between your machine and the world. 
        /// Defaults to open.localtunnel.com
        /// </summary>
        public string ServiceHost
        {
            get { return _serviceHost; }
            set { _serviceHost = value; }
        }

        /// <summary>
        /// The host generated by localtunnel
        /// </summary>
        public string TunnelHost
        {
            get
            {
                if (_config == null)
                {
                    return string.Empty;
                }
                return _config.host;
            }
        }

        /// <summary>
        /// Date the tunnel was created
        /// </summary>
        public DateTime Created { get; private set; }

        /// <summary>
        /// Port to be exposed
        /// </summary>
        public int LocalPort { get; private set; }

        /// <summary>
        /// Public Key file path
        /// </summary>
        public string PublicKeyFile { get; private set; }

        /// <summary>
        /// Private key filepath
        /// </summary>
        public string PrivateKeyFile
        {
            get
            {
                if (string.IsNullOrEmpty(_privateKeyFile))
                {
                    string privatekey = this.PublicKeyFile.Replace(".pub", "");
                    if (!File.Exists(privatekey)) {
                        throw new FileNotFoundException("Private key file not found", privatekey);
                    }
                    return privatekey;
                }
                else
                {
                    return _privateKeyFile;
                }
            }
            private set
            {
                if (!File.Exists(_privateKeyFile))
                {
                    throw new FileNotFoundException("Private key file not found", _privateKeyFile);
                }

                _privateKeyFile = value;
            }
        }

        /// <summary>
        /// Private key filepath
        /// </summary>
        private string _privateKeyFile;

        /// <summary>
        /// The contents of the private key
        /// </summary>
        private string _privateKey { 
            get {
                if (string.IsNullOrEmpty(PrivateKeyFile))
                {
                    if (_keyPair == null)
                    {
                        throw new Exception("SSH Keys not set");
                    }
                    MemoryStream o = new MemoryStream();
                    _keyPair.writePrivateKey(o);
                    return o.ToString();
                }
                else
                {
                    return File.ReadAllText(PrivateKeyFile);
                }
            } 
        }

        /// <summary>
        /// The contents of the public key
        /// </summary>
        private string _publicKey { 
            get {
                if (string.IsNullOrEmpty(PublicKeyFile))
                {
                    if (_keyPair == null)
                    {
                        throw new Exception("SSH Keys not set");
                    }
                    MemoryStream o = new MemoryStream();
                    string comment = string.Format("localtunnel-{0}", (int)((DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds));
                    _keyPair.writePublicKey(o, comment);
                    string str = System.Text.Encoding.Default.GetString(o.GetBuffer(), 0, o.GetBuffer().Length);
                    string[] splitChar = new string[1];
                    splitChar[0] = "\\n";
                    return str.Substring(0, str.IndexOf(comment)) + comment;
                }
                else
                {
                    return File.ReadAllText(PublicKeyFile);
                }
            } 
        }
        
        /// <summary>
        /// Configuration obtained through a first request to the service.
        /// </summary>
        private ConfigurationResponse _config;

        /// <summary>
        /// SSH Session object
        /// </summary>
        private Session _session { get; set; }

        /// <summary>
        /// Service host
        /// </summary>
        private string _serviceHost = "open.localtunnel.com";

        /// <summary>
        /// SSH Key pair generated for the tunnel
        /// </summary>
        private KeyPair _keyPair { get; set; }

        /// <summary>
        /// SSH 
        /// </summary>
        private JSch _jsch = new JSch();

        #endregion

        #region Constructors

        /// <summary>
        /// Simple constructor, key pairs are generated automatically.
        /// </summary>
        /// <param name="localPort"></param>
        public Tunnel(int localPort)
        {
            this.LocalPort = localPort;
            _keyPair = KeyPair.genKeyPair(this._jsch, KeyPair.RSA, 2048);        
        }

        /// <summary>
        /// Constructor that assumes the private key file is removing *.pub from the public key filename.
        /// </summary>
        /// <param name="localPort">Port to be forwarded</param>
        /// <param name="publicKeyFile">Key to add to LocalTunnel's service</param>
        public Tunnel(int localPort, string publicKeyFile)
        {
            this.LocalPort = localPort;
            this.PublicKeyFile = publicKeyFile;
        }

        /// <summary>
        /// Constructor that specifies both keys.
        /// </summary>
        /// <param name="localPort">Port to be forwarded</param>
        /// <param name="publicKeyFile">Key to add to LocalTunnel's service</param>
        /// <param name="privateKeyFile">Private key used when creating the tunnel via ssh to the service.</param>
        public Tunnel(int localPort, string publicKeyFile, string privateKeyFile)
        {
            this.LocalPort = localPort;
            this.PublicKeyFile  = publicKeyFile;
            this.PrivateKeyFile = privateKeyFile;
        }

        #endregion

        #region Core client methods

        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <returns></returns>
        public Tunnel Execute()
        {
            RegisterTunnel();
            StartTunnel();

            return this;
        }

        /// <summary>
        /// Stops the local tunnel.
        /// </summary>
        public void StopTunnel()
        {
            _session.delPortForwardingR(_config.through_port);
            _session.disconnect();
        }

        /// <summary>
        /// Registers a tunnel, meaning creating a request to localtunnel to open up a subdomain and port to bridge to.
        /// </summary>
        private void RegisterTunnel()
        {
            string url = string.Format("http://{0}/", ServiceHost);

            try
            {
                _config = Utilities.DoPost<ConfigurationResponse>(url, new Dictionary<string, string>() { { "key", _publicKey } });

                if (!string.IsNullOrEmpty(_config.error))
                {
                    throw new ServiceException(_config.error);
                }
            }
            catch (ArgumentException ae)
            {
                throw new ServiceException("Invalid server response");
            }

            Created =  DateTime.Now;
            Port.AddUsage(LocalPort, ServiceHost);
        }

        /// <summary>
        /// Starts the ssh connection, and bridge the streams.
        /// </summary>
        private void StartTunnel()
        {
            try
            {
                // Create a new JSch instance
                JSch jsch = new JSch();
                
                // Use the private key to identify the user
                if (_keyPair == null)
                {
                    jsch.addIdentity(PrivateKeyFile);
                }
                else
                {
                    jsch.addIdentity("localtunnel", _keyPair);
                }

                // Create a new SSH session
                _session = jsch.getSession(_config.user, _config.host);

                UserInfo ui = new MyUserInfo();
                _session.setUserInfo(ui);

                // Connect
                _session.connect();

                // Forward port
                _session.setPortForwardingR(_config.through_port, "127.0.0.1", LocalPort);
               
                Console.WriteLine(string.Format("Setting up port {0} redirect to {1}", LocalPort, _config.host));

            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }


        }

        #endregion

       
    }
}
