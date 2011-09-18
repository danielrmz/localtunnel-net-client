﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Sftp.Responses
{
    internal class SftpStatusResponse : SftpResponse
    {
        public override SftpMessageTypes SftpMessageType
        {
            get { return SftpMessageTypes.Status; }
        }


        public StatusCodes StatusCode { get; private set; }

        public string ErrorMessage { get; private set; }

        public string Language { get; private set; }
        
        protected override void LoadData()
        {
            base.LoadData();
            
            this.StatusCode = (StatusCodes)this.ReadUInt32();

            if (!this.IsEndOfData)
            {
                this.ErrorMessage = this.ReadString();
                this.Language = this.ReadString();
            }
        }
    }
}
