﻿using Renci.SshNet.Common;

namespace Renci.SshNet.Messages.Connection
{
    /// <summary>
    /// Represents type specific information for channel request.
    /// </summary>
    public abstract class RequestInfo : SshData
    {
        /// <summary>
        /// Gets the name of the request.
        /// </summary>
        /// <value>
        /// The name of the request.
        /// </value>
        public abstract string RequestName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether reply message is needed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if reply message is needed; otherwise, <c>false</c>.
        /// </value>
        public bool WantReply { get; protected set; }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            this.WantReply = this.ReadBoolean();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            this.Write(this.WantReply);
        }
    }
}
