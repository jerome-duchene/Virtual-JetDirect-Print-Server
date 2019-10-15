using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VirtualJetDirectServer
{
    class Program
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            // configure logging engine
            NLog.Targets.Target fileTarget = new NLog.Targets.FileTarget()
            {
                Name = "file",
                FileName = $@"{Properties.Settings.Default.LogFile}",
                Layout = @"${longdate}|${level:uppercase=true}|${environment:COMPUTERNAME}|${environment:USERNAME}|${logger}|${message}${onexception:${newline}EXCEPTION OCCURRED\:${exception:format=type,message,method,stacktrace:maxInnerExceptionLevel=10:innerFormat=shortType,message,method}}"
            };
            NLog.Targets.Target consoleTarget = new NLog.Targets.ConsoleTarget()
            {
                Name = "console",
                DetectConsoleAvailable = true
            };
            NLog.Config.LoggingConfiguration config = new NLog.Config.LoggingConfiguration();
            config.AddRuleForAllLevels(fileTarget);
            config.AddRuleForAllLevels(consoleTarget);
            NLog.LogManager.Configuration = config;

            _log.Trace($"Log saved at: {Properties.Settings.Default.LogFile}");

            // if we call the program from command line or Windows
            if(Environment.UserInteractive)
            {
                if(args.Contains("install"))
                {
                    _log.Info("Installing print server service");
                    ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                    return;
                }

                if (args.Contains("uninstall"))
                {
                    _log.Info("Uninstalling print server service");
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                    return;
                }

                if (args.Contains("standalone"))
                {
                    VirtualJetDirectService svc = new VirtualJetDirectService();
                    Console.WriteLine("Starting service");
                    svc.FakeStart();
                    Console.WriteLine("Service running...");
                    Console.WriteLine("Press any key to stop it.");
                    Console.ReadKey(true);
                    svc.FakeStop();
                    Console.WriteLine("Service stopped. Bye.");
                    return;
                }

                Console.WriteLine(
$@"Missing or invalid arguments.

Available parameters (case sensitive):
* install: deploy the service
* uninstall: remove the service
* standalone: run an instance of the service in the console");
                Console.ReadKey(true);
                return;
            }

            // else: execute the code of the service
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[]
            {
                new VirtualJetDirectService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
