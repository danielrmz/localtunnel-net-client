﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Renci.SshNet.Security.Cryptography
{
    /// <summary>
    /// Provides HMAC algorithm implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HMac<T> : KeyedHashAlgorithm where T : HashAlgorithm, new()
    {
        private HashAlgorithm _hash;
        private bool _isHashing;
        private byte[] _innerPadding;
        private byte[] _outerPadding;
        private byte[] _key;

        /// <summary>
        /// Gets the size of the block.
        /// </summary>
        /// <value>
        /// The size of the block.
        /// </value>
        protected int BlockSize
        {
            get
            {
                return this._hash.InputBlockSize;
            }
        }

        /// <summary>
        /// Rfc 2104.
        /// </summary>
        /// <param name="key">The key.</param>
        public HMac(byte[] key)
        {
            // Create the hash algorithms.
            this._hash = new T();

            this.HashSizeValue = this._hash.HashSize;

            this._key = key;

            this.InternalInitialize();
        }

        /// <summary>
        /// 
        /// </summary>
        public override byte[] Key
        {
            get
            {
                return (byte[])KeyValue.Clone();
            }
            set
            {
                this.SetKey(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Initialize()
        {
            this.InternalInitialize();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rgb"></param>
        /// <param name="ib"></param>
        /// <param name="cb"></param>
        protected override void HashCore(byte[] rgb, int ib, int cb)
        {
            if (!this._isHashing)
            {
                this._hash.TransformBlock(this._innerPadding, 0, this.BlockSize, this._innerPadding, 0);
                this._isHashing = true;
            }
            this._hash.TransformBlock(rgb, ib, cb, rgb, ib);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override byte[] HashFinal()
        {
            if (!this._isHashing)
            {
                this._hash.TransformBlock(this._innerPadding, 0, 64, this._innerPadding, 0);
                this._isHashing = true;
            }

            // Finalize the original hash.
            this._hash.TransformFinalBlock(new byte[0], 0, 0);

            var hashValue = this._hash.Hash;

            // Write the outer array.
            this._hash.TransformBlock(this._outerPadding, 0, this.BlockSize, this._outerPadding, 0);

            // Write the inner hash and finalize the hash.            
            this._hash.TransformFinalBlock(hashValue, 0, hashValue.Length);

            this._isHashing = false;

            return this._hash.Hash;
        }

        private void InternalInitialize()
        {
            this._isHashing = false;
            this.SetKey(this._key);
        }

        private void SetKey(byte[] value)
        {
            if (this._isHashing)
            {
                throw new Exception("Cannot change key during hash operation");
            }
            if (value.Length > this.BlockSize)
            {
                this.KeyValue = this._hash.ComputeHash(value);
                // No need to call Initialize, ComputeHash does it automatically.
            }
            else
            {
                this.KeyValue = value.Clone() as byte[];
            }

            this._innerPadding = new byte[this.BlockSize];
            this._outerPadding = new byte[this.BlockSize];

            // Compute inner and outer padding.
            int i = 0;
            for (i = 0; i < 64; i++)
            {
                this._innerPadding[i] = 0x36;
                this._outerPadding[i] = 0x5C;
            }
            for (i = 0; i < this.KeyValue.Length; i++)
            {
                this._innerPadding[i] ^= this.KeyValue[i];
                this._outerPadding[i] ^= this.KeyValue[i];
            }
        }
    }
}
