using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualJetDirectServer
{
    partial class VirtualJetDirectService : ServiceBase
    {
        #region Mbers
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private VirtualJetDirectServer _printServer = new VirtualJetDirectServer();
        #endregion

        #region Ctor
        public VirtualJetDirectService()
        {
            InitializeComponent();

            // bind the event for managing a new job to print
            _printServer.OnNewJob += PrintServer_OnNewJob;
            _printServer.OnClientDisconnected += _printServer_OnClientDisconnected;
        }
        #endregion

        #region ServiceBase implementation
        protected override void OnStart(string[] args)
        {
            try
            {
                Thread srv = new Thread(new ThreadStart(_printServer.Start));
                srv.Start();
               _log.Info("[Service] Starting print server");
            }
            catch (Exception ex)
            {
                _log.Fatal(ex, "[Service] Unexpected exception on starting print server");
                Stop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                _printServer.Stop();
                _log.Info("[Service] Stopping print server");
            }
            catch (Exception ex)
            {
                _log.Fatal(ex, "[Service] Unexpected exception on stopping print server");
            }
        }
        #endregion

        #region Public method
        public void FakeStart()
        {
            ThreadPool.QueueUserWorkItem(ThreadStart);
        }

        public void FakeStop()
        {
            OnStop();
        }
        #endregion

        #region Private method
        private void ThreadStart(object state)
        {
            OnStart(null);
        }

        private void PrintServer_OnNewJob(StringBuilder document)
        {
            _log.Info("[Service] New job to print");
            PrintDocument(document);
        }

        private void _printServer_OnClientDisconnected(StringBuilder document)
        {
            _log.Warn("[Service] Client disconnected, try to print current data");
            PrintDocument(document);
        }

        private void PrintDocument(StringBuilder document)
        {
            string jobName = "Untitled document";
            string content = document.ToString();

            // check if the job have a name
            string searchPattern = null;
            string extractedName = "";

            if (content.Contains("@PJL SET JOBNAME")) // name set by environment variable
                searchPattern = "@PJL SET JOBNAME";
            if (content.Contains("@PJL JOB NAME")) // name set by command
                searchPattern = "@PJL JOB NAME";
            if (!string.IsNullOrEmpty(searchPattern))
            {
                // extract job name
                if (ExtractDocumentName(content, searchPattern, out extractedName))
                    jobName = extractedName;
            }

            // save the job on the local disk
            OutputJob(document, jobName);

            // send the document to the configured printer
            _log.Trace($"[Service] Print '{jobName}' on '{Properties.Settings.Default.PrinterName}'");
            RawPrinterHelper.SendStringToPrinter(Properties.Settings.Default.PrinterName, jobName, content);
        }

        private bool ExtractDocumentName(string content, string pattern, out string extractedName)
        {
            extractedName = "";
            Regex regex = new Regex($"{pattern}\\s*=\\s*\"(.+)\"");
            var nameFound = regex.Match(content);
            if (!nameFound.Success) return false;

            extractedName = nameFound.Groups[1].Value;
            return true;
        }

        private void OutputJob(StringBuilder document, string documentName)
        {
            string filePath = Path.Combine(Properties.Settings.Default.OutputDir, $"{DateTime.Now:yyyyMMddHHmmssfff}.ps");
            _log.Trace($"[Service] Write document '{documentName}' to '{filePath}'");
            File.WriteAllText(filePath, document.ToString());
        }
        #endregion
    }
}
