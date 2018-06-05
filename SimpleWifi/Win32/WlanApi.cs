using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text;
using SimpleWifi.Win32.Interop;
using SimpleWifi.Win32.Helpers;

namespace SimpleWifi.Win32
{
	/// <summary>
	/// Represents a client to the Zeroconf (Native Wifi) service.
	/// </summary>
	/// <remarks>
	/// This class is the entrypoint to Native Wifi management. To manage WiFi settings, create an instance
	/// of this class.
	/// </remarks>
	public class WlanClient
	{
		internal IntPtr ClientHandle;
		internal uint NegotiatedVersion;
		internal WlanInterop.WlanNotificationCallbackDelegate WlanNotificationCallback;

		private Dictionary<Guid,WlanInterface> _ifaces = new Dictionary<Guid,WlanInterface>();

		private const int NO_WIFI = 1062;

		public bool NoWifiAvailable = false;

		/// <summary>
		/// Creates a new instance of a Native Wifi service client.
		/// Throws Win32 errors: ERROR_INVALID_PARAMETER, ERROR_NOT_ENOUGH_MEMORY, RPC_STATUS, ERROR_REMOTE_SESSION_LIMIT_EXCEEDED.
		/// </summary>
		public WlanClient()
		{
			int errorCode = 0;
			OperatingSystem osInfo = Environment.OSVersion;			

			bool isWinXP = 
				osInfo.Platform == PlatformID.Win32NT && 
				osInfo.Version.Major == 5 && 
				osInfo.Version.Minor != 0;
						
			if (isWinXP && osInfo.ServicePack == "Service Pack 1") // wlanapi not supported in sp1 (or sp2 without hotfix)
			{
				errorCode = NO_WIFI;                
			}
			else
			{
				// Perform exception safe init
				// It can be SP2 without hotfix which would generate exception
				try
				{
					errorCode = WlanInterop.WlanOpenHandle(WlanInterop.WLAN_CLIENT_VERSION_XP_SP2, IntPtr.Zero, out NegotiatedVersion, out ClientHandle);					
				}
				catch
				{
					errorCode = NO_WIFI;
				}                
			}
			
			if (errorCode != 0)
			{
				NoWifiAvailable = true;
				return;
			}

			// 1062 = no wifi
			// OK!
			// WlanInterop.ThrowIfError(errorCode);

			try
			{
				// Interop callback
				WlanNotificationCallback = new WlanInterop.WlanNotificationCallbackDelegate(OnWlanNotification);

				WlanNotificationSource prevSrc;
				WlanInterop.ThrowIfError(WlanInterop.WlanRegisterNotification(ClientHandle, WlanNotificationSource.All, false, WlanNotificationCallback, IntPtr.Zero, IntPtr.Zero, out prevSrc));
			}
			catch
			{
				WlanInterop.WlanCloseHandle(ClientHandle, IntPtr.Zero);
				throw;
			}
		}
		
		~WlanClient()
		{
			// Free the handle when deconstructing the client. There won't be a handle if its xp sp 2 without wlanapi installed
			try
			{
				WlanInterop.WlanCloseHandle(ClientHandle, IntPtr.Zero);
			}
		    catch
		    {
		        // ignored
		    }
		}

		// Called from interop
		private void OnWlanNotification(ref WlanNotificationData notifyData, IntPtr context)
		{
			if (NoWifiAvailable)
				return;

			WlanInterface wlanIface = _ifaces.ContainsKey(notifyData.interfaceGuid) ? _ifaces[notifyData.interfaceGuid] : null;

		    if (notifyData.notificationSource == WlanNotificationSource.ACM)
		        switch ((WlanNotificationCodeAcm) notifyData.notificationCode)
		        {
		            case WlanNotificationCodeAcm.ConnectionStart:
		            case WlanNotificationCodeAcm.ConnectionComplete:
		            case WlanNotificationCodeAcm.ConnectionAttemptFail:
		            case WlanNotificationCodeAcm.Disconnecting:
		            case WlanNotificationCodeAcm.Disconnected:
		                WlanConnectionNotificationData? connNotifyData =
		                    WlanHelpers.ParseWlanConnectionNotification(ref notifyData);

		                if (connNotifyData.HasValue)
		                    wlanIface?.OnWlanConnection(notifyData, connNotifyData.Value);

		                break;
		            case WlanNotificationCodeAcm.ScanFail:
		                int expectedSize = Marshal.SizeOf(typeof(int));

		                if (notifyData.dataSize >= expectedSize)
		                {
		                    int reasonInt = Marshal.ReadInt32(notifyData.dataPtr);

		                    // Want to make sure this doesn't crash if windows sends a reasoncode not defined in the enum.
		                    if (Enum.IsDefined(typeof(WlanReasonCode), reasonInt))
		                    {
		                        WlanReasonCode reasonCode = (WlanReasonCode) reasonInt;

		                        wlanIface?.OnWlanReason(notifyData, reasonCode);
		                    }
		                }

		                break;
		        }
		    else if (notifyData.notificationSource == WlanNotificationSource.MSM)
		    {
		        switch ((WlanNotificationCodeMsm) notifyData.notificationCode)
		        {
		            case WlanNotificationCodeMsm.Associating:
		            case WlanNotificationCodeMsm.Associated:
		            case WlanNotificationCodeMsm.Authenticating:
		            case WlanNotificationCodeMsm.Connected:
		            case WlanNotificationCodeMsm.RoamingStart:
		            case WlanNotificationCodeMsm.RoamingEnd:
		            case WlanNotificationCodeMsm.Disassociating:
		            case WlanNotificationCodeMsm.Disconnected:
		            case WlanNotificationCodeMsm.PeerJoin:
		            case WlanNotificationCodeMsm.PeerLeave:
		            case WlanNotificationCodeMsm.AdapterRemoval:
		                WlanConnectionNotificationData? connNotifyData =
		                    WlanHelpers.ParseWlanConnectionNotification(ref notifyData);

		                if (connNotifyData.HasValue)
		                    wlanIface?.OnWlanConnection(notifyData, connNotifyData.Value);

		                break;
		        }
		    }

		    wlanIface?.OnWlanNotification(notifyData);
		}

		/// <summary>
		/// Gets the WLAN interfaces.
		/// 
		/// Possible Win32 exceptions:
		/// 
		/// ERROR_INVALID_PARAMETER: A parameter is incorrect. This error is returned if the hClientHandle or ppInterfaceList parameter is NULL. This error is returned if the pReserved is not NULL. This error is also returned if the hClientHandle parameter is not valid.
		/// ERROR_INVALID_HANDLE: The handle hClientHandle was not found in the handle table.
		/// RPC_STATUS: Various error codes.
		/// ERROR_NOT_ENOUGH_MEMORY: Not enough memory is available to process this request and allocate memory for the query results.
		/// </summary>
		/// <value>The WLAN interfaces.</value>
		public WlanInterface[] Interfaces
		{
			get
			{
				if (NoWifiAvailable)
					return null;
				IntPtr ifaceList;

				WlanInterop.ThrowIfError(WlanInterop.WlanEnumInterfaces(ClientHandle, IntPtr.Zero, out ifaceList)); 

				try
				{
					WlanInterfaceInfoListHeader header = (WlanInterfaceInfoListHeader) Marshal.PtrToStructure(ifaceList, typeof (WlanInterfaceInfoListHeader));
					
					Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);
					WlanInterface[] interfaces = new WlanInterface[header.numberOfItems];
					List<Guid> currentIfaceGuids = new List<Guid>();

					for (int i = 0; i < header.numberOfItems; ++i)
					{
						WlanInterfaceInfo info = (WlanInterfaceInfo) Marshal.PtrToStructure(new IntPtr(listIterator), typeof(WlanInterfaceInfo));

						listIterator += Marshal.SizeOf(info);
						currentIfaceGuids.Add(info.interfaceGuid);

						WlanInterface wlanIface = _ifaces.ContainsKey(info.interfaceGuid) ? _ifaces[info.interfaceGuid] : new WlanInterface(this, info);

						interfaces[i] = wlanIface;
						_ifaces[info.interfaceGuid] = wlanIface;
					}

					// Remove stale interfaces
					Queue<Guid> deadIfacesGuids = new Queue<Guid>();
					foreach (Guid ifaceGuid in _ifaces.Keys)
					{
						if (!currentIfaceGuids.Contains(ifaceGuid))
							deadIfacesGuids.Enqueue(ifaceGuid);
					}

					while(deadIfacesGuids.Count != 0)
					{
						Guid deadIfaceGuid = deadIfacesGuids.Dequeue();
						_ifaces.Remove(deadIfaceGuid);						
					}

					return interfaces;
				}
				finally
				{
					WlanInterop.WlanFreeMemory(ifaceList);
				}
			}
		}
	}
}
