// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileLockInfo.cs" company="WildGums">
//   Copyright (c) 2008 - 2017 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.FileSystem
{
    using System;
    using System.Runtime.InteropServices;
    using Catel.Logging;

    public static class FileLockInfo
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        #region Constants
        // maximum character count of application friendly name. 
        private const int CCH_RM_MAX_APP_NAME = 255;
        // maximum character count of service short name. 
        private const int CCH_RM_MAX_SVC_NAME = 63;
        // A system restart is not required. 
        private const int RmRebootReasonNone = 0;
        #endregion

        #region Methods
        /// <summary> 
        /// Registers resources to a Restart Manager session. The Restart Manager uses  
        /// the list of resources registered with the session to determine which  
        /// applications and services must be shut down and restarted. Resources can be  
        /// identified by filenames, service short names, or RM_UNIQUE_PROCESS structures 
        /// that describe running applications. 
        /// </summary> 
        /// <param name="pSessionHandle"> 
        /// A handle to an existing Restart Manager session. 
        /// </param> 
        /// <param name="nFiles">The number of files being registered</param> 
        /// <param name="rgsFilenames"> 
        /// An array of null-terminated strings of full filename paths. 
        /// </param> 
        /// <param name="nApplications">The number of processes being registered</param> 
        /// <param name="rgApplications">An array of RM_UNIQUE_PROCESS structures</param> 
        /// <param name="nServices">The number of services to be registered</param> 
        /// <param name="rgsServiceNames"> 
        /// An array of null-terminated strings of service short names. 
        /// </param> 
        /// <returns>The function can return one of the system error codes that  
        /// are defined in Winerror.h 
        /// </returns> 
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RmRegisterResources(uint pSessionHandle,
            UInt32 nFiles, string[] rgsFilenames,
            UInt32 nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            UInt32 nServices, string[] rgsServiceNames);

        /// <summary> 
        /// Starts a new Restart Manager session. A maximum of 64 Restart Manager  
        /// sessions per user session can be open on the system at the same time.  
        /// When this function starts a session, it returns a session handle and  
        /// session key that can be used in subsequent calls to the Restart Manager API. 
        /// </summary> 
        /// <param name="pSessionHandle"> 
        /// A pointer to the handle of a Restart Manager session. 
        /// </param> 
        /// <param name="dwSessionFlags">Reserved. This parameter should be 0.</param> 
        /// <param name="strSessionKey"> 
        /// A null-terminated string that contains the session key to the new session. 
        /// </param> 
        /// <returns></returns> 
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags,
            string strSessionKey);

        /// <summary> 
        /// Ends the Restart Manager session. This function should be called by the  
        /// primary installer that has previously started the session by calling the  
        /// RmStartSession function. The RmEndSession function can be called by a  
        /// secondary installer that is joined to the session once no more resources  
        /// need to be registered by the secondary installer. 
        /// </summary> 
        /// <param name="pSessionHandle"> 
        /// A handle to an existing Restart Manager session. 
        /// </param> 
        /// <returns> 
        /// The function can return one of the system error codes 
        /// that are defined in Winerror.h. 
        /// </returns> 
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RmEndSession(uint pSessionHandle);

        /// <summary> 
        /// Gets a list of all applications and services that are currently using  
        /// resources that have been registered with the Restart Manager session. 
        /// </summary> 
        /// <param name="dwSessionHandle"> 
        /// A handle to an existing Restart Manager session. 
        /// </param> 
        /// <param name="pnProcInfoNeeded">A pointer to an array size necessary to  
        /// receive RM_PROCESS_INFO structures required to return information for  
        /// all affected applications and services. 
        /// </param> 
        /// <param name="pnProcInfo"> 
        /// A pointer to the total number of RM_PROCESS_INFO structures in an array 
        /// and number of structures filled. 
        /// </param> 
        /// <param name="rgAffectedApps"> 
        /// An array of RM_PROCESS_INFO structures that list the applications and  
        /// services using resources that have been registered with the session. 
        /// </param> 
        /// <param name="lpdwRebootReasons"> 
        /// Pointer to location that receives a value of the RM_REBOOT_REASON 
        /// enumeration that describes the reason a system restart is needed. 
        /// </param> 
        /// <returns></returns> 
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);


        public static string[] GetProcessesLockingFile(string fileName)
        {
            var sessionkey = Guid.NewGuid().ToString();
            uint handle;
            var res = RmStartSession(out handle, 0, sessionkey);
            if (res != 0)
            {
                Log.Warning($"Failed to get processes locking file '{fileName}': Could not start new Restart Manager session");
                return new string[] { };
            }

            try
            {
                uint pnProcInfoNeeded;
                uint pnProcInfo = 100;
                uint lpdwRebootReasons = RmRebootReasonNone;
                string[] resources = {fileName};

                RM_PROCESS_INFO[] processInfo =
                    new RM_PROCESS_INFO[pnProcInfo];

                res = RmRegisterResources(handle, 1, resources, 0, null, 0, null);
                if (res != 0)
                {
                    Log.Warning($"Failed to get processes locking file '{fileName}': Could not register resource to a Restart Manager session");
                    return new string[] { };
                }

                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                if (res == 0) //The function completed successfully. 
                {
                    var result = new string[pnProcInfo];
                    for (var i = 0; i < pnProcInfo; i++)
                    {
                        result[i] = processInfo[i].AppName;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to get processes locking file '{fileName}'");
            }
            finally
            {
                RmEndSession(handle);
            }

            return new string[] { };
        }
        #endregion

        #region Nested type: RM_APP_TYPE
        /// <summary> 
        /// Specifies the type of application that is described by 
        /// the RM_PROCESS_INFO structure. 
        /// </summary> 
        private enum RM_APP_TYPE
        {
            // The application cannot be classified as any other type. 
            RmUnknownApp = 0,
            // A Windows application run as a stand-alone process that 
            // displays a top-level window. 
            RmMainWindow = 1,
            // A Windows application that does not run as a stand-alone 
            // process and does not display a top-level window. 
            RmOtherWindow = 2,
            // The application is a Windows service. 
            RmService = 3,
            // The application is Windows Explorer. 
            RmExplorer = 4,
            // The application is a stand-alone console application. 
            RmConsole = 5,
            // A system restart is required to complete the installation because 
            // a process cannot be shut down. 
            RmCritical = 1000
        }
        #endregion

        #region Nested type: RM_PROCESS_INFO
        /// <summary> 
        /// Describes an application that is to be registered with the Restart Manager. 
        /// </summary> 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            // Contains an RM_UNIQUE_PROCESS structure that uniquely identifies the 
            // application by its PID and the time the process began. 
            public readonly RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            // If the process is a service, this parameter returns the  
            // long name for the service. 
            public readonly string AppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            // If the process is a service, this is the short name for the service. 
            public readonly string ServiceShortName;

            // Contains an RM_APP_TYPE enumeration value. 
            public readonly RM_APP_TYPE ApplicationType;
            // Contains a bit mask that describes the current status of the application. 
            public readonly uint AppStatus;
            // Contains the Terminal Services session ID of the process. 
            public readonly uint TSSessionId;
            // TRUE if the application can be restarted by the  
            // Restart Manager; otherwise, FALSE. 
            [MarshalAs(UnmanagedType.Bool)] public readonly bool Restartable;
        }
        #endregion

        #region Nested type: RM_UNIQUE_PROCESS
        /// <summary> 
        /// Uniquely identifies a process by its PID and the time the process began.  
        /// An array of RM_UNIQUE_PROCESS structures can be passed 
        /// to the RmRegisterResources function. 
        /// </summary> 
        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            // The product identifier (PID). 
            public readonly int ProcessId;
            // The creation time of the process. 
            public readonly System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }
        #endregion
    }
}
