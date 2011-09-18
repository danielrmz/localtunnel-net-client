﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Security.Cryptography;

namespace Renci.SshNet
{
    /// <summary>
    /// Holds information about key size and cipher to use
    /// </summary>
    public class CipherInfo
    {
        /// <summary>
        /// Gets the size of the key.
        /// </summary>
        /// <value>
        /// The size of the key.
        /// </value>
        public int KeySize { get; private set; }

        /// <summary>
        /// Gets the cipher.
        /// </summary>
        public Func<byte[], byte[], BlockCipher> Cipher { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CipherInfo"/> class.
        /// </summary>
        /// <param name="keySize">Size of the key.</param>
        /// <param name="cipher">The cipher.</param>
        public CipherInfo(int keySize, Func<byte[], byte[], BlockCipher> cipher)
        {
            this.KeySize = keySize;
            this.Cipher = (key, iv) => (cipher(key.Take(this.KeySize / 8).ToArray(), iv));
        }
    }
}
