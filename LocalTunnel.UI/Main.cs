/*
Copyright (C) 2011 by Daniel Ramirez (hello@danielrmz.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LocalTunnel.Models;

using System.IO;
using LocalTunnel.Library;

namespace LocalTunnel.UI
{
    public partial class Main : Form
    {
        private static string _SSHKeyName = "key.pub";

        public Main()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a new tunnel and updates the data grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdTunnel_Click(object sender, EventArgs e)
        {
            cmdTunnel.Enabled = false;

            int port = int.Parse(txtPort.Value.ToString());

            stripStatus.Text = string.Format("Creating tunnel to port {0}...", port);

            Tunnel tunnel = (new Tunnel(port, _SSHKeyName).Execute());

            tunnelBindingSource.Add(tunnel);

            Clipboard.SetDataObject(string.Format("http://{0}/", tunnel.TunnelHost));

            stripStatus.Text = string.Format("Tunnel to {0} created!, copied to clipboard", port);

            txtPort.Value = 80;

            cmdTunnel.Enabled = true;

        }

        /// <summary>
        /// Verifies if a key has been set if not, we cannot create tunnels
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_Load(object sender, EventArgs e)
        {
            if (!File.Exists(_SSHKeyName))
            {
                lblKey.Visible = true;
                cmdTunnel.Enabled = false;
            }
        }     

        /// <summary>
        /// Sets the public key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void publicKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile.ShowDialog();
            string publicKey = openFile.FileName;
            string privateKey = publicKey.Replace(".pub", "");

            if (string.IsNullOrEmpty(publicKey))
            {
                return;
            }

            if (!File.Exists(privateKey))
            {
                MessageBox.Show("The private key file must be on the same directory as the public key, and should not have extension");
                return;
            }

            File.Copy(publicKey, _SSHKeyName);
            File.Copy(privateKey, _SSHKeyName.Replace(".pub",""));

            cmdTunnel.Enabled = true;
            lblKey.Visible = false;
        }

        /// <summary>
        /// Exits the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Tunnel t in this.tunnelBindingSource.List)
            {
                t.StopTunnel();
            }

            Application.Exit();
        }

        /// <summary>
        /// Exits the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (Tunnel t in this.tunnelBindingSource.List)
            {
                t.StopTunnel();
            }
            this.grdData.Rows.Clear();

            Application.Exit();
        }

        /// <summary>
        /// Stops the selected tunnel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string tunnelName;
            int tunnelPort;
            Tunnel tunnel =  (Tunnel)this.grdData.Rows[cellEventIndex].DataBoundItem;

            tunnelName = tunnel.TunnelHost;
            tunnelPort = tunnel.LocalPort;

            tunnel.StopTunnel();
            this.tunnelBindingSource.List.RemoveAt(cellEventIndex);

            stripStatus.Text = string.Format("{0} tunnel to port {1}  was deleted.", tunnelName, tunnelPort);

        }

        /// <summary>
        /// The current clicked datagrid row
        /// </summary>
        private int cellEventIndex;

        /// <summary>
        /// Saves the row clicked by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void grdData_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                cellEventIndex = e.RowIndex;
            }
        }

        /// <summary>
        /// Copies the host url
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tunnel tunnel = (Tunnel)this.grdData.Rows[cellEventIndex].DataBoundItem;
            Clipboard.SetDataObject(string.Format("http://{0}/", tunnel.TunnelHost));

            stripStatus.Text = string.Format("{0} copied to clipboard", tunnel.TunnelHost);

        }


    }
}
