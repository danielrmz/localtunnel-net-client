﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Sftp.Responses;

namespace Renci.SshNet.Sftp.Requests
{
    internal class SftpReadDirRequest : SftpRequest
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.ReadDir; }
        }

        public byte[] Handle { get; private set; }

        public SftpReadDirRequest(uint requestId, byte[] handle, Action<SftpNameResponse> nameAction, Action<SftpStatusResponse> statusAction)
            : base(requestId, statusAction)
        {
            this.Handle = handle;
            this.SetAction(nameAction);
        }

        protected override void LoadData()
        {
            base.LoadData();
            this.Handle = this.ReadBinaryString();
        }

        protected override void SaveData()
        {
            base.SaveData();
            this.WriteBinaryString(this.Handle);
        }
    }
}
