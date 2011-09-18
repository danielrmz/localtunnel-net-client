﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Common;
using Renci.SshNet.Security.Cryptography;

namespace Renci.SshNet.Security
{
    /// <summary>
    /// Implements key support for host algorithm.
    /// </summary>
    public class KeyHostAlgorithm : HostAlgorithm
    {
        private Key _key;

        /// <summary>
        /// Gets the public key data.
        /// </summary>
        public override byte[] Data
        {
            get
            {
                return new SshKeyData(this.Name, this._key.Public).GetBytes();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyHostAlgorithm"/> class.
        /// </summary>
        /// <param name="name">Host key name.</param>
        /// <param name="key">Host key.</param>
        public KeyHostAlgorithm(string name, Key key)
            : base(name)
        {
            this._key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HostAlgorithm"/> class.
        /// </summary>
        /// <param name="name">Host key name.</param>
        /// <param name="key">Host key.</param>
        /// <param name="data">Host key encoded data.</param>
        public KeyHostAlgorithm(string name, Key key, byte[] data)
            : base(name)
        {
            this._key = key;

            var sshKey = new SshKeyData();
            sshKey.Load(data);
            this._key.Public = sshKey.Keys;
        }

        /// <summary>
        /// Signs the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public override byte[] Sign(byte[] data)
        {
            return new SignatureKeyData(this.Name, this._key.Sign(data)).GetBytes().ToArray();
        }

        /// <summary>
        /// Verifies the signature.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="signature">The signature.</param>
        /// <returns></returns>
        public override bool VerifySignature(byte[] data, byte[] signature)
        {
            var signatureData = new SignatureKeyData();
            signatureData.Load(signature);

            return this._key.VerifySignature(data, signatureData.Signature);
        }

        private class SshKeyData : SshData
        {
            public BigInteger[] Keys { get; private set; }

            public string Name { get; private set; }

            public SshKeyData()
            {

            }

            public SshKeyData(string name, params BigInteger[] keys)
            {
                this.Name = name;
                this.Keys = keys;
            }

            protected override void LoadData()
            {
                this.Name = this.ReadString();
                var keys = new List<BigInteger>();
                while (!this.IsEndOfData)
                {
                    keys.Add(this.ReadBigInt());
                }
                this.Keys = keys.ToArray();
            }

            protected override void SaveData()
            {
                this.Write(this.Name);
                foreach (var key in this.Keys)
                {
                    this.Write(key);
                }
            }
        }

        private class SignatureKeyData : SshData
        {
            /// <summary>
            /// Gets or sets the name of the algorithm.
            /// </summary>
            /// <value>
            /// The name of the algorithm.
            /// </value>
            public string AlgorithmName { get; private set; }

            /// <summary>
            /// Gets or sets the signature.
            /// </summary>
            /// <value>
            /// The signature.
            /// </value>
            public byte[] Signature { get; private set; }

            public SignatureKeyData()
            {

            }

            public SignatureKeyData(string name, byte[] signature)
            {
                this.AlgorithmName = name;
                this.Signature = signature;
            }

            /// <summary>
            /// Called when type specific data need to be loaded.
            /// </summary>
            protected override void LoadData()
            {
                this.AlgorithmName = this.ReadString();
                this.Signature = this.ReadBinaryString();
            }

            /// <summary>
            /// Called when type specific data need to be saved.
            /// </summary>
            protected override void SaveData()
            {
                this.Write(this.AlgorithmName);
                this.WriteBinaryString(this.Signature);
            }
        }
    }
}
