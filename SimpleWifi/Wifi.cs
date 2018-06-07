using SimpleWifi.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SimpleWifi.Win32.Interop;

using NotifCodeACM = SimpleWifi.Win32.Interop.WlanNotificationCodeAcm;
using NotifCodeMSM = SimpleWifi.Win32.Interop.WlanNotificationCodeMsm;

namespace SimpleWifi
{
	public class Wifi
	{
		public event EventHandler<WifiStatusEventArgs> ConnectionStatusChanged;
		
		private readonly WlanClient _client;
		private WifiStatus _connectionStatus;
        private bool _isConnectionStatusSet = false;
        public bool NoWifiAvailable = false;

		public Wifi()
		{
			_client = new WlanClient();
            NoWifiAvailable = _client.NoWifiAvailable;
            if (_client.NoWifiAvailable)
                return;
			
			foreach (var inte in _client.Interfaces)
				inte.WlanNotification += inte_WlanNotification;
		}

        /// <summary>
        /// Scan All Interfaces.
        /// </summary>
	    public void Scan()
	    {
	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
                wlanIface.Scan();
	        }
	    }

        /// <summary>
        /// Return one access point that matached specified ssid Name from specified wlaninterface.
        /// </summary>
        /// <param name="wlanIface"></param>
        /// <param name="SsidName"></param>
        /// <returns></returns>
	    public AccessPoint GetAccessPoint(WlanInterface wlanIface, string SsidName)
	    {
	        if (_client.NoWifiAvailable)
	            return null;

	        if (wlanIface == null) return null;

	        WlanAvailableNetwork[] rawNetworks = wlanIface.GetAvailableNetworkList();
	        List<WlanAvailableNetwork> networks = new List<WlanAvailableNetwork>();

	        // Remove network entries without profile name if one exist with a profile name.
	        foreach (WlanAvailableNetwork network in rawNetworks)
	        {
	            bool hasProfileName = !string.IsNullOrEmpty(network.profileName);
	            bool anotherInstanceWithProfileExists = rawNetworks.Any(n => n.Equals(network) && !string.IsNullOrEmpty(n.profileName));

	            if (!anotherInstanceWithProfileExists || hasProfileName)
	                networks.Add(network);
	        }

	        foreach (WlanAvailableNetwork network in networks)
	        {
	            if (Encoding.ASCII.GetString(network.dot11Ssid.SSID, 0, (int)network.dot11Ssid.SSIDLength) !=
	                SsidName) continue;
	            AccessPoint accessPoint = new AccessPoint(wlanIface, network);
	            return accessPoint;
	        }

	        return null;
	    }
        /// <summary>
        /// Return one access point that matached specified ssid Name.
        /// </summary>
        /// <param name="SsidName"></param>
        /// <returns></returns>
	    public AccessPoint GetAccessPoint(string SsidName)
	    {
	        if (_client.NoWifiAvailable)
	            return null;

	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            WlanAvailableNetwork[] rawNetworks = wlanIface.GetAvailableNetworkList();
	            List<WlanAvailableNetwork> networks = new List<WlanAvailableNetwork>();

	            // Remove network entries without profile name if one exist with a profile name.
	            foreach (WlanAvailableNetwork network in rawNetworks)
	            {
	                bool hasProfileName = !string.IsNullOrEmpty(network.profileName);
	                bool anotherInstanceWithProfileExists = rawNetworks.Any(n => n.Equals(network) && !string.IsNullOrEmpty(n.profileName));

	                if (!anotherInstanceWithProfileExists || hasProfileName)
	                    networks.Add(network);
	            }

	            foreach (WlanAvailableNetwork network in networks)
	            {
	                if (Encoding.ASCII.GetString(network.dot11Ssid.SSID, 0, (int) network.dot11Ssid.SSIDLength) !=
	                    SsidName) continue;
                    AccessPoint accessPoint = new AccessPoint(wlanIface, network);
	                return accessPoint;
	            }
            }
	        return null;
        }

	    /// <summary>
	    /// Returns a list over all available access points from specified wlaninterface.
	    /// </summary>
	    /// <param name="wlanIface"></param>
	    /// <returns></returns>
	    public List<AccessPoint> GetAccessPoints(WlanInterface wlanIface)
	    {
	        List<AccessPoint> accessPoints = new List<AccessPoint>();
	        if (_client.NoWifiAvailable)
	            return accessPoints;

	        WlanAvailableNetwork[] rawNetworks = wlanIface.GetAvailableNetworkList();
	        List<WlanAvailableNetwork> networks = new List<WlanAvailableNetwork>();

	        // Remove network entries without profile name if one exist with a profile name.
	        foreach (WlanAvailableNetwork network in rawNetworks)
	        {
	            bool hasProfileName = !string.IsNullOrEmpty(network.profileName);
	            bool anotherInstanceWithProfileExists =
	                rawNetworks.Any(n => n.Equals(network) && !string.IsNullOrEmpty(n.profileName));

	            if (!anotherInstanceWithProfileExists || hasProfileName)
	                networks.Add(network);
	        }

	        foreach (WlanAvailableNetwork network in networks)
	        {
	            accessPoints.Add(new AccessPoint(wlanIface, network));
	        }


	        return accessPoints;
	    }

	    /// <summary>
	    /// Returns a list over all available access points
	    /// </summary>
	    public List<AccessPoint> GetAccessPoints()
	    {
	        List<AccessPoint> accessPoints = new List<AccessPoint>();
	        if (_client.NoWifiAvailable)
	            return accessPoints;

	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            WlanAvailableNetwork[] rawNetworks = wlanIface.GetAvailableNetworkList();
	            List<WlanAvailableNetwork> networks = new List<WlanAvailableNetwork>();

	            // Remove network entries without profile name if one exist with a profile name.
	            foreach (WlanAvailableNetwork network in rawNetworks)
	            {
	                bool hasProfileName = !string.IsNullOrEmpty(network.profileName);
	                bool anotherInstanceWithProfileExists = rawNetworks.Any(n => n.Equals(network) && !string.IsNullOrEmpty(n.profileName));

	                if (!anotherInstanceWithProfileExists || hasProfileName)
	                    networks.Add(network);
	            }

	            foreach (WlanAvailableNetwork network in networks)
	            {
	                accessPoints.Add(new AccessPoint(wlanIface, network));
	            }
	        }

	        return accessPoints;
	    }

	    /// <summary>
	    /// For Test
	    /// Returns a list over all available access points
	    /// </summary>
	    public IEnumerable<AccessPoint> EnumerateAccessPoints()
	    {
	        if (_client.NoWifiAvailable) yield break;
	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            IEnumerable<WlanAvailableNetwork> rawNetworks = wlanIface.GetAvailableNetworkList();
	            List<WlanAvailableNetwork> networks = new List<WlanAvailableNetwork>();

	            // Remove network entries without profile name if one exist with a profile name.
	            foreach (WlanAvailableNetwork network in rawNetworks)
	            {
	                bool hasProfileName = !string.IsNullOrEmpty(network.profileName);
	                bool anotherInstanceWithProfileExists =
	                    rawNetworks.Any(n => n.Equals(network) && !string.IsNullOrEmpty(n.profileName));

	                if (!anotherInstanceWithProfileExists || hasProfileName)
	                    networks.Add(network);
	            }

	            foreach (WlanAvailableNetwork network in networks)
	            {
	                yield return new AccessPoint(wlanIface, network);
	            }
	        }
	    }

        /// <summary>
        /// Get known rofile Name from all interfaces.
        /// </summary>
        /// <returns></returns>
	    public List<string> GetKnownProfileNames()
	    {
	        List<string> profiles = new List<string>();
	        if (_client.NoWifiAvailable)
	            return null;

	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            WlanProfileInfo[] rawProfileInfo = wlanIface.GetProfiles();

	            foreach (WlanProfileInfo profileInfo in rawProfileInfo)
	            {
	                bool anotherInstanceWithProfileExists =
	                    rawProfileInfo.Any(n => n.Equals(profileInfo) && !string.IsNullOrEmpty(n.profileName));

	                if (!anotherInstanceWithProfileExists)
	                    profiles.Add(profileInfo.profileName);
	            }
	        }
	        return profiles;
	    }

        /// <summary>
        /// Get known profile names from specified interface.
        /// </summary>
        /// <param name="wlanIface"></param>
        /// <returns></returns>
	    public List<string> GetKnownProfileNames(WlanInterface wlanIface)
	    {
	        List<string> profiles = new List<string>();
	        if (_client.NoWifiAvailable)
	            return null;

	        WlanProfileInfo[] rawProfileInfo = wlanIface.GetProfiles();

	        foreach (WlanProfileInfo profileInfo in rawProfileInfo)
	        {
	            bool anotherInstanceWithProfileExists =
	                rawProfileInfo.Any(n => n.Equals(profileInfo) && !string.IsNullOrEmpty(n.profileName));

	            if (!anotherInstanceWithProfileExists)
	                profiles.Add(profileInfo.profileName);
	        }

	        return profiles;
	    }

        /// <summary>
        /// Get Known Profile's Xmls from all interfaces
        /// </summary>
        /// <param name="isProtected">if false, key material not encrypted</param>
        /// <returns></returns>
	    public List<string> GetKnownProfileXmls(bool isProtected= true)
	    {
	        List<string> profiles = new List<string>();
	        if (_client.NoWifiAvailable)
	            return null;

	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            string[] rawProfileInfo = wlanIface.GetProfilesXml(isProtected);

	            foreach (string profileInfo in rawProfileInfo)
	            {
	                bool anotherInstanceWithProfileExists =
	                    rawProfileInfo.Any(n => n.Equals(profileInfo) && !string.IsNullOrEmpty(n));

	                if (!anotherInstanceWithProfileExists)
	                    profiles.Add(profileInfo);
	            }
	        }
	        return profiles;
        }

        /// <summary>
        /// Get Known Profile's Xmls from specified interfaces
        /// </summary>
        /// <param name="wlanIface"></param>
        /// <param name="isProtected">if false, key material not encrypted</param>
        /// <returns></returns>
	    public List<string> GetKnownProfileXmls(WlanInterface wlanIface, bool isProtected = true)
	    {
            List<string> profiles = new List<string>();
	        if (_client.NoWifiAvailable)
	            return null;

	        string[] rawProfileInfo = wlanIface.GetProfilesXml(isProtected);

	        foreach (string profileInfo in rawProfileInfo)
	        {
	            bool anotherInstanceWithProfileExists =
	                rawProfileInfo.Any(n => n.Equals(profileInfo) && !string.IsNullOrEmpty(n));

	            if (!anotherInstanceWithProfileExists)
	                profiles.Add(profileInfo);
            }

	        return profiles;
	    }

	    /// <summary>
	    /// Get Known specified profile's Xml from all interfaces
	    /// if couple of interface has same profile name, it will return two xml string.
	    /// </summary>
	    /// <param name="profileName"></param>
	    /// <param name="isProtected"></param>
	    /// <returns></returns>
	    public List<string> GetKnownProfileXml(string profileName, bool isProtected = true)
	    {
	        List<string> profiles = new List<string>();
	        if (_client.NoWifiAvailable)
	            return null;

	        foreach (WlanInterface wlanIface in _client.Interfaces)
	        {
	            string profileXml = wlanIface.GetProfileXml(profileName, isProtected);

	            if (!string.IsNullOrEmpty(profileXml))
	                profiles.Add(profileXml);
	        }
	        return profiles;
	    }

        /// <summary>
        /// Disconnect all wifi interfaces
        /// </summary>
        public void Disconnect()
        {
            if (_client.NoWifiAvailable)
                return;

			foreach (WlanInterface wlanIface in _client.Interfaces)
			{
				wlanIface.Disconnect();
			}		
		}
		public WifiStatus ConnectionStatus
		{
			get
			{
				if (!_isConnectionStatusSet)
					ConnectionStatus = GetForcedConnectionStatus();

				return _connectionStatus;
			}
			private set
			{
				_isConnectionStatusSet = true;
				_connectionStatus = value;
			}
		}

		private void inte_WlanNotification(WlanNotificationData notifyData)
		{
			if (notifyData.notificationSource == WlanNotificationSource.ACM && (NotifCodeACM)notifyData.NotificationCode == NotifCodeACM.Disconnected)
				OnConnectionStatusChanged(WifiStatus.Disconnected);
			else if (notifyData.notificationSource == WlanNotificationSource.MSM && (NotifCodeMSM)notifyData.NotificationCode == NotifCodeMSM.Connected)
				OnConnectionStatusChanged(WifiStatus.Connected);
		}

		private void OnConnectionStatusChanged(WifiStatus newStatus)
		{
			ConnectionStatus = newStatus;

		    ConnectionStatusChanged?.Invoke(this, new WifiStatusEventArgs(newStatus));
		}

		// I don't like this method, it's slow, ugly and should be refactored ASAP.
        private WifiStatus GetForcedConnectionStatus()
        {
            if (NoWifiAvailable)
                return WifiStatus.Disconnected;

			bool connected = false;

			foreach (var i in _client.Interfaces)
			{
				try
				{
					var a = i.CurrentConnection; // Current connection throws an exception if disconnected.
					connected = true;
				}
			    catch
			    {
			        // ignored
			    }
			}

			return connected ? WifiStatus.Connected : WifiStatus.Disconnected;
		}		
	}

	public class WifiStatusEventArgs : EventArgs
	{
		public WifiStatus NewStatus { get; private set; }

		internal WifiStatusEventArgs(WifiStatus status) : base()
		{
			this.NewStatus = status;
		}

	}

	public enum WifiStatus
	{
		Disconnected,
		Connected
	}
}
