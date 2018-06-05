using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SimpleWifi
{
	public class AuthRequest
	{
	    private readonly bool _isEAPStore;
	    private readonly WlanAvailableNetwork _network;
		private readonly WlanInterface _interface;

		public AuthRequest(AccessPoint ap)
		{	
			_network	= ap.Network;
			_interface	= ap.Interface;

			IsPasswordRequired = 
				_network.securityEnabled &&
				_network.dot11DefaultCipherAlgorithm != Dot11CipherAlgorithm.None;

			_isEAPStore =
				_network.dot11DefaultAuthAlgorithm == Dot11AuthAlgorithm.RSNA ||
				_network.dot11DefaultAuthAlgorithm == Dot11AuthAlgorithm.WPA;

			IsUsernameRequired = _isEAPStore;
			IsDomainSupported	= _isEAPStore;
		}
		
		public bool IsPasswordRequired { get; }
	    public bool IsUsernameRequired { get; }
	    public bool IsDomainSupported { get; }

	    public string Password { get; set; }

	    public string Username { get; set; }

	    public string Domain { get; set; }

	    public bool IsPasswordValid
		{
			get
			{
				#warning Robin: Not sure that Enterprise networks have the same requirements on the password complexity as standard ones.
				return PasswordHelper.IsValid(Password, _network.dot11DefaultCipherAlgorithm);
			}
		}
		
		private bool SaveToEAP() 
		{
			if (!_isEAPStore || !IsPasswordValid)
				return false;

			string userXML = EapUserFactory.Generate(_network.dot11DefaultCipherAlgorithm, Username, Password, Domain);
			_interface.SetEAP(_network.profileName, userXML);

			return true;		
		}

		internal bool Process()
		{
			if (!IsPasswordValid)
				return false;
			
			string profileXML = ProfileFactory.Generate(_network, Password);
			_interface.SetProfile(WlanProfileFlags.AllUser, profileXML, true);

			if (_isEAPStore && !SaveToEAP())
				return false;			
			
			return true;
		}

        public void RequestPasswordConsole(AccessPoint ap, bool useVBInputBox = false)
        {
            AuthRequest authRequest = null;
            try
            {
                // Auth
                authRequest = new AuthRequest(ap);
            }
            catch (System.NullReferenceException e)
            {
                //If null, ap banished when try to connect
                return;

            }
            bool overwrite = true;

            if (authRequest.IsPasswordRequired)
            {
                if (ap.HasProfile)
                // If there already is a stored profile for the network, we can either use it or overwrite it with a new password.
                {
                    ///For Console
                    Console.Write("\r\nA network profile already exist, do you want to use it (y/n)? ");
                    if (Console.ReadLine().ToLower() == "y")
                    {
                        overwrite = false;
                    }
                }

                if (overwrite)
                {
                    ///For Console
                    if (authRequest.IsUsernameRequired)
                    {
                        if (useVBInputBox)
                            authRequest.Username =
                                Microsoft.VisualBasic.Interaction.InputBox("Please Enter the UserName?", "UserName",
                                    "");
                        else
                        {
                            Console.Write("\r\nPlease enter a username: ");
                            authRequest.Username = Console.ReadLine();
                        }
                    }

                    //PasswordPrompt
                    bool validPassFormat = false;
                    while (!validPassFormat)
                    {
                        string password;

                        if (useVBInputBox)
                            password = Microsoft.VisualBasic.Interaction.InputBox("Please Enter the Password?", "Password", "");
                        else
                        {
                            Console.Write("\r\nPlease enter the wifi password: ");
                            password = Console.ReadLine();
                        }

                        validPassFormat = ap.IsValidPassword(password);
                        if (!validPassFormat)
                            Console.WriteLine("\r\nPassword is not valid for this network type.");
                        else
                        {
                            authRequest.Password = password;
                        }
                    }

                    if (!authRequest.IsDomainSupported) return;
                    if (useVBInputBox)
                        authRequest.Domain =
                            Microsoft.VisualBasic.Interaction.InputBox("Please Enter the Domain?", "Domain", "");
                    else
                    {
                        Console.Write("\r\nPlease enter a domain: ");
                        authRequest.Domain = Console.ReadLine();
                    }
                }
            }
        }

        public void RequestPasswordWinForm(AccessPoint ap)
        {
            AuthRequest authRequest = null;
            try
            {
                // Auth
                authRequest = new AuthRequest(ap);
            }
            catch (System.NullReferenceException e)
            {
                //If null, ap banished when try to connect
                return;

            }
            bool overwrite = true;

            if (authRequest.IsPasswordRequired)
            {
                if (ap.HasProfile)
                // If there already is a stored profile for the network, we can either use it or overwrite it with a new password.
                {
                    ///For Windows Form
                    if (MessageBox.Show("Do you Want Connect using network profile?", "Using Profile", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        overwrite = false;
                    }
                }

                if (overwrite)
                {
                    ///For Windows Form
                    if (authRequest.IsUsernameRequired)
                    {
                        authRequest.Username = Microsoft.VisualBasic.Interaction.InputBox("Please Enter the UserName?", "UserName", "");
                    }

                    //PasswordPrompt
                    bool validPassFormat = false;
                    while (!validPassFormat)
                    {
                        string password;

                        password = Microsoft.VisualBasic.Interaction.InputBox("Please Enter the Password?", "Password", "");

                        validPassFormat = ap.IsValidPassword(password);
                        if (!validPassFormat)
                        {
                            if (MessageBox.Show(
                                    "Password is not valid for this network type.\n Do you want to re-enter the Password?",
                                    "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                                break;
                        }
                        else
                        {
                            authRequest.Password = password;
                        }
                    }

                    if (authRequest.IsDomainSupported)
                    {
                        authRequest.Domain = Microsoft.VisualBasic.Interaction.InputBox("Please Enter the Domain?", "Domain", "");
                    }
                }
            }
        }
    }

	public static class PasswordHelper
	{
		/// <summary>
		/// Checks if a password is valid for a cipher type.
		/// </summary>
		public static bool IsValid(string password, Dot11CipherAlgorithm cipherAlgorithm)
		{
			switch (cipherAlgorithm)
			{
				case Dot11CipherAlgorithm.None:
					return true;
				case Dot11CipherAlgorithm.WEP: // WEP key is 10, 26 or 40 hex digits long.
					if (string.IsNullOrEmpty(password))
						return false;

					int len = password.Length;

					bool correctLength = len == 10 || len == 26 || len == 40;
					bool onlyHex = new Regex("^[0-9A-F]+$").IsMatch(password);

					return correctLength && onlyHex;
				case Dot11CipherAlgorithm.CCMP: // WPA2-PSK 8 to 63 ASCII characters					
				case Dot11CipherAlgorithm.TKIP: // WPA-PSK 8 to 63 ASCII characters
					if (string.IsNullOrEmpty(password))
						return false;

					return 8 <= password.Length && password.Length <= 63;
				default:
					return true;
			}
		}
	}
}
