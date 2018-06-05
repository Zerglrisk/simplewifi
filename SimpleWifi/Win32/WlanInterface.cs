using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SimpleWifi.Win32
{
	/// <summary>
	/// Represents a Wifi network interface.
	/// </summary>
	public class WlanInterface
	{
		private readonly WlanClient _client;
		private readonly WlanInterfaceInfo _info;

		#region Events
		/// <summary>
		/// Represents a method that will handle <see cref="WlanNotification"/> events.
		/// </summary>
		/// <param name="notifyData">The notification data.</param>
		public delegate void WlanNotificationEventHandler(WlanNotificationData notifyData);

		/// <summary>
		/// Represents a method that will handle <see cref="WlanConnectionNotification"/> events.
		/// </summary>
		/// <param name="notifyData">The notification data.</param>
		/// <param name="connNotifyData">The notification data.</param>
		public delegate void WlanConnectionNotificationEventHandler(WlanNotificationData notifyData, WlanConnectionNotificationData connNotifyData);

		/// <summary>
		/// Represents a method that will handle <see cref="WlanReasonNotification"/> events.
		/// </summary>
		/// <param name="notifyData">The notification data.</param>
		/// <param name="reasonCode">The reason code.</param>
		public delegate void WlanReasonNotificationEventHandler(WlanNotificationData notifyData, WlanReasonCode reasonCode);

		/// <summary>
		/// Occurs when an event of any kind occurs on a WLAN interface.
		/// </summary>
		public event WlanNotificationEventHandler WlanNotification;

		/// <summary>
		/// Occurs when a WLAN interface changes connection state.
		/// </summary>
		public event WlanConnectionNotificationEventHandler WlanConnectionNotification;

		/// <summary>
		/// Occurs when a WLAN operation fails due to some reason.
		/// </summary>
		public event WlanReasonNotificationEventHandler WlanReasonNotification;
		
		private bool _queueEvents;
		private AutoResetEvent _eventQueueFilled = new AutoResetEvent(false);
		private Queue<object> _eventQueue = new Queue<object>();
		#endregion

		#region Properties
		internal WlanInterface(WlanClient client, WlanInterfaceInfo info)
		{
			this._client = client;
			this._info = info;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="WlanInterface"/> is automatically configured.
		/// </summary>
		/// <value><c>true</c> if "autoconf" is enabled; otherwise, <c>false</c>.</value>
		public bool Autoconf
		{
			get
			{
				return GetInterfaceInt(WlanIntfOpcode.AutoconfEnabled) != 0;
			}
			set
			{
				SetInterfaceInt(WlanIntfOpcode.AutoconfEnabled, value ? 1 : 0);
			}
		}


		/// <summary>
		/// Gets the network interface of this wireless interface.
		/// </summary>
		/// <remarks>
		/// The network interface allows querying of generic network properties such as the interface's IP address.
		/// </remarks>
		public NetworkInterface NetworkInterface
		{
			get
			{
				// Do not cache the NetworkInterface; We need it fresh
				// each time cause otherwise it caches the IP information.
				foreach (NetworkInterface netIface in NetworkInterface.GetAllNetworkInterfaces())
				{
					Guid netIfaceGuid = new Guid(netIface.Id);
					if (netIfaceGuid.Equals(_info.interfaceGuid))
					{
						return netIface;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// The GUID of the interface (same content as the <see cref="System.Net.NetworkInformation.NetworkInterface.Id"/> value).
		/// </summary>
		public Guid InterfaceGuid => _info.interfaceGuid;

	    /// <summary>
		/// The description of the interface.
		/// This is a user-immutable string containing the vendor and model name of the adapter.
		/// </summary>
		public string InterfaceDescription => _info.interfaceDescription;

	    /// <summary>
		/// The friendly name given to the interface by the user (e.g. "Local Area Network Connection").
		/// </summary>
		public string InterfaceName => NetworkInterface.Name;

	    /// <summary>
		/// Gets or sets the BSS type for the indicated interface.
		/// </summary>
		/// <value>The type of the BSS.</value>
		public Dot11BssType BssType
		{
			get
			{
				return (Dot11BssType)GetInterfaceInt(WlanIntfOpcode.BssType);
			}
			set
			{
				SetInterfaceInt(WlanIntfOpcode.BssType, (int)value);
			}
		}
	    public WlanRadioState RadioState => GetRadioState();

	    /// <summary>
        /// Gets the state of the interface.
        /// </summary>
        /// <value>The state of the interface.</value>
        public WlanInterfaceState InterfaceState => (WlanInterfaceState)GetInterfaceInt(WlanIntfOpcode.InterfaceState);

	    /// <summary>
		/// Gets the channel.
		/// </summary>
		/// <value>The channel.</value>
		/// <remarks>Not supported on Windows XP SP2.</remarks>
		public int Channel => GetInterfaceInt(WlanIntfOpcode.ChannelNumber);

	    /// <summary>
		/// Gets the RSSI.
		/// </summary>
		/// <value>The RSSI.</value>
		/// <remarks>Not supported on Windows XP SP2.</remarks>
		public int RSSI => GetInterfaceInt(WlanIntfOpcode.RSSI);

	    /// <summary>
		/// Gets the current operation mode.
		/// </summary>
		/// <value>The current operation mode.</value>
		/// <remarks>Not supported on Windows XP SP2.</remarks>
		public Dot11OperationMode CurrentOperationMode => (Dot11OperationMode)GetInterfaceInt(WlanIntfOpcode.CurrentOperationMode);

	    /// <summary>
		/// Gets the attributes of the current connection.
		/// </summary>
		/// <value>The current connection attributes.</value>
		/// <exception cref="Win32Exception">An exception with code 0x0000139F (The group or resource is not in the correct state to perform the requested operation.) will be thrown if the interface is not connected to a network.</exception>
		public WlanConnectionAttributes CurrentConnection
		{
			get
			{
				int valueSize;
				IntPtr valuePtr;
				WlanOpcodeValueType opcodeValueType;

				// TODO: Should get result from WlanInterop.WlanQueryInterface and handle if it's 0x0000139F (not connected) gracefully
				WlanInterop.ThrowIfError(WlanInterop.WlanQueryInterface(_client.ClientHandle, _info.interfaceGuid, WlanIntfOpcode.CurrentConnection, IntPtr.Zero, out valueSize, out valuePtr, out opcodeValueType));
				try
				{
					return (WlanConnectionAttributes)Marshal.PtrToStructure(valuePtr, typeof(WlanConnectionAttributes));
				}
				finally
				{
					WlanInterop.WlanFreeMemory(valuePtr);
				}
			}
		}

		#endregion

		/// <summary>
		/// Requests a scan for available networks.
		/// </summary>
		/// <remarks>
		/// The method returns immediately. Progress is reported through the <see cref="WlanNotification"/> event.
		/// </remarks>
		public void Scan()
		{
			WlanInterop.ThrowIfError(WlanInterop.WlanScan(_client.ClientHandle, _info.interfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));
		}

		/// <summary>
		/// Converts a pointer to a available networks list (header + entries) to an array of available network entries.
		/// </summary>
		/// <param name="bssListPtr">A pointer to an available networks list's header.</param>
		/// <returns>An array of available network entries.</returns>
		private WlanAvailableNetwork[] ConvertAvailableNetworkListPtr(IntPtr availNetListPtr)
		{
			WlanAvailableNetworkListHeader availNetListHeader = (WlanAvailableNetworkListHeader)Marshal.PtrToStructure(availNetListPtr, typeof(WlanAvailableNetworkListHeader));
			long availNetListIt = availNetListPtr.ToInt64() + Marshal.SizeOf(typeof(WlanAvailableNetworkListHeader));			
			WlanAvailableNetwork[] availNets = new WlanAvailableNetwork[availNetListHeader.numberOfItems];			
			for (int i = 0; i < availNetListHeader.numberOfItems; ++i)
			{
				availNets[i] = (WlanAvailableNetwork)Marshal.PtrToStructure(new IntPtr(availNetListIt), typeof(WlanAvailableNetwork));
				availNetListIt += Marshal.SizeOf(typeof(WlanAvailableNetwork));
			}

			return availNets;
		}

	    public WlanAvailableNetwork[] GetAvailableNetworkList()
	    {
	        return GetAvailableNetworkList(0);
	    }
        /// <summary>
        /// Retrieves the list of available networks.
        /// </summary>
        /// <param name="flags">Controls the type of networks returned.</param>
        /// <returns>A list of the available networks.</returns>
        public WlanAvailableNetwork[] GetAvailableNetworkList(WlanGetAvailableNetworkFlags flags)
		{
			IntPtr availNetListPtr;
			WlanInterop.ThrowIfError(WlanInterop.WlanGetAvailableNetworkList(_client.ClientHandle, _info.interfaceGuid, flags, IntPtr.Zero, out availNetListPtr));

			try
			{
				return ConvertAvailableNetworkListPtr(availNetListPtr);
			}
			finally
			{
				WlanInterop.WlanFreeMemory(availNetListPtr);
			}
		}

		/// <summary>
		/// Converts a pointer to a BSS list (header + entries) to an array of BSS entries.
		/// </summary>
		/// <param name="bssListPtr">A pointer to a BSS list's header.</param>
		/// <returns>An array of BSS entries.</returns>
		private WlanBssEntry[] ConvertBssListPtr(IntPtr bssListPtr)
		{
			WlanBssListHeader bssListHeader = (WlanBssListHeader)Marshal.PtrToStructure(bssListPtr, typeof(WlanBssListHeader));
			long bssListIt = bssListPtr.ToInt64() + Marshal.SizeOf(typeof(WlanBssListHeader));
			WlanBssEntry[] bssEntries = new WlanBssEntry[bssListHeader.numberOfItems];
			for (int i = 0; i < bssListHeader.numberOfItems; ++i)
			{
				bssEntries[i] = (WlanBssEntry)Marshal.PtrToStructure(new IntPtr(bssListIt), typeof(WlanBssEntry));
				bssListIt += Marshal.SizeOf(typeof(WlanBssEntry));
			}
			return bssEntries;
		}

		/// <summary>
		/// Retrieves the basic service sets (BSS) list of all available networks.
		/// </summary>
		public WlanBssEntry[] GetNetworkBssList()
		{
			IntPtr bssListPtr;
			WlanInterop.ThrowIfError(WlanInterop.WlanGetNetworkBssList(_client.ClientHandle, _info.interfaceGuid, IntPtr.Zero, Dot11BssType.Any, false, IntPtr.Zero, out bssListPtr));

			try
			{
				return ConvertBssListPtr(bssListPtr);
			}
			finally
			{
				WlanInterop.WlanFreeMemory(bssListPtr);
			}
		}

		/// <summary>
		/// Retrieves the basic service sets (BSS) list of the specified network.
		/// </summary>
		/// <param name="ssid">Specifies the SSID of the network from which the BSS list is requested.</param>
		/// <param name="bssType">Indicates the BSS type of the network.</param>
		/// <param name="securityEnabled">Indicates whether security is enabled on the network.</param>
		public WlanBssEntry[] GetNetworkBssList(Dot11Ssid ssid, Dot11BssType bssType)
		{
			IntPtr ssidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
			Marshal.StructureToPtr(ssid, ssidPtr, false);

			try
			{
				IntPtr bssListPtr;
				WlanInterop.ThrowIfError(WlanInterop.WlanGetNetworkBssList(_client.ClientHandle, _info.interfaceGuid, ssidPtr, bssType, true, IntPtr.Zero, out bssListPtr));

				try
				{
					return ConvertBssListPtr(bssListPtr);
				}
				finally
				{
					WlanInterop.WlanFreeMemory(bssListPtr);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(ssidPtr);
			}
		}

		/// <summary>
		/// Connects to a network defined by a connection parameters structure.
		/// </summary>
		/// <param name="connectionParams">The connection paramters.</param>
		protected void Connect(WlanConnectionParameters connectionParams)
		{
			WlanInterop.ThrowIfError(WlanInterop.WlanConnect(_client.ClientHandle, _info.interfaceGuid, ref connectionParams, IntPtr.Zero));
		}

		public void Disconnect()
		{
			WlanInterop.ThrowIfError(WlanInterop.WlanDisconnect(_client.ClientHandle, _info.interfaceGuid, IntPtr.Zero));
		}

		/// <summary>
		/// Requests a connection (association) to the specified wireless network.
		/// </summary>
		/// <remarks>
		/// The method returns immediately. Progress is reported through the <see cref="WlanNotification"/> event.
		/// </remarks>
		public void Connect(WlanConnectionMode connectionMode, Dot11BssType bssType, string profile)
		{
			WlanConnectionParameters connectionParams = new WlanConnectionParameters();
			connectionParams.wlanConnectionMode = connectionMode;
			connectionParams.profile = profile;
			connectionParams.dot11BssType = bssType;
			connectionParams.flags = 0;
			Connect(connectionParams);
		}

		/// <summary>
		/// Connects (associates) to the specified wireless network, returning either on a success to connect
		/// or a failure.
		/// </summary>
		/// <param name="connectionMode"></param>
		/// <param name="bssType"></param>
		/// <param name="profile"></param>
		/// <param name="connectTimeout"></param>
		/// <returns></returns>
		public bool ConnectSynchronously(WlanConnectionMode connectionMode, Dot11BssType bssType, string profile, int connectTimeout)
		{
			_queueEvents = true; // NOTE: This can cause side effects, other places in the application might not get events properly.
			try
			{
				Connect(connectionMode, bssType, profile);
				while (_queueEvents && _eventQueueFilled.WaitOne(connectTimeout, true))
				{
					lock (_eventQueue)
					{
						while (_eventQueue.Count != 0)
						{
							object e = _eventQueue.Dequeue();
						    if (!(e is WlanConnectionNotificationEventData)) continue;
						    WlanConnectionNotificationEventData wlanConnectionData = (WlanConnectionNotificationEventData)e;
						    // Check if the conditions are good to indicate either success or failure.
						    if (wlanConnectionData.NotifyData.notificationSource == WlanNotificationSource.MSM)
						    {
						        switch ((WlanNotificationCodeMsm)wlanConnectionData.NotifyData.notificationCode)
						        {
						            case WlanNotificationCodeMsm.Connected:										
						                if (wlanConnectionData.ConnNotifyData.profileName == profile)
						                    return true;
						                break;
						        }
						    }
						    break;
						}
					}
				}
			}
			finally
			{
			    _queueEvents = false;
			    lock (_eventQueue)
			    {
			        _eventQueue.Clear();
			    }
			}
			return false; // timeout expired and no "connection complete"
		}

		/// <summary>
		/// Connects to the specified wireless network.
		/// </summary>
		/// <remarks>
		/// The method returns immediately. Progress is reported through the <see cref="WlanNotification"/> event.
		/// </remarks>
		public void Connect(WlanConnectionMode connectionMode, Dot11BssType bssType, Dot11Ssid ssid, WlanConnectionFlags flags)
		{
			WlanConnectionParameters connectionParams = new WlanConnectionParameters();
			connectionParams.wlanConnectionMode = connectionMode;
			connectionParams.dot11SsidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
			Marshal.StructureToPtr(ssid, connectionParams.dot11SsidPtr, false);
			connectionParams.dot11BssType = bssType;
			connectionParams.flags = flags;
			
			Connect(connectionParams);

			Marshal.DestroyStructure(connectionParams.dot11SsidPtr, ssid.GetType());
			Marshal.FreeHGlobal(connectionParams.dot11SsidPtr);
		}

		/// <summary>
		/// Deletes a profile.
		/// </summary>
		/// <param name="profileName">
		/// The name of the profile to be deleted. Profile names are case-sensitive.
		/// On Windows XP SP2, the supplied name must match the profile name derived automatically from the SSID of the network. For an infrastructure network profile, the SSID must be supplied for the profile name. For an ad hoc network profile, the supplied name must be the SSID of the ad hoc network followed by <c>-adhoc</c>.
		/// </param>
		public void DeleteProfile(string profileName)
		{
			WlanInterop.ThrowIfError(WlanInterop.WlanDeleteProfile(_client.ClientHandle, _info.interfaceGuid, profileName, IntPtr.Zero));
		}

		/// <summary>
		/// Sets the profile.
		/// </summary>
		/// <param name="flags">The flags to set on the profile.</param>
		/// <param name="profileXml">The XML representation of the profile. On Windows XP SP 2, special care should be taken to adhere to its limitations.</param>
		/// <param name="overwrite">If a profile by the given name already exists, then specifies whether to overwrite it (if <c>true</c>) or return an error (if <c>false</c>).</param>
		/// <returns>The resulting code indicating a success or the reason why the profile wasn't valid.</returns>
		public WlanReasonCode SetProfile(WlanProfileFlags flags, string profileXml, bool overwrite)
		{
			WlanReasonCode reasonCode;

			WlanInterop.ThrowIfError(WlanInterop.WlanSetProfile(_client.ClientHandle, _info.interfaceGuid, flags, profileXml, null, overwrite, IntPtr.Zero, out reasonCode));

			return reasonCode;
		}

		public void SetEAP(string profileName, string userXML)
		{
			WlanInterop.ThrowIfError(WlanInterop.WlanSetProfileEapXmlUserData(_client.ClientHandle, _info.interfaceGuid, profileName, SetEapUserDataMode.None, userXML, IntPtr.Zero));
		}

	    /// <summary>
	    /// Gets the profile's XML specification.
	    /// </summary>
	    /// <param name="profileName">The name of the profile.</param>
	    /// <param name="isProtected">If True KeyMaterial Will show crypted(It can be use after windows 7)</param>
	    /// <returns>The XML document.</returns>
	    public string GetProfileXml(string profileName, bool isProtected)
		{
			IntPtr profileXmlPtr;
            //It Only Work After Windows 7 (GetPlaintextKey)
			WlanProfileFlags flags = isProtected ? WlanProfileFlags.AllUser : WlanProfileFlags.GetPlaintextKey;
			WlanAccess access;

			WlanInterop.ThrowIfError(WlanInterop.WlanGetProfile(_client.ClientHandle, _info.interfaceGuid, profileName, IntPtr.Zero, out profileXmlPtr, ref flags, out access));

			try
			{
				return Marshal.PtrToStringUni(profileXmlPtr);
			}
			finally
			{
				WlanInterop.WlanFreeMemory(profileXmlPtr);
			}
		}

		/// <summary>
		/// Gets the information of all profiles on this interface.
		/// </summary>
		/// <returns>The profiles information.</returns>
		public WlanProfileInfo[] GetProfiles()
		{
			IntPtr profileListPtr;
			WlanInterop.ThrowIfError(WlanInterop.WlanGetProfileList(_client.ClientHandle, _info.interfaceGuid, IntPtr.Zero, out profileListPtr));

			try
			{
				WlanProfileInfoListHeader header = (WlanProfileInfoListHeader)Marshal.PtrToStructure(profileListPtr, typeof(WlanProfileInfoListHeader));
				WlanProfileInfo[] profileInfos = new WlanProfileInfo[header.numberOfItems];
				long profileListIterator = profileListPtr.ToInt64() + Marshal.SizeOf(header);

				for (int i = 0; i < header.numberOfItems; ++i)
				{
					WlanProfileInfo profileInfo = (WlanProfileInfo)Marshal.PtrToStructure(new IntPtr(profileListIterator), typeof(WlanProfileInfo));
					profileInfos[i] = profileInfo;
					profileListIterator += Marshal.SizeOf(profileInfo);
				}

				return profileInfos;
			}
			finally
			{
				WlanInterop.WlanFreeMemory(profileListPtr);
			}
		}

		internal void OnWlanConnection(WlanNotificationData notifyData, WlanConnectionNotificationData connNotifyData)
		{
		    WlanConnectionNotification?.Invoke(notifyData, connNotifyData);

		    if (!_queueEvents) return;
		    WlanConnectionNotificationEventData queuedEvent = new WlanConnectionNotificationEventData();
		    queuedEvent.NotifyData = notifyData;
		    queuedEvent.ConnNotifyData = connNotifyData;
		    EnqueueEvent(queuedEvent);
		}

		internal void OnWlanReason(WlanNotificationData notifyData, WlanReasonCode reasonCode)
		{
		    WlanReasonNotification?.Invoke(notifyData, reasonCode);

		    if (!_queueEvents) return;
		    WlanReasonNotificationData queuedEvent = new WlanReasonNotificationData
		    {
		        NotifyData = notifyData,
		        ReasonCode = reasonCode
		    };
		    EnqueueEvent(queuedEvent);
		}

		internal void OnWlanNotification(WlanNotificationData notifyData)
		{
		    WlanNotification?.Invoke(notifyData);
		}

		/// <summary>
		/// Enqueues a notification event to be processed serially.
		/// </summary>
		private void EnqueueEvent(object queuedEvent)
		{
			lock (_eventQueue)
				_eventQueue.Enqueue(queuedEvent);

			_eventQueueFilled.Set();
		}


		/// <summary>
		/// Sets a parameter of the interface whose data type is <see cref="int"/>.
		/// 
		/// Possible Win32 errors:
		/// ERROR_ACCESS_DENIED: The caller does not have sufficient permissions to perform the requested operation.
		/// ERROR_GEN_FAILURE: The parameter specified by OpCode is not supported by the driver or NIC.
		/// ERROR_INVALID_HANDLE: The handle hClientHandle was not found in the handle table.
		/// ERROR_INVALID_PARAMETER: One parameter is likely NULL
		/// RPC_STATUS: Various return codes to indicate errors occurred when connecting.
		/// </summary>
		/// <param name="opCode">The opcode of the parameter.</param>
		/// <param name="value">The value to set.</param>
		private void SetInterfaceInt(WlanIntfOpcode opCode, int value)
		{
			IntPtr valuePtr = Marshal.AllocHGlobal(sizeof(int));
			Marshal.WriteInt32(valuePtr, value);

			try
			{
				WlanInterop.ThrowIfError(WlanInterop.WlanSetInterface(_client.ClientHandle, _info.interfaceGuid, opCode, sizeof(int), valuePtr, IntPtr.Zero));
			}
			finally
			{
				Marshal.FreeHGlobal(valuePtr);
			}
		}

		/// <summary>
		/// Gets a parameter of the interface whose data type is <see cref="int"/>.
		/// 
		/// Possible Win32 errors:
		/// ERROR_ACCESS_DENIED: The caller does not have sufficient permissions to perform the requested operation.
		/// ERROR_INVALID PARAMETER: hClientHandle is NULL or invalid, pInterfaceGuid is NULL, pReserved is not NULL, ppData is NULL, or pdwDataSize is NULL.
		/// ERROR_INVALID_HANDLE: The handle hClientHandle was not found in the handle table.
		/// ERROR_INVALID_STATE: OpCode is set to wlan_intf_opcode_current_connection and the client is not currently connected to a network.
		/// ERROR_NOT_ENOUGH_MEMORY: Failed to allocate memory for the query results.
		/// RPC_STATUS: Various error codes.
		/// </summary>
		/// <param name="opCode">The opcode of the parameter.</param>
		/// <returns>The integer value.</returns>
		private int GetInterfaceInt(WlanIntfOpcode opCode)
		{
			IntPtr valuePtr;
			int valueSize;
			WlanOpcodeValueType opcodeValueType;

			WlanInterop.ThrowIfError(WlanInterop.WlanQueryInterface(_client.ClientHandle, _info.interfaceGuid, opCode, IntPtr.Zero, out valueSize, out valuePtr, out opcodeValueType));

			try
			{
				return Marshal.ReadInt32(valuePtr);
			}
			finally
			{
				WlanInterop.WlanFreeMemory(valuePtr);
			}
		}

	    /// <summary>
	    /// Get Radio State
	    /// </summary>
	    /// <returns>Radio State</returns>
	    internal WlanRadioState GetRadioState()
	    {
	        IntPtr valuePtr;
	        int valueSize;
	        WlanOpcodeValueType opcodeValueType;

	        WlanInterop.ThrowIfError(WlanInterop.WlanQueryInterface(_client.ClientHandle, _info.interfaceGuid, WlanIntfOpcode.RadioState, IntPtr.Zero, out valueSize, out valuePtr, out opcodeValueType));

	        try
	        {
	            return (WlanRadioState)Marshal.PtrToStructure(valuePtr, typeof(WlanRadioState));
	        }
	        finally
	        {
	            WlanInterop.WlanFreeMemory(valuePtr);
	        }
	    }


        //It Will Change throwiferror to this.
        private string GetErrorMessage(int errorCode)
	    {
	        int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
	        int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
	        int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
	        int messageSize = 255;
	        String lpMsgBuf = "";
	        int dwFlags = FORMAT_MESSAGE_ALLOCATE_BUFFER |
	                      FORMAT_MESSAGE_FROM_SYSTEM |
	                      FORMAT_MESSAGE_IGNORE_INSERTS;
	        IntPtr ptrlpSource = new IntPtr();
	        IntPtr prtArguments = new IntPtr();
	        int retVal = WlanInterop.FormatMessage(dwFlags, ref ptrlpSource, errorCode, 0,
	            ref lpMsgBuf, messageSize,
	            ref prtArguments);
	        if (0 == retVal)
	        {
	            throw new Exception("Failed to format message for error code " +
	                                errorCode + ". ");
	        }
	        return lpMsgBuf;
	    }

    }
}
