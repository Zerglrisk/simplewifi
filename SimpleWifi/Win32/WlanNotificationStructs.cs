using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleWifi.Win32
{
	internal struct WlanConnectionNotificationEventData
	{
		public WlanNotificationData NotifyData;
		public WlanConnectionNotificationData ConnNotifyData;
	}

	internal struct WlanReasonNotificationData
	{
		public WlanNotificationData NotifyData;
		public WlanReasonCode ReasonCode;
	}
}
