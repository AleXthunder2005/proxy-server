using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProxyServer.views
{
    public partial class MainForm: Form
    {

        private ProxyController _proxyController;

        public MainForm()
        {
            InitializeComponent();
            _proxyController = new ProxyController(this);
        }

        public void UpdateLog(string logMessage)
        {
            tbLog.AppendText(logMessage + Environment.NewLine);
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
