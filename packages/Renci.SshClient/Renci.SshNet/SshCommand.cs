﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Renci.SshNet.Channels;
using Renci.SshNet.Common;
using Renci.SshNet.Messages;
using Renci.SshNet.Messages.Connection;
using Renci.SshNet.Messages.Transport;
using System.Globalization;

namespace Renci.SshNet
{
    /// <summary>
    /// Represents SSH command that can be executed.
    /// </summary>
    public partial class SshCommand : IDisposable
    {
        private Encoding _encoding;

        private Session _session;

        private ChannelSession _channel;

        private CommandAsyncResult _asyncResult;

        private AsyncCallback _callback;

        private EventWaitHandle _sessionErrorOccuredWaitHandle = new AutoResetEvent(false);

        private Exception _exception;

        private bool _hasError;

        private object _endExecuteLock = new object();

        /// <summary>
        /// Gets the command text.
        /// </summary>
        public string CommandText { get; private set; }

        /// <summary>
        /// Gets or sets the command timeout.
        /// </summary>
        /// <value>
        /// The command timeout.
        /// </value>
        public TimeSpan CommandTimeout { get; set; }

        /// <summary>
        /// Gets the command exit status.
        /// </summary>
        public int ExitStatus { get; private set; }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        public Stream OutputStream { get; private set; }

        /// <summary>
        /// Gets the extended output stream.
        /// </summary>
        public Stream ExtendedOutputStream { get; private set; }

        private StringBuilder _result;
        /// <summary>
        /// Gets the command execution result.
        /// </summary>
        public string Result
        {
            get
            {
                if (this._result == null)
                {
                    this._result = new StringBuilder();
                }

                if (this.OutputStream != null && this.OutputStream.Length > 0)
                {
                    using (var sr = new StreamReader(this.OutputStream, this._encoding))
                    {
                        this._result.Append(sr.ReadToEnd());
                    }
                }

                return this._result.ToString();
            }
        }

        private StringBuilder _error;
        /// <summary>
        /// Gets the command execution error.
        /// </summary>
        public string Error
        {
            get
            {
                if (this._hasError)
                {
                    if (this._error == null)
                    {
                        this._error = new StringBuilder();
                    }

                    if (this.ExtendedOutputStream != null && this.ExtendedOutputStream.Length > 0)
                    {
                        using (var sr = new StreamReader(this.ExtendedOutputStream, this._encoding))
                        {
                            this._error.Append(sr.ReadToEnd());
                        }
                    }

                    return this._error.ToString();
                }
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SshCommand"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="encoding">The encoding.</param>
        internal SshCommand(Session session, string commandText, Encoding encoding)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            this._encoding = encoding;
            this._session = session;
            this.CommandText = commandText;
            this.CommandTimeout = new TimeSpan(0, 0, 0, 0, -1);

            this._session.Disconnected += Session_Disconnected;
            this._session.ErrorOccured += Session_ErrorOccured;
        }

        /// <summary>
        /// Begins an asynchronous command execution.
        /// </summary>
        /// <param name="callback">An optional asynchronous callback, to be called when the command execution is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An <see cref="System.IAsyncResult"/> that represents the asynchronous command execution, which could still be pending.</returns>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException">Client is not connected.</exception>
        /// <exception cref="Renci.SshNet.Common.SshOperationTimeoutException">Operation has timed out.</exception>
        /// <exception cref="InvalidOperationException">Asynchronous operation is already in progress.</exception>
        /// <exception cref="SshException">Invalid operation.</exception>
        /// <exception cref="ArgumentException">CommandText property is empty.</exception>
        public IAsyncResult BeginExecute(AsyncCallback callback, object state)
        {
            //  Prevent from executing BeginExecute before calling EndExecute
            if (this._asyncResult != null)
            {
                throw new InvalidOperationException("Asynchronous operation is already in progress.");
            }

            //  Create new AsyncResult object
            this._asyncResult = new CommandAsyncResult(this)
            {
                AsyncWaitHandle = new ManualResetEvent(false),
                IsCompleted = false,
                AsyncState = state,
            };

                        //  When command re-executed again, create a new channel
            if (this._channel != null)
            {
                throw new SshException("Invalid operation.");
            }

            this.CreateChannel();

            if (string.IsNullOrEmpty(this.CommandText))
                throw new ArgumentException("CommandText property is empty.");

            this._callback = callback;

            this._channel.Open();

            //  Send channel command request
            this._channel.SendExecRequest(this.CommandText);

            return _asyncResult;
        }

        /// <summary>
        /// Begins an asynchronous command execution.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="callback">An optional asynchronous callback, to be called when the command execution is complete.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An <see cref="System.IAsyncResult"/> that represents the asynchronous command execution, which could still be pending.</returns>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException">Client is not connected.</exception>
        /// <exception cref="Renci.SshNet.Common.SshOperationTimeoutException">Operation has timed out.</exception>
        public IAsyncResult BeginExecute(string commandText, AsyncCallback callback, object state)
        {
            this.CommandText = commandText;
            return BeginExecute(callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous command execution to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Either the IAsyncResult object did not come from the corresponding async method on this type, or EndExecute was called multiple times with the same IAsyncResult.</exception>
        public string EndExecute(IAsyncResult asyncResult)
        {
            if (this._asyncResult == asyncResult && this._asyncResult != null)
            {
                lock (this._endExecuteLock)
                {
                    if (this._asyncResult != null)
                    {
                        //  Make sure that operation completed if not wait for it to finish
                        this.WaitHandle(this._asyncResult.AsyncWaitHandle);

                        this._channel.Close();

                        this._channel = null;

                        this._asyncResult = null;

                        return this.Result;
                    }
                }
            }

            throw new ArgumentException("Either the IAsyncResult object did not come from the corresponding async method on this type, or EndExecute was called multiple times with the same IAsyncResult.");
        }

        /// <summary>
        /// Executes command specified by <see cref="CommandText"/> property.
        /// </summary>
        /// <returns>Command execution result</returns>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException">Client is not connected.</exception>
        /// <exception cref="Renci.SshNet.Common.SshOperationTimeoutException">Operation has timed out.</exception>
        public string Execute()
        {
            return this.EndExecute(this.BeginExecute(null, null));
        }

        /// <summary>
        /// Cancels command execution in asynchronous scenarios. CURRENTLY NOT IMPLEMENTED.
        /// </summary>
        //public void Cancel()
        //{
        //    if (this._channel != null && this._channel.IsOpen)
        //    {
        //        //this._channel.SendData(Encoding.ASCII.GetBytes("~."));
        //        this._channel.SendExecRequest("\0x03");

        //        //this._channel.SendSignalRequest("ABRT");
        //        //this._channel.SendSignalRequest("ALRM");
        //        //this._channel.SendSignalRequest("FPE");
        //        //this._channel.SendSignalRequest("HUP");
        //        //this._channel.SendSignalRequest("ILL");
        //        //this._channel.SendSignalRequest("INT");
        //        //this._channel.SendSignalRequest("PIPE");
        //        //this._channel.SendSignalRequest("QUIT");
        //        //this._channel.SendSignalRequest("SEGV");
        //        //this._channel.SendSignalRequest("TERM");
        //        //this._channel.SendSignalRequest("SEGV");
        //        //this._channel.SendSignalRequest("USR1");
        //        //this._channel.SendSignalRequest("USR2");
        //    }
        //}

        /// <summary>
        /// Executes the specified command text.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <returns>Command execution result</returns>
        /// <exception cref="Renci.SshNet.Common.SshConnectionException">Client is not connected.</exception>
        /// <exception cref="Renci.SshNet.Common.SshOperationTimeoutException">Operation has timed out.</exception>
        public string Execute(string commandText)
        {
            this.CommandText = commandText;
            return this.Execute();
        }

        private void CreateChannel()
        {
            this._channel = this._session.CreateChannel<ChannelSession>();
            this._channel.DataReceived += Channel_DataReceived;
            this._channel.ExtendedDataReceived += Channel_ExtendedDataReceived;
            this._channel.RequestReceived += Channel_RequestReceived;
            this._channel.Closed += Channel_Closed;

            //  Dispose of streams if already exists
            if (this.OutputStream != null)
            {
                this.OutputStream.Dispose();
                this.OutputStream = null;
            }

            if (this.ExtendedOutputStream != null)
            {
                this.ExtendedOutputStream.Dispose();
                this.ExtendedOutputStream = null;
            }

            //  Initialize output streams and StringBuilders
            this.OutputStream = new PipeStream();
            this.ExtendedOutputStream = new PipeStream();

            this._result = null;
            this._error = null;
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            this._exception = new SshConnectionException("An established connection was aborted by the software in your host machine.", DisconnectReason.ConnectionLost);

            this._sessionErrorOccuredWaitHandle.Set();
        }

        private void Session_ErrorOccured(object sender, ExceptionEventArgs e)
        {
            this._exception = e.Exception;

            this._sessionErrorOccuredWaitHandle.Set();
        }

        private void Channel_Closed(object sender, Common.ChannelEventArgs e)
        {
            if (this.OutputStream != null)
            {
                this.OutputStream.Flush();
            }

            if (this.ExtendedOutputStream != null)
            {
                this.ExtendedOutputStream.Flush();
            }

            this._asyncResult.IsCompleted = true;

            if (this._callback != null)
            {
                //  Execute callback on different thread                
                this.ExecuteThread(() => { this._callback(this._asyncResult); });
            }
            ((EventWaitHandle)this._asyncResult.AsyncWaitHandle).Set();
        }

        private void Channel_RequestReceived(object sender, Common.ChannelRequestEventArgs e)
        {
            Message replyMessage = new ChannelFailureMessage(this._channel.LocalChannelNumber);

            if (e.Info is ExitStatusRequestInfo)
            {
                ExitStatusRequestInfo exitStatusInfo = e.Info as ExitStatusRequestInfo;

                this.ExitStatus = (int)exitStatusInfo.ExitStatus;

                replyMessage = new ChannelSuccessMessage(this._channel.LocalChannelNumber);
            }

            if (e.Info.WantReply)
            {
                this._session.SendMessage(replyMessage);
            }
        }

        private void Channel_ExtendedDataReceived(object sender, Common.ChannelDataEventArgs e)
        {
            if (this.ExtendedOutputStream != null)
            {
                this.ExtendedOutputStream.Write(e.Data, 0, e.Data.Length);
                this.ExtendedOutputStream.Flush();
            }

            if (e.DataTypeCode == 1)
            {
                this._hasError = true;
            }
        }

        private void Channel_DataReceived(object sender, Common.ChannelDataEventArgs e)
        {
            if (this.OutputStream != null)
            {
                this.OutputStream.Write(e.Data, 0, e.Data.Length);
                //this._outputSteamWriter.Write(this._encoding.GetString(e.Data, 0, e.Data.Length));
                this.OutputStream.Flush();
            }

            if (this._asyncResult != null)
            {
                lock (this._asyncResult)
                {
                    this._asyncResult.BytesReceived += e.Data.Length;
                }
            }
        }

        /// <exception cref="SshOperationTimeoutException">Command '{0}' has timed out.</exception>
        /// <remarks>The actual command will be included in the exception message.</remarks>
        private void WaitHandle(WaitHandle waitHandle)
        {
            var waitHandles = new WaitHandle[]
                {
                    this._sessionErrorOccuredWaitHandle,
                    waitHandle,
                };

            var index = EventWaitHandle.WaitAny(waitHandles, this.CommandTimeout);

            if (index < 1)
            {
                throw this._exception;
            }
            else if (index > 1)
            {
                //  throw time out error
                throw new SshOperationTimeoutException(string.Format(CultureInfo.CurrentCulture, "Command '{0}' has timed out.", this.CommandText));
            }
        }
        
        partial void ExecuteThread(Action action);

        #region IDisposable Members

        private bool _isDisposed = false;

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
            if (!this._isDisposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged ResourceMessages.
                if (disposing)
                {

                    this._session.Disconnected -= Session_Disconnected;
                    this._session.ErrorOccured -= Session_ErrorOccured;

                    // Dispose managed ResourceMessages.
                    if (this.OutputStream != null)
                    {
                        this.OutputStream.Dispose();
                        this.OutputStream = null;
                    }

                    // Dispose managed ResourceMessages.
                    if (this.ExtendedOutputStream != null)
                    {
                        this.ExtendedOutputStream.Dispose();
                        this.ExtendedOutputStream = null;
                    }

                    // Dispose managed ResourceMessages.
                    if (this._sessionErrorOccuredWaitHandle != null)
                    {
                        this._sessionErrorOccuredWaitHandle.Dispose();
                        this._sessionErrorOccuredWaitHandle = null;
                    }

                    // Dispose managed ResourceMessages.
                    if (this._channel != null)
                    {
                        this._channel.DataReceived -= Channel_DataReceived;
                        this._channel.ExtendedDataReceived -= Channel_ExtendedDataReceived;
                        this._channel.RequestReceived -= Channel_RequestReceived;
                        this._channel.Closed -= Channel_Closed;

                        this._channel.Dispose();
                        this._channel = null;
                    }
                }

                // Note disposing has been done.
                this._isDisposed = true;
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SshCommand"/> is reclaimed by garbage collection.
        /// </summary>
        ~SshCommand()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
