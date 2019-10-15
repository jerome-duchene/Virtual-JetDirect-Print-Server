using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace VirtualJetDirectServer
{
    [RunInstaller(true)]
    public partial class VirtualJetDirectInstaller : Installer
    {
        #region Ctor
        public VirtualJetDirectInstaller()
        {
            InitializeComponent();
            this.AfterInstall += VirtualJerDirectInstaller_AfterInstall;
        }

        #endregion

        #region Private event handler
        private void VirtualJerDirectInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            using (ServiceController ctrl = new ServiceController(PrintServiceInstall.ServiceName))
            {
                ctrl.Start();
            }
        }
        #endregion
    }
}
