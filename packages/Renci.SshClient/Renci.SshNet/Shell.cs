﻿using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Renci.SshNet.Channels;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Connection;

namespace Renci.SshNet
{
    /// <summary>
    /// Represents instance of the SSH shell object
    /// </summary>
    public partial class Shell : IDisposable
    {
        private readonly Session _session;

        private ChannelSession _channel;

        private EventWaitHandle _channelClosedWaitHandle;

        private Stream _input;

        private string _terminalName;

        private uint _columns;

        private uint _rows;

        private uint _width;

        private uint _height;

        private string _terminalMode;

        private EventWaitHandle _dataReaderTaskCompleted;

        private Stream _outputStream;

        private Stream _extendedOutputStream;

        private int _bufferSize;

        /// <summary>
        /// Gets a value indicating whether this shell is started.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if started is started; otherwise, <c>false</c>.
        /// </value>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Occurs when shell is starting.
        /// </summary>
        public event EventHandler<EventArgs> Starting;

        /// <summary>
        /// Occurs when shell is started.
        /// </summary>
        public event EventHandler<EventArgs> Started;

        /// <summary>
        /// Occurs when shell is stopping.
        /// </summary>
        public event EventHandler<EventArgs> Stopping;

        /// <summary>
        /// Occurs when shell is stopped.
        /// </summary>
        public event EventHandler<EventArgs> Stopped;

        /// <summary>
        /// Occurs when an error occurred.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ErrorOccurred;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shell"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        /// <param name="extendedOutput">The extended output.</param>
        /// <param name="terminalName">Name of the terminal.</param>
        /// <param name="columns">The columns.</param>
        /// <param name="rows">The rows.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="terminalMode">The terminal mode.</param>
        /// <param name="bufferSize">Size of the buffer for output stream.</param>
        internal Shell(Session session, Stream input, Stream output, Stream extendedOutput, string terminalName, uint columns, uint rows, uint width, uint height, string terminalMode, int bufferSize)
        {
            this._session = session;
            this._input = input;
            this._outputStream = output;
            this._extendedOutputStream = extendedOutput;
            this._terminalName = terminalName;
            this._columns = columns;
            this._rows = rows;
            this._width = width;
            this._height = height;
            this._terminalMode = terminalMode;
            this._bufferSize = bufferSize;
        }

        /// <summary>
        /// Starts this shell.
        /// </summary>
        /// <exception cref="SshException">Shell is started.</exception>
        public void Start()
        {
            if (this.IsStarted)
            {
                throw new SshException("Shell is started.");
            }

            if (this.Starting != null)
            {
                this.Starting(this, new EventArgs());
            }

            this._channel = this._session.CreateChannel<ChannelSession>();
            this._channel.DataReceived += Channel_DataReceived;
            this._channel.ExtendedDataReceived += Channel_ExtendedDataReceived;
            this._channel.Closed += Channel_Closed;
            this._session.Disconnected += Session_Disconnected;
            this._session.ErrorOccured += Session_ErrorOccured;

            this._channel.Open();
            this._channel.SendPseudoTerminalRequest(this._terminalName, this._columns, this._rows, this._width, this._height, this._terminalMode);
            this._channel.SendShellRequest();

            this._channelClosedWaitHandle = new AutoResetEvent(false);

            //  Start input stream listener
            this._dataReaderTaskCompleted = new ManualResetEvent(false);
            this.ExecuteThread(() =>
            {
                try
                {
                    var buffer = new byte[this._bufferSize];

                    while (this._channel.IsOpen)
                    {
                        var asyncResult = this._input.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult result)
                        {
                            //  If input stream is closed and disposed already dont finish reading the stream
                            if (this._input == null)
                                return;

                            var read = this._input.EndRead(result);
                            if (read > 0)
                            {
                                this._session.SendMessage(new ChannelDataMessage(this._channel.RemoteChannelNumber, buffer.Take(read).ToArray()));
                            }

                        }, null);

                        EventWaitHandle.WaitAny(new WaitHandle[] { asyncResult.AsyncWaitHandle, this._channelClosedWaitHandle });

                        if (asyncResult.IsCompleted)
                            continue;
                        else
                            break;
                    }
                }
                catch (Exception exp)
                {
                    this.RaiseError(new ExceptionEventArgs(exp));
                }
                finally
                {
                    this._dataReaderTaskCompleted.Set();
                }
            });

            this.IsStarted = true;

            if (this.Started != null)
            {
                this.Started(this, new EventArgs());
            }
        }

        /// <summary>
        /// Stops this shell.
        /// </summary>
        /// <exception cref="SshException">Shell is not started.</exception>
        public void Stop()
        {
            if (!this.IsStarted)
            {
                throw new SshException("Shell is not started.");
            }

            //  If channel is open then close it to cause Channel_Closed method to be called
            if (this._channel != null && this._channel.IsOpen)
            {
                this._channel.Close();
            }
        }

        private void Session_ErrorOccured(object sender, ExceptionEventArgs e)
        {
            this.RaiseError(e);
        }

        private void RaiseError(ExceptionEventArgs e)
        {
            if (this.ErrorOccurred != null)
            {
                this.ErrorOccurred(this, e);
            }
        }

        private void Session_Disconnected(object sender, System.EventArgs e)
        {
            this.Stop();
        }

        private void Channel_ExtendedDataReceived(object sender, Common.ChannelDataEventArgs e)
        {
            if (this._extendedOutputStream != null)
            {
                this._extendedOutputStream.Write(e.Data, 0, e.Data.Length);
            }
        }

        private void Channel_DataReceived(object sender, Common.ChannelDataEventArgs e)
        {
            if (this._outputStream != null)
            {
                this._outputStream.Write(e.Data, 0, e.Data.Length);
            }
        }

        private void Channel_Closed(object sender, Common.ChannelEventArgs e)
        {
            if (this.Stopping != null)
            {
                //  Handle event on different thread
                this.ExecuteThread(() => { this.Stopping(this, new EventArgs()); });
            }

            if (this._channel.IsOpen)
                this._channel.Close();

            this._channelClosedWaitHandle.Set();

            this._input.Dispose();
            this._input = null;

            this._dataReaderTaskCompleted.WaitOne(this._session.ConnectionInfo.Timeout);
            this._dataReaderTaskCompleted.Dispose();
            this._dataReaderTaskCompleted = null;

            this._channel.DataReceived -= Channel_DataReceived;
            this._channel.ExtendedDataReceived -= Channel_ExtendedDataReceived;
            this._channel.Closed -= Channel_Closed;
            this._session.Disconnected -= Session_Disconnected;
            this._session.ErrorOccured -= Session_ErrorOccured;

            if (this.Stopped != null)
            {
                //  Handle event on different thread
                this.ExecuteThread(() => { this.Stopped(this, new EventArgs()); });
            }

            this._channel = null;
        }

        partial void ExecuteThread(Action action);

        #region IDisposable Members

        private bool _disposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged ResourceMessages.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged ResourceMessages.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged ResourceMessages.
                if (disposing)
                {
                    if (this._channelClosedWaitHandle != null)
                    {
                        this._channelClosedWaitHandle.Dispose();
                        this._channelClosedWaitHandle = null;
                    }

                    if (this._dataReaderTaskCompleted != null)
                    {
                        this._dataReaderTaskCompleted.Dispose();
                        this._dataReaderTaskCompleted = null;
                    }
                }

                // Note disposing has been done.
                this._disposed = true;
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Session"/> is reclaimed by garbage collection.
        /// </summary>
        ~Shell()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion

    }
}
