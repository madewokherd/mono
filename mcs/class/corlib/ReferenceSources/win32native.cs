using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Win32
{
	static class Win32Native
	{
		internal const string ADVAPI32 = "advapi32.dll";
        internal const String KERNEL32 = "kernel32.dll";

		// Error codes from WinError.h
		internal const int ERROR_SUCCESS = 0x0;
		internal const int ERROR_INVALID_FUNCTION = 0x1;
		internal const int ERROR_FILE_NOT_FOUND = 0x2;
		internal const int ERROR_PATH_NOT_FOUND = 0x3;
		internal const int ERROR_ACCESS_DENIED  = 0x5;
		internal const int ERROR_INVALID_HANDLE = 0x6;
		internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
		internal const int ERROR_INVALID_DATA = 0xd;
		internal const int ERROR_INVALID_DRIVE = 0xf;
		internal const int ERROR_NO_MORE_FILES = 0x12;
		internal const int ERROR_NOT_READY = 0x15;
		internal const int ERROR_BAD_LENGTH = 0x18;
		internal const int ERROR_SHARING_VIOLATION = 0x20;
		internal const int ERROR_NOT_SUPPORTED = 0x32;
		internal const int ERROR_FILE_EXISTS = 0x50;
		internal const int ERROR_INVALID_PARAMETER = 0x57;
		internal const int ERROR_BROKEN_PIPE = 0x6D;
		internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
		internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
		internal const int ERROR_INVALID_NAME = 0x7B;
		internal const int ERROR_BAD_PATHNAME = 0xA1;
		internal const int ERROR_ALREADY_EXISTS = 0xB7;
		internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
		internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
		internal const int ERROR_NO_DATA = 0xE8;
		internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
		internal const int ERROR_MORE_DATA = 0xEA;
		internal const int ERROR_DIRECTORY = 0x10B;
		internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
		internal const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation
		internal const int ERROR_NO_TOKEN = 0x3f0;
		internal const int ERROR_DLL_INIT_FAILED = 0x45A;
		internal const int ERROR_NON_ACCOUNT_SID = 0x4E9;
		internal const int ERROR_NOT_ALL_ASSIGNED = 0x514;
		internal const int ERROR_UNKNOWN_REVISION = 0x519;
		internal const int ERROR_INVALID_OWNER = 0x51B;
		internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
		internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
		internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
		internal const int ERROR_NONE_MAPPED = 0x534;
		internal const int ERROR_INVALID_ACL = 0x538;
		internal const int ERROR_INVALID_SID = 0x539;
		internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
		internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
		internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
		internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
		internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

		internal const FileAttributes FILE_ATTRIBUTE_DIRECTORY = FileAttributes.Directory;

		public static string GetMessage (int hr)
		{
			return "Error " + hr;
		}

		public static int MakeHRFromErrorCode (int errorCode)
		{
			return unchecked(((int)0x80070000) | errorCode);
		}

		public class SECURITY_ATTRIBUTES
		{

		}

		internal class WIN32_FIND_DATA
		{
			internal int dwFileAttributes = 0;
			internal String cFileName = null;
		}

        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1;

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool GetVolumeInformation(String drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName);

        [DllImport(KERNEL32, SetLastError=false, EntryPoint="SetErrorMode", ExactSpelling=true)]
        private static extern int SetErrorMode_VistaAndOlder(int newMode);

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="SetThreadErrorMode")]
        private static extern bool SetErrorMode_Win7AndNewer(int newMode, out int oldMode);

        // RTM versions of Win7 and Windows Server 2008 R2
        private static readonly Version ThreadErrorModeMinOsVersion = new Version(6, 1, 7600);

        // this method uses the thread-safe version of SetErrorMode on Windows 7 / Windows Server 2008 R2 operating systems.
        // 
        internal static int SetErrorMode(int newMode)
        {
#if !FEATURE_CORESYSTEM // ARMSTUB
            if (Environment.OSVersion.Version >= ThreadErrorModeMinOsVersion)
            {
                int oldMode;
                SetErrorMode_Win7AndNewer(newMode, out oldMode);
                return oldMode;
            }
#endif
            return SetErrorMode_VistaAndOlder(newMode);
        }
	}
}
