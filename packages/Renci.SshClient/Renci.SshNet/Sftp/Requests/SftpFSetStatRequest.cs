﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Sftp.Responses;

namespace Renci.SshNet.Sftp.Requests
{
    internal class SftpFSetStatRequest : SftpRequest
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.FSetStat; }
        }

        public byte[] Handle { get; private set; }

        public SftpFileAttributes Attributes { get; private set; }

        public SftpFSetStatRequest(uint requestId, byte[] handle, SftpFileAttributes attributes, Action<SftpStatusResponse> statusAction)
            : base(requestId, statusAction)
        {
            this.Handle = handle;
            this.Attributes = attributes;
        }

        protected override void LoadData()
        {
            base.LoadData();
            this.Handle = this.ReadBinaryString();
            this.Attributes = this.ReadAttributes();
        }

        protected override void SaveData()
        {
            base.SaveData();
            this.WriteBinaryString(this.Handle);
            this.Write(this.Attributes);
        }
    }
}
