﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Sftp.Responses;

namespace Renci.SshNet.Sftp.Requests
{
    internal class SftpSetStatRequest : SftpRequest
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.SetStat; }
        }

        public string Path { get; private set; }

        public SftpFileAttributes Attributes { get; private set; }

        public SftpSetStatRequest(uint requestId, string path, SftpFileAttributes attributes, Action<SftpStatusResponse> statusAction)
            : base(requestId, statusAction)
        {
            this.Path = path;
            this.Attributes = attributes;
        }

        protected override void LoadData()
        {
            base.LoadData();
            this.Path = this.ReadString();
            this.Attributes = this.ReadAttributes();
        }

        protected override void SaveData()
        {
            base.SaveData();
            this.Write(this.Path);
            this.Write(this.Attributes);
        }
    }
}
