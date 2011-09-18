﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Security.Cryptography.Ciphers
{
    /// <summary>
    /// Base class for cipher padding implementations
    /// </summary>
    public abstract class CipherPadding
    {
        /// <summary>
        /// Pads specified input to match block size.
        /// </summary>
        /// <param name="blockSize">Size of the block.</param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public abstract byte[] Pad(int blockSize, byte[] input);
    }
}
