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
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;

using LocalTunnel.Library;
using LocalTunnel.Library.Models;
using LocalTunnel.Library.Exceptions;

using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Shell;
using System.Net.NetworkInformation;

namespace LocalTunnel.UI
{
    public partial class Main : Form
    {
        #region Properties and Constructors

        /// <summary>
        /// local filename of the public key file to be used
        /// </summary>
        private static string _SSHPrivateKeyName = "key";

        /// <summary>
        /// Taskbar manager 
        /// </summary>
        private TaskbarManager taskBarManager;

        /// <summary>
        /// Constructor
        /// </summary>
        public Main()
        {
            InitializeComponent();
        }
        
        #endregion

        #region Jumplist methods

        /// <summary>
        /// Listen for window messages 
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == MessageHelper.TunnelPort)
            {
                // Create new tunnel. 
                try
                {
                    int messagePortId = m.WParam.ToInt32();
                    Port port = Port.GetUsedPorts().Where(portdb => portdb.Id == messagePortId).First();
                    if (port != null)
                    {
                        CreateTunnel(port.Host, port.Number, _SSHPrivateKeyName, port.ServiceHost);
                        notifyMessage.BalloonTipText = string.Format("Port {0} tunneled correctly, URL copied to clipboard", port.Number);
                        notifyMessage.BalloonTipTitle = "localtunnel";
                        notifyMessage.Visible = true; 
                        notifyMessage.ShowBalloonTip(1000 * 15);
                        MessageBox.Show(notifyMessage.BalloonTipText);
                    }
                }
                catch (ServiceException se)
                {
                    // Show tooltip.
                    MessageBox.Show("Error: " + se.Message);
                }
                return;
            }

            base.WndProc(ref m);
        }
        
        /// <summary>
        /// Creates the recently used ports jumplist
        /// </summary>
        private void CreateJumpList()
        {
            taskBarManager = TaskbarManager.Instance;
            if (TaskbarManager.IsPlatformSupported)
            {
                string currentDir = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;

                JumpList list = JumpList.CreateJumpList();
                
                // Separate by service hosts the recent ports.
                Port.GetUsedPorts().GroupBy( p => p.ServiceHost).ToList().ForEach(group => {
                    JumpListCustomCategory userActionsCategory = new JumpListCustomCategory(group.Key);

                    group.OrderByDescending(p => p.UsedTimes).ToList().ForEach(port =>
                    {
                        JumpListLink userActionLink = new JumpListLink(Assembly.GetEntryAssembly().Location,
                                                        string.Format("{0} @ {1}", port.Number.ToString(), port.Host))
                                                        {
                                                            Arguments = port.Id.ToString(),
                                                            IconReference = new IconReference(currentDir + "\\Resources\\tunnel-jump.ico,0")
                                                        };
                        userActionsCategory.AddJumpListItems(userActionLink);
                    });

                    list.AddCustomCategories(userActionsCategory);
                
                });
                
                list.Refresh();
            }
        }

        #endregion

        #region Common methods

        /// <summary>
        /// Creates the localtunnel.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sshKeyName"></param>
        /// <param name="serviceHost"></param>
        private void CreateTunnel(string host, int port, string sshKeyName, string serviceHost)
        {
            stripStatus.Text = string.Format("Creating tunnel to port {0}...", port);

            Tunnel tunnel = new Tunnel(host, port, sshKeyName);
            int idx = tunnelBindingSource.List.Count;
            tunnel.ServiceHost = serviceHost;
            tunnel.OnSocketException = new Tunnel.EventSocketException((tun, message) => {
                tun.StopTunnel(); 
                
                List<Tunnel> list = new List<Tunnel>();
                foreach (Tunnel t in tunnelBindingSource.List)
                {
                    if (!t.TunnelHost.Equals(tun.TunnelHost))
                    {
                        list.Add(t);
                    }
                }
                
                SetBindingSourceDataSource(tunnelBindingSource, list);
                MessageBox.Show(message, "Connection closed");
            });

            tunnel.Execute();

            tunnelBindingSource.Add(tunnel);

            Clipboard.SetDataObject(string.Format("http://{0}/", tunnel.TunnelHost));
            stripStatus.Text = string.Format("Tunnel to {0} created!, copied to clipboard", port);
            txtPort.Value = 80;
            cmdTunnel.Enabled = true;

            // Update jump list
            CreateJumpList();
        }

        /// <summary>
        /// Sets the new binding source, used when invoked from
        /// another thread
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="newDataSource"></param>
        public void SetBindingSourceDataSource(BindingSource bs, object newDataSource)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<BindingSource, object>(SetBindingSourceDataSource), new object[] { bs, newDataSource });
            }
            else
            {
                bs.DataSource = newDataSource;
            }
        }

        /// <summary>
        /// Closes all the current active tunnels and exits the application
        /// </summary>
        private void CloseAndExit()
        {
            foreach (Tunnel t in this.tunnelBindingSource.List)
            {
                t.StopTunnel();
            }
            this.grdData.Rows.Clear();

            Application.Exit();
        }

        #endregion

        #region Events

        /// <summary>
        /// Creates a new tunnel and updates the data grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdTunnel_Click(object sender, EventArgs e)
        {
            cmdTunnel.Enabled = false;

            try
            {
                int port = int.Parse(txtPort.Value.ToString());
                string serviceHost = "open.localtunnel.com";
                string host = "127.0.0.1";

                if (!string.IsNullOrEmpty(txtServiceHost.Text.Trim()))
                {
                    serviceHost = txtServiceHost.Text.Trim();
                }

                if (!string.IsNullOrEmpty(txtLocalHost.Text.Trim()))
                {
                    host = txtLocalHost.Text.Trim();
                }

                CreateTunnel(host, port, _SSHPrivateKeyName, serviceHost);
            }
            catch (ServiceException se)
            {
                stripStatus.Text = string.Format("Error: {0}", se.Message);
            } 
            cmdTunnel.Enabled = true;
        }

        /// <summary>
        /// Handles jumplists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_Shown(object sender, EventArgs e)
        {
            this.CreateJumpList();
            Program.HandleCmdLineArgs();
        }

        /// <summary>
        /// Sets the public key
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void publicKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFile.ShowDialog();

            string privateKey = openFile.FileName;

            if (string.IsNullOrEmpty(privateKey))
            {
                return;
            }

            // TODO: Validate private key is valid and let the user know right away.

            // Save locally.
            File.Copy(privateKey, _SSHPrivateKeyName, true);

            cmdTunnel.Enabled = true;
        }

        /// <summary>
        /// Exits the app through the menu strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.CloseAndExit();
        }

        /// <summary>
        /// Exits the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.CloseAndExit();
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
            
            if (this.grdData.Rows[cellEventIndex] == null)
            {
                return;
            }

            Tunnel tunnel =  (Tunnel)this.grdData.Rows[cellEventIndex].DataBoundItem;

            tunnelName = tunnel.TunnelHost;
            tunnelPort = tunnel.LocalPort;

            tunnel.StopTunnel();
            this.grdData.Rows.RemoveAt(cellEventIndex);

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

        /// <summary>
        /// Checks every minute for non-responding or closed tunnels.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void statusTimer_Tick(object sender, EventArgs e)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            Dictionary<int, bool> ports = new Dictionary<int, bool>();
            tcpConnInfoArray.ToList().ForEach(ip => ports[ip.LocalEndPoint.Port] = true);

            List<Tunnel> newDataSource = new List<Tunnel>();
            foreach (Tunnel t in tunnelBindingSource.List)
            {
                if (!t.IsStopped && (!ports.ContainsKey(t.LocalPort) || !t.IsConnected))
                {
                    t.StopTunnel();
                    t.ReOpenTunnel();
                    //MessageBox.Show(string.Format("Port {0} is not available anymore, closing connection", t.LocalPort), "Connection closed");
                    stripStatus.Text = string.Empty;
                }

                newDataSource.Add(t);
            }

            SetBindingSourceDataSource(tunnelBindingSource, newDataSource);
        }

        /// <summary>
        /// Opens the advanced tunnel setup panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lnkAdvanced_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (pnlAdvanced.Visible == false)
            {
                pnlAdvanced.Visible = true;
                this.tableLayoutPanel1.RowStyles[0].Height = 33; 
            } else {
                pnlAdvanced.Visible = false;
                this.tableLayoutPanel1.RowStyles[0].Height = 18; 
            }
        }

        #endregion

    }
}
