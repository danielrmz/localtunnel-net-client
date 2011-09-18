﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Renci.SshNet.Common
{
    /// <summary>
    /// Provides data for <see cref="Renci.SshNet.KeyboardInteractiveConnectionInfo.AuthenticationPrompt"/> event.
    /// </summary>
    public class AuthenticationPromptEventArgs : AuthenticationEventArgs
    {
        /// <summary>
        /// Gets prompt language.
        /// </summary>
        public string Language { get; private set; }

        /// <summary>
        /// Gets prompt instruction.
        /// </summary>
        public string Instruction { get; private set; }

        /// <summary>
        /// Gets server information request prompts.
        /// </summary>
        public IEnumerable<AuthenticationPrompt> Prompts { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationPromptEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="instruction">The instruction.</param>
        /// <param name="language">The language.</param>
        /// <param name="prompts">The information request prompts.</param>
        public AuthenticationPromptEventArgs(string username, string instruction, string language, IEnumerable<AuthenticationPrompt> prompts)
            : base(username)
        {
            this.Instruction = instruction;
            this.Language = language;
            this.Prompts = prompts;
        }
    }
}
