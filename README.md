# Virtual-JetDirect-Print-Server
Create a virtual print server compatible with JetDirect and HP PJL command
Based on the RawPrintServer project found on Sourceforge.net

Language: C#
Version: .Net Framework 4.5.2
Supported OS: all OS able to run .Net Framework 4.5.2 (tested under Windows 10 64bit)

Dependances:
- Nlog 4.6.2

Usage:
Execute the application with a command line interface.

- VirtualJetDirectServer.exe install: Deploy the application as a Windows services (asked creadential during installation process)
- VirtualJetDirectServer.exe uninstall: Remove the application from the Windows services
- VirtualJetDirectServer.exe standalone: Run the application in the command line

Edit VirtualJetDirectServer.exe.config to change parameters:
- PrinterName: Name or network path of the printer to use
- LogFile: File path for the log informations
- OutputDir: Directory where to save a copy of the job 
- ServerPort: TCP port to use for the server (default: 9100)

Status:

For the moment, I manage the commands: @PJL INFO STATUS, @PJL JOB, @PJL EOJ.
For job forwarding, the job name has taken in @PJL SET JOBNAME or @PJL JOB NAME if exists.
Tested with an equipment that can print on a postscript network printer and by configuring a raw printer under Windows 10 desktop.
Used for printing from an equipment to a specific software printer.
