﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Security.Cryptography.Ciphers.Paddings
{
    /// <summary>
    /// Implements PKCS7 cipher padding
    /// </summary>
    public class PKCS7Padding : CipherPadding
    {
        /// <summary>
        /// Transforms the specified input.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public override byte[] Pad(int blockSize, byte[] input)
        {
            var numOfPaddedBytes = blockSize - (input.Length % blockSize);

            var output = new byte[input.Length + numOfPaddedBytes];
            Buffer.BlockCopy(input, 0, output, 0, input.Length);
            for (int i = 0; i < numOfPaddedBytes; i++)
            {
                output[input.Length + i] = output[input.Length - 1];
            }

            return output;
        }
    }
}
