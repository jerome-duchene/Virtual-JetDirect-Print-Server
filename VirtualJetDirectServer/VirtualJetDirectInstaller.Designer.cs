namespace VirtualJetDirectServer
{
    partial class VirtualJetDirectInstaller
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PrintServiceInstall = new System.ServiceProcess.ServiceInstaller();
            this.PrintServiceProcessInstall = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // PrintServiceInstall
            // 
            this.PrintServiceInstall.Description = "Virtual print server managing HP PJL command. To change the configuration, edit t" +
    "he VirtualJetDirectServer.exe.config file";
            this.PrintServiceInstall.DisplayName = "Virtual JetDirect print server";
            this.PrintServiceInstall.ServiceName = "VirtualPrintServer";
            this.PrintServiceInstall.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // PrintServiceProcessInstall
            // 
            this.PrintServiceProcessInstall.Password = null;
            this.PrintServiceProcessInstall.Username = null;
            // 
            // VirtualJerDirectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.PrintServiceInstall,
            this.PrintServiceProcessInstall});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller PrintServiceInstall;
        private System.ServiceProcess.ServiceProcessInstaller PrintServiceProcessInstall;
    }
}
