﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Common;
using Renci.SshNet.Security.Cryptography;

namespace Renci.SshNet.Security
{
    /// <summary>
    /// Base class for SSH host algorithms.
    /// </summary>
    public abstract class HostAlgorithm
    {
        /// <summary>
        /// Gets the host key name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the host key data.
        /// </summary>
        public abstract byte[] Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostAlgorithm"/> class.
        /// </summary>
        /// <param name="name">The host key name.</param>
        public HostAlgorithm(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Signs the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public abstract byte[] Sign(byte[] data);

        /// <summary>
        /// Verifies the signature.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="signature">The signature.</param>
        /// <returns></returns>
        public abstract bool VerifySignature(byte[] data, byte[] signature);
    }
}
