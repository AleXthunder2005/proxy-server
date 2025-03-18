using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProxyServer.controllers;
using static ProxyServer.Settings;

namespace ProxyServer.views
{
    public partial class MainForm: Form
    {

        private ProxyController _proxyController;

        public MainForm()
        {
            InitializeComponent();
            InitializeSettings();
            _proxyController = new ProxyController(this);
        }

        public void UpdateLog(string logMessage)
        {
            tbLog.Text += logMessage;
        }

        public void SafeUpdateLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLog(message)));
            }
            else
            {
                UpdateLog(message);
            }
        }

        private void proxyStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _proxyController.StartProxy();
        }

        private void proxyStopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _proxyController.StopProxy();
        }

        private void settingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
