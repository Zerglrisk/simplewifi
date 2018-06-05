using SimpleWifi.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SimpleWifi.Win32.Interop;

namespace SimpleWifi
{
	public class AccessPoint
	{
	    internal AccessPoint(WlanInterface interfac, WlanAvailableNetwork network)
		{
			Interface = interfac;
			Network = network;
		}

		public string Name => Encoding.UTF8.GetString(Network.dot11Ssid.SSID, 0, (int)Network.dot11Ssid.SSIDLength);

	    public uint SignalStrength => Network.wlanSignalQuality;

	    /// <summary>
		/// If the computer has a connection profile stored for this access point
		/// </summary>
		public bool HasProfile
		{
			get
			{
				try
				{
					return Interface.GetProfiles().Any(p => p.profileName == Name);
				}
				catch 
				{ 
					return false; 
				}
			}
		}
		
		public bool IsSecure => Network.securityEnabled;

	    public bool IsConnected
		{
			get
			{
				try
				{
					var a = Interface.CurrentConnection; // This prop throws exception if not connected, which forces me to this try catch. Refactor plix.
					return a.profileName == Network.profileName;
				}
				catch
				{
					return false;
				}
			}

		}

		/// <summary>
		/// Returns the underlying network object.
		/// </summary>
		internal WlanAvailableNetwork Network { get; }


	    /// <summary>
		/// Returns the underlying interface object.
		/// </summary>
		internal WlanInterface Interface { get; }

	    /// <summary>
		/// Checks that the password format matches this access point's encryption method.
		/// </summary>
		public bool IsValidPassword(string password)
		{
			return PasswordHelper.IsValid(password, Network.dot11DefaultCipherAlgorithm);
		}		
		
		/// <summary>
		/// Connect synchronous to the access point.
		/// </summary>
		public bool Connect(AuthRequest request, bool overwriteProfile = false)
		{
			// No point to continue with the connect if the password is not valid if overwrite is true or profile is missing.
			if (!request.IsPasswordValid && (!HasProfile || overwriteProfile))
				return false;

			// If we should create or overwrite the profile, do so.
		    if (HasProfile && !overwriteProfile)
		        return Interface.ConnectSynchronously(WlanConnectionMode.Profile, Network.dot11BssType, Name, 6000);
		    if (HasProfile)
		        Interface.DeleteProfile(Name);

		    request.Process();


		    // TODO: Auth algorithm: IEEE80211_Open + Cipher algorithm: None throws an error.
			// Probably due to connectionmode profile + no profile exist, cant figure out how to solve it though.
			return Interface.ConnectSynchronously(WlanConnectionMode.Profile, Network.dot11BssType, Name, 6000);			
		}

		/// <summary>
		/// Connect asynchronous to the access point.
		/// </summary>
		public void ConnectAsync(AuthRequest request, bool overwriteProfile = false, Action<bool> onConnectComplete = null)
		{
			// TODO: Refactor -> Use async connect in wlaninterface.
			ThreadPool.QueueUserWorkItem(new WaitCallback((o) => {
				bool success = false;

				try
				{
					success = Connect(request, overwriteProfile);
				}
				catch (Win32Exception)
				{					
					success = false;
				}

			    onConnectComplete?.Invoke(success);
			}));
		}
				
		public string GetProfileXML()
		{
		    return HasProfile ? Interface.GetProfileXml(Name) : string.Empty;
		}

		public void DeleteProfile()
		{
			try
			{
				if (HasProfile)
					Interface.DeleteProfile(Name);
			}
		    catch
		    {
		        // ignored
		    }
		}

		public sealed override string ToString()
		{
			StringBuilder info = new StringBuilder();
			info.AppendLine("Interface: " + Interface.InterfaceName);
			info.AppendLine("Auth algorithm: " + Network.dot11DefaultAuthAlgorithm);
			info.AppendLine("Cipher algorithm: " + Network.dot11DefaultCipherAlgorithm);
			info.AppendLine("BSS type: " + Network.dot11BssType);
			info.AppendLine("Connectable: " + Network.networkConnectable);
			
			if (!Network.networkConnectable)
				info.AppendLine("Reason to false: " + Network.wlanNotConnectableReason);

			return info.ToString();
		}
	}
}
