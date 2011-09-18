﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Connection;

namespace Renci.SshNet.Channels
{
    /// <summary>
    /// Implements "direct-tcpip" SSH channel.
    /// </summary>
    internal partial class ChannelDirectTcpip : Channel
    {
        public EventWaitHandle _channelEof = new AutoResetEvent(false);

        private EventWaitHandle _channelOpen = new AutoResetEvent(false);

        private EventWaitHandle _channelData = new AutoResetEvent(false);

        private Socket _socket;

        /// <summary>
        /// Gets the type of the channel.
        /// </summary>
        /// <value>
        /// The type of the channel.
        /// </value>
        public override ChannelTypes ChannelType
        {
            get { return ChannelTypes.DirectTcpip; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDirectTcpip"/> class.
        /// </summary>
        public ChannelDirectTcpip()
            : base()
        {

        }

        /// <summary>
        /// Binds channel to specified remote host.
        /// </summary>
        /// <param name="remoteHost">The remote host.</param>
        /// <param name="port">The port.</param>
        /// <param name="socket">The socket.</param>
        public void Bind(string remoteHost, uint port, Socket socket)
        {
            this._socket = socket;

            IPEndPoint ep = socket.RemoteEndPoint as IPEndPoint;
            

            if (!this.IsConnected)
            {
                throw new SshException("Session is not connected.");
            }

            //  Open channel
            this.SendMessage(new ChannelOpenMessage(this.LocalChannelNumber, this.LocalWindowSize, this.PacketSize,
                                                        new DirectTcpipChannelInfo(remoteHost, port, ep.Address.ToString(), (uint)ep.Port)));

            //  Wait for channel to open
            this.WaitHandle(this._channelOpen);

            //  Start reading data from the port and send to channel
            EventWaitHandle readerTaskError = new AutoResetEvent(false);

            var readerTaskCompleted = new ManualResetEvent(false);
            Exception exception = null;

            this.ExecuteThread(() =>
            {
                try
                {
                    var buffer = new byte[this.PacketSize - 9];

                    while (this._socket.Connected || this.IsConnected)
                    {
                        try
                        {

                            var read = 0;
                            this.InternalSocketReceive(buffer, ref read);
                            if (read > 0)
                            {
                                this.SendMessage(new ChannelDataMessage(this.RemoteChannelNumber, buffer.Take(read).ToArray()));
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (SocketException exp)
                        {
                            if (exp.SocketErrorCode == SocketError.WouldBlock ||
                                exp.SocketErrorCode == SocketError.IOPending ||
                                exp.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                            {
                                // socket buffer is probably empty, wait and try again
                                Thread.Sleep(30);
                            }
                            else if (exp.SocketErrorCode == SocketError.ConnectionAborted)
                            {
                                break;
                            }
                            else
                                throw;  // throw any other error
                        }
                    }
                }
                catch (Exception exp)
                {
                    readerTaskError.Set();
                    exception = exp;
                }
                finally
                {
                    readerTaskCompleted.Set();
                }
            });

            //  Channel was open and we MUST receive EOF notification, 
            //  data transfer can take longer then connection specified timeout
            System.Threading.WaitHandle.WaitAny(new WaitHandle[] { this._channelEof, readerTaskError });

            this._socket.Dispose();
            this._socket = null;

            //  Wait for task to finish and will throw any errors if any
            readerTaskCompleted.WaitOne();

            if (exception != null)
                throw exception;
        }

        /// <summary>
        /// Called when channel data is received.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnData(byte[] data)
        {
            base.OnData(data);

            this.InternalSocketSend(data);
        }

        /// <summary>
        /// Called when channel is opened by the server.
        /// </summary>
        /// <param name="remoteChannelNumber">The remote channel number.</param>
        /// <param name="initialWindowSize">Initial size of the window.</param>
        /// <param name="maximumPacketSize">Maximum size of the packet.</param>
        protected override void OnOpenConfirmation(uint remoteChannelNumber, uint initialWindowSize, uint maximumPacketSize)
        {
            base.OnOpenConfirmation(remoteChannelNumber, initialWindowSize, maximumPacketSize);

            this._channelOpen.Set();
        }

        /// <summary>
        /// Called when channel has no more data to receive.
        /// </summary>
        protected override void OnEof()
        {
            base.OnEof();

            this._channelEof.Set();
        }

        partial void ExecuteThread(Action action);

        partial void InternalSocketReceive(byte[] buffer, ref int read);

        partial void InternalSocketSend(byte[] data);

        protected override void Dispose(bool disposing)
        {
            if (this._socket != null)
            {
                this._socket.Dispose();
                this._socket = null;
            }

            if (this._channelEof != null)
            {
                this._channelEof.Dispose();
                this._channelEof = null;
            }

            if (this._channelOpen != null)
            {
                this._channelOpen.Dispose();
                this._channelOpen = null;
            }

            if (this._channelData != null)
            {
                this._channelData.Dispose();
                this._channelData = null;
            }

            base.Dispose(disposing);
        }
    }
}
