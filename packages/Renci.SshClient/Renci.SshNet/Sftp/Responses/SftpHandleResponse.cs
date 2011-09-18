﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Sftp.Responses
{
    internal class SftpHandleResponse : SftpResponse
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.Handle; }
        }

        public byte[] Handle { get; private set; }

        protected override void LoadData()
        {
            base.LoadData();
            
            this.Handle = this.ReadBinaryString();
        }
    }
}
