﻿using System;
using System.Collections.Generic;

namespace Renci.SshNet.Compression
{
    /// <summary>
    /// Represents "zlib" compression implementation
    /// </summary>
    internal class Zlib : Compressor
    {
        private bool _active;

        /// <summary>
        /// Gets algorithm name.
        /// </summary>
        public override string Name
        {
            get { return "zlib"; }
        }

        /// <summary>
        /// Initializes the algorithm
        /// </summary>
        /// <param name="session">The session.</param>
        public override void Init(Session session)
        {
            base.Init(session);

            session.UserAuthenticationSuccessReceived += Session_UserAuthenticationSuccessReceived;
        }

        private void Session_UserAuthenticationSuccessReceived(object sender, MessageEventArgs<Messages.Authentication.SuccessMessage> e)
        {
            this._active = true;
            this.Session.UserAuthenticationSuccessReceived -= Session_UserAuthenticationSuccessReceived;
        }

        /// <summary>
        /// Compresses the specified data.
        /// </summary>
        /// <param name="data">Data to compress.</param>
        /// <returns>
        /// Compressed data
        /// </returns>
        public override byte[] Compress(byte[] data)
        {
            if (!this._active)
            {
                return data;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Decompresses the specified data.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <returns>
        /// Decompressed data.
        /// </returns>
        public override byte[] Decompress(byte[] data)
        {
            if (!this._active)
            {
                return data;
            }

            throw new NotImplementedException();
        }
    }
}