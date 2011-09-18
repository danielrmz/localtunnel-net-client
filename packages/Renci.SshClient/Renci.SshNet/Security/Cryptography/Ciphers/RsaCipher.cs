﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Renci.SshNet.Common;

namespace Renci.SshNet.Security.Cryptography.Ciphers
{
    /// <summary>
    /// Implements RSA cipher algorithm.
    /// </summary>
    public class RsaCipher : AsymmetricCipher
    {
        private static RNGCryptoServiceProvider _randomizer = new System.Security.Cryptography.RNGCryptoServiceProvider();

        private bool _isPrivate;

        private RsaKey _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsaCipher"/> class.
        /// </summary>
        /// <param name="key">The RSA key.</param>
        public RsaCipher(RsaKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            this._key = key;

            this._isPrivate = !this._key.D.IsZero;
        }

        /// <summary>
        /// Encrypts the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public override byte[] Encrypt(byte[] data)
        {
            //  Calculate signature
            var paddedBlock = new byte[this._key.Modulus.BitLength / 8 - 1];
            paddedBlock[0] = 0x01;
            for (int i = 1; i < paddedBlock.Length - data.Length - 1; i++)
            {
                paddedBlock[i] = 0xFF;
            }

            Buffer.BlockCopy(data, 0, paddedBlock, paddedBlock.Length - data.Length, data.Length);

            return this.Transform(paddedBlock);
        }

        /// <summary>
        /// Decrypts the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown when decrypted block type is not supported.</exception>
        public override byte[] Decrypt(byte[] data)
        {
            var paddedBlock = this.Transform(data);

            if (paddedBlock[0] != 1 && paddedBlock[0] != 2)
                throw new NotSupportedException("Only block type 01 or 02 are supported.");

            var position = 1;
            while (position < paddedBlock.Length && paddedBlock[position] != 0)
                position++;
            position++;

            var result = new byte[paddedBlock.Length - position];

            Buffer.BlockCopy(paddedBlock, position, result, 0, result.Length);

            return result;
        }

        private byte[] Transform(byte[] data)
        {
            var bytes = new List<byte>(data.Reverse());
            bytes.Add(0);

            var input = new BigInteger(bytes.ToArray());

            BigInteger result;

            if (this._isPrivate)
            {
                BigInteger random = BigInteger.One;

                var max = this._key.Modulus - 1;
                
                var bitLength = this._key.Modulus.BitLength;

                var bytesArray = new byte[bitLength / 8 + (((bitLength % 8) > 0) ? 1 : 0)];

                while (random <= BigInteger.One || random >= max)
                {
                    _randomizer.GetBytes(bytesArray);

                    bytesArray[bytesArray.Length - 1] = (byte)(bytesArray[bytesArray.Length - 1] & 0x7F);   //  Ensure not a negative value
                    random = new BigInteger(bytesArray.Reverse().ToArray());
                }

                BigInteger blindedInput = BigInteger.PositiveMod((BigInteger.ModPow(random, this._key.Exponent, this._key.Modulus) * input), this._key.Modulus);

                // mP = ((input Mod p) ^ dP)) Mod p
                var mP = BigInteger.ModPow((blindedInput % this._key.P), this._key.DP, this._key.P);

                // mQ = ((input Mod q) ^ dQ)) Mod q
                var mQ = BigInteger.ModPow((blindedInput % this._key.Q), this._key.DQ, this._key.Q);

                var h = BigInteger.PositiveMod(((mP - mQ) * this._key.InverseQ), this._key.P);

                var m = h * this._key.Q + mQ;

                BigInteger rInv = BigInteger.ModInverse(random, this._key.Modulus);

                result = BigInteger.PositiveMod((m * rInv), this._key.Modulus);
            }
            else
            {
                result = BigInteger.ModPow(input, this._key.Exponent, this._key.Modulus);
            }
            
            return result.ToByteArray().Reverse().ToArray();
        }
    }
}
