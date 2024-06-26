//
// System.Configuration.ConfigurationManager.cs
//
// Authors:
//	Duncan Mak (duncan@ximian.com)
// 	Lluis Sanchez Gual (lluis@novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Text;
using System.Configuration.Internal;
using System.Security.Cryptography;

namespace System.Configuration {

	/*roaming user config path: C:\Documents and Settings\toshok\Application Data\domain-System.Configurati_Url_py3nlovv3wxe21qgacxc3n2b1mph2log\1.0.0.0\user.config */

	public static class ConfigurationManager
	{
//		static bool systemWebInUse;
		static InternalConfigurationFactory configFactory = new InternalConfigurationFactory ();
		static IInternalConfigSystem configSystem = new ClientConfigurationSystem ();
		static object lockobj = new object ();
		
		internal static ExeConfigurationFileMap GetDefaultExeFileMap(Assembly calling_assembly, string exePath,
			ConfigurationUserLevel userLevel) {
			ExeConfigurationFileMap map = new ExeConfigurationFileMap ();

			/* Roaming and RoamingAndLocal should be different

			On windows,
			  PerUserRoaming = \Documents and Settings\<username>\Application Data\...
			  PerUserRoamingAndLocal = \Documents and Settings\<username>\Local Settings\Application Data\...
			*/
			if (exePath != null && exePath.Length != 0) {
				if (!Path.IsPathRooted (exePath))
					exePath = Path.GetFullPath (exePath);
				if (!File.Exists (exePath)) {
					Exception cause = new ArgumentException ("The specified path does not exist.", "exePath");
					throw new ConfigurationErrorsException ("Error Initializing the configuration system:", cause);
				}
			}

			ClientConfigPaths clientConfigPaths = ClientConfigPaths.GetPaths((String.IsNullOrEmpty(exePath) ? null : exePath), true);
			switch (userLevel) {
			case ConfigurationUserLevel.None:
				map.ExeConfigFilename = clientConfigPaths.ApplicationConfigUri;
				break;
			case ConfigurationUserLevel.PerUserRoaming:
				map.RoamingUserConfigFilename = clientConfigPaths.RoamingConfigFilename;
				goto case ConfigurationUserLevel.None;

			case ConfigurationUserLevel.PerUserRoamingAndLocal:
				map.LocalUserConfigFilename = clientConfigPaths.LocalConfigFilename;
				goto case ConfigurationUserLevel.PerUserRoaming;
 			}

			return map;
		}

		internal static Configuration OpenExeConfigurationInternal (ConfigurationUserLevel userLevel, Assembly calling_assembly, string exePath)
		{
			return ConfigurationFactory.Create (typeof(ExeConfigurationHost),
				GetDefaultExeFileMap(calling_assembly, exePath, userLevel), userLevel);
		}

		public static Configuration OpenExeConfiguration (ConfigurationUserLevel userLevel)
		{
			return OpenExeConfigurationInternal (userLevel, Assembly.GetEntryAssembly () ?? Assembly.GetCallingAssembly (), null);
		}
		
		public static Configuration OpenExeConfiguration (string exePath)
		{
			return OpenExeConfigurationInternal (ConfigurationUserLevel.None, Assembly.GetEntryAssembly () ?? Assembly.GetCallingAssembly (), exePath);
		}

		public static Configuration OpenMappedExeConfiguration (ExeConfigurationFileMap fileMap, ConfigurationUserLevel userLevel)
		{
			if (fileMap is null) {
				fileMap = GetDefaultExeFileMap(Assembly.GetEntryAssembly () ?? Assembly.GetCallingAssembly (), null, userLevel);
			}
			return ConfigurationFactory.Create (typeof(ExeConfigurationHost), fileMap, userLevel);
		}

		public static Configuration OpenMachineConfiguration ()
		{
			ConfigurationFileMap map = new ConfigurationFileMap ();
			return ConfigurationFactory.Create (typeof(MachineConfigurationHost), map);
		}
		
		public static Configuration OpenMappedMachineConfiguration (ConfigurationFileMap fileMap)
		{
			return ConfigurationFactory.Create (typeof(MachineConfigurationHost), fileMap);
		}
		
		internal static IInternalConfigConfigurationFactory ConfigurationFactory {
			get { return configFactory; }
		}

		internal static IInternalConfigSystem ConfigurationSystem {
			get { return configSystem; }
		}

		public static object GetSection (string sectionName)
		{
			object o = ConfigurationSystem.GetSection (sectionName);
			if (o is ConfigurationSection)
				return ((ConfigurationSection) o).GetRuntimeObject ();
			else
				return o;
		}

		public static void RefreshSection (string sectionName)
		{
			ConfigurationSystem.RefreshConfig (sectionName);
		}

		public static NameValueCollection AppSettings {
			get {
				return (NameValueCollection) GetSection ("appSettings");
			}
		}

		public static ConnectionStringSettingsCollection ConnectionStrings {
			get {
				ConnectionStringsSection connectionStrings = (ConnectionStringsSection) GetSection ("connectionStrings");
				return connectionStrings.ConnectionStrings;
			}
		}

		/* invoked from System.Web */
		internal static IInternalConfigSystem ChangeConfigurationSystem (IInternalConfigSystem newSystem)
		{
			if (newSystem == null)
				throw new ArgumentNullException ("newSystem");

			lock (lockobj) {
				// KLUDGE!! We need that when an assembly loaded inside an ASP.NET
				// domain does OpenExeConfiguration ("") - we must return the path
				// to web.config in that instance.
				/*
				string t = newSystem.GetType ().ToString ();
				if (String.Compare (t, "System.Web.Configuration.HttpConfigurationSystem", StringComparison.OrdinalIgnoreCase) == 0)
					systemWebInUse = true;
				else
					systemWebInUse = false;
				*/
				IInternalConfigSystem old = configSystem;
				configSystem = newSystem;
				return old;
			}
		}
	}
}
