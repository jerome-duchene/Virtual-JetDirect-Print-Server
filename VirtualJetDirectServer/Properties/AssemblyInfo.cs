using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Virtual JetDirect print server")]
[assembly: AssemblyDescription("Virtual JetDirect print server")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("VirtualJetDirectServer")]
[assembly: AssemblyCopyright("Copyright © Phozen 2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d94f5a46-be8b-4da0-9e78-e26a5a70aba2")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("0.1.2.*")]
#if DEBUG
[assembly: AssemblyInformationalVersion("0.1.2 Beta")]
#else
[assembly: AssemblyInformationalVersion("0.1.2 RC")]
#endif
/*
 * Version  Date        Who                     Comment
 * ---------------------------------------------------------------------------------------------------------------------
 * 0.1.0                Jérôme Duchêne          First release: upgrade of the project RawPrintServer 1.0.0 made in C++
 * 0.1.1    04Nov2019   Jérôme Duchêne          Bug: windows service still in starting mode, embedded OnStart code in a new thread
 * 0.1.2    23Jan2020   Jérôme Duchêne          Bug: it can happen that the PJL command was split on buffer (in this case, the EOJ wasn't detected)
 */