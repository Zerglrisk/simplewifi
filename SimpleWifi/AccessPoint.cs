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

	        //For WIndows 7
	        //If XP Return ERROR_NOT_SUPPORTED
	        //The request is not supported.This error is returned if this function was called from a Windows XP with SP3 or Wireless LAN API for Windows XP with SP2 client.This error is also returned if the WLAN AutoConfig service is disabled.
	        Bssids = Interface.GetNetworkBssList(Network.dot11Ssid, Network.dot11BssType).ToList();
	    }

	    public string Name => Encoding.UTF8.GetString(Network.dot11Ssid.SSID, 0, (int)Network.dot11Ssid.SSIDLength);

	    public string Authentication => Network.Dot11DefaultAuthAlgorithmToSTring;
	    public string Encryption => Network.Dot11DefaultCipherAlgorithmToString;
	    public string Hex => BitConverter.ToString(Encoding.Default.GetBytes(this.Name)).Replace("-", "");
	    public string NetworkType => Network.dot11BssType.ToString();
        /// <summary>
        /// It Support For WIndows 7
        /// </summary>
	    public List<WlanBssEntry> Bssids { get; } // => Interface.GetNetworkBssList(Network.dot11Ssid, Network.dot11BssType).ToList();
        /// <summary>
        /// It support For Xp
        /// </summary>
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
					return (Network.flags & WlanAvailableNetworkFlags.HasProfile) != 0;
				    //Not Use: Interface.GetProfiles().Any(p => p.profileName == Name);
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
				    return (Network.flags & WlanAvailableNetworkFlags.Connected) != 0;
                    //Not Use
                    //var a = Interface.CurrentConnection; // This prop throws exception if not connected, which forces me to this try catch. Refactor plix.
                    //return a.profileName == Network.profileName;
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
        /// Not Use For now
        /// </summary>
        /// <param name="security"></param>
	    public void SetSecurity(bool security)
	    {
	        var wlanAvailableNetwork = Network;
	        wlanAvailableNetwork.securityEnabled = security;
	    }
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
				
		public string GetProfileXML(bool isProtected = true)
		{
		    return HasProfile ? Interface.GetProfileXml(Name, isProtected) : string.Empty;
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
