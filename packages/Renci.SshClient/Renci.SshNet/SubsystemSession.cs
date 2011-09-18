﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Renci.SshNet.Channels;
using Renci.SshNet.Common;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Renci.SshNet.Sftp.Responses;
using Renci.SshNet.Sftp.Requests;
using Renci.SshNet.Messages.Connection;

namespace Renci.SshNet.Sftp
{
    internal abstract class SubsystemSession : IDisposable
    {
        private Session _session;

        private string _subsystemName;

        private ChannelSession _channel;

        private Exception _exception;

        private EventWaitHandle _errorOccuredWaitHandle = new AutoResetEvent(false);

        protected TimeSpan _operationTimeout;

        public event EventHandler<ExceptionEventArgs> ErrorOccured;

        protected uint ChannelNumber
        {
            get
            {
                return this._channel.RemoteChannelNumber;
            }
        }

        #region SFTP messages

        //internal event EventHandler<MessageEventArgs<StatusMessage>> StatusMessageReceived;

        //internal event EventHandler<MessageEventArgs<DataMessage>> DataMessageReceived;

        //internal event EventHandler<MessageEventArgs<HandleMessage>> HandleMessageReceived;

        //internal event EventHandler<MessageEventArgs<NameMessage>> NameMessageReceived;

        //internal event EventHandler<MessageEventArgs<AttributesMessage>> AttributesMessageReceived;

        #endregion
        
        /// <summary>
        /// Initializes a new instance of the SubsystemSession class.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> or <paramref name="subsystemName"/> is null.</exception>
        public SubsystemSession(Session session, string subsystemName, TimeSpan operationTimeout)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            if (subsystemName == null)
                throw new ArgumentNullException("subsystemName");
                
            this._session = session;
            this._subsystemName = subsystemName;
            this._operationTimeout = operationTimeout;
        }

        public void Connect()
        {
            this._channel = this._session.CreateChannel<ChannelSession>();

            this._session.ErrorOccured += Session_ErrorOccured;
            this._session.Disconnected += Session_Disconnected;
            this._channel.DataReceived += Channel_DataReceived;

            this._channel.Open();

            this._channel.SendSubsystemRequest(_subsystemName);

            this.OnChannelOpen();
        }

        public void Disconnect()
        {
            this.Dispose();
        }

        //public void SendData(byte[] data)
        //{
        //    this._session.SendMessage(new ChannelDataMessage(this._channel.RemoteChannelNumber, data));
        //}

        public void SendData(ChannelDataMessage message)
        {
            this._session.SendMessage(message);
        }

        protected abstract void OnChannelOpen();

        protected abstract void OnDataReceived(uint dataTypeCode, byte[] data);

        protected void RaiseError(Exception error)
        {
            this._exception = error;

            this._errorOccuredWaitHandle.Set();

            if (this.ErrorOccured != null)
            {
                this.ErrorOccured(this, new ExceptionEventArgs(error));
            }
        }

        private void Channel_DataReceived(object sender, Common.ChannelDataEventArgs e)
        {
            this.OnDataReceived(e.DataTypeCode, e.Data);
        }

        internal void WaitHandle(WaitHandle waitHandle, TimeSpan operationTimeout)
        {
            var waitHandles = new WaitHandle[]
                {
                    this._errorOccuredWaitHandle,
                    waitHandle,
                };

            var index = EventWaitHandle.WaitAny(waitHandles, operationTimeout);

            if (index < 1)
            {
                throw this._exception;
            }
            else if (index > 1)
            {
                //  throw time out error
                throw new SshOperationTimeoutException(string.Format(CultureInfo.CurrentCulture, "Sftp operation has timed out."));
            }
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            this.RaiseError(new SshException("Connection was lost"));
        }

        private void Session_ErrorOccured(object sender, ExceptionEventArgs e)
        {
            this.RaiseError(e.Exception);
        }

        #region IDisposable Members

        private bool _isDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._isDisposed)
            {
                if (this._channel != null)
                {
                    this._channel.DataReceived -= Channel_DataReceived;

                    this._channel.Dispose();
                    this._channel = null;
                }

                this._session.ErrorOccured -= Session_ErrorOccured;
                this._session.Disconnected -= Session_Disconnected;

                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this._errorOccuredWaitHandle != null)
                    {
                        this._errorOccuredWaitHandle.Dispose();
                        this._errorOccuredWaitHandle = null;
                    }
                }

                // Note disposing has been done.
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="SftpSession"/> is reclaimed by garbage collection.
        /// </summary>
        ~SubsystemSession()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
