using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SimpleWifi
{
    public class XMLParser
    {
        public static ParsedWlanProfileInfo ProfileXmlParsing(string XmlString)
        {
            ParsedWlanProfileInfo info = new ParsedWlanProfileInfo();

            try
            {
                bool temp;
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(XmlString);

                XmlNodeList profile = xml.GetElementsByTagName("WLANProfile");
                if (profile.Count == 0) //it's not wlan profile xml file.
                    return null;
                //name node must have in xml
                var logicalName = profile[0].ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("name"));

                if (string.IsNullOrEmpty(logicalName.InnerText))
                    return null; //error it is not completed xml file.
                info.LogicaName = logicalName.InnerText;

                var SSIDConfig = profile[0].ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("SSIDConfig"));
                var SSID = SSIDConfig.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("SSID"));
                var name = SSID.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("name"));
                var hex = SSID.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("hex"));
                var nonBroadcast = SSIDConfig.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("nonBroadcast"));

                info.Name = name?.InnerText;
                info.Hex = hex?.InnerText;
                if (nonBroadcast!= null && !string.IsNullOrEmpty(nonBroadcast.InnerText) && bool.TryParse(nonBroadcast.InnerText, out temp))
                    info.NonBroadcast = temp;

                var connectionType = profile[0].ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("connectionType"));
                var connectionMode = profile[0].ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("connectionMode"));
                info.ConnectionType = connectionType?.InnerText;
                info.ConnectionMode = connectionMode?.InnerText;
                var autoSwitch = profile[0].ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("autoSwitch"));
                if (autoSwitch != null && !string.IsNullOrEmpty(autoSwitch.InnerText) && bool.TryParse(autoSwitch.InnerText, out temp))
                    info.AutoSwitch = temp;

                var MSM = profile[0].ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("MSM"));
                var security = MSM.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("security"));
                var authEncryption = security.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("authEncryption"));
                var authentication = authEncryption.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("authentication"));
                var encryption = authEncryption.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("encryption"));
                var useOneX = authEncryption.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("useOneX"));
                info.Authentication = authentication?.InnerText;
                info.Encryption = encryption?.InnerText;
                if (useOneX != null && !string.IsNullOrEmpty(useOneX.InnerText) && bool.TryParse(useOneX.InnerText, out temp))
                    info.useOneX = temp;

                if (info.useOneX != null && info.useOneX == true)
                {
                    var OneX = MSM.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("OneX"));
                    var EAPConfig = OneX.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("EAPConfig"));
                    var EapHostConfig = EAPConfig.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("EapHostConfig"));
                    var EapMethod = EapHostConfig.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("EapMethod"));
                    if (EapMethod != null)
                    {
                        var EapCommonType = EapMethod.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("eapCommon:Type"));
                        var EapCommonAuthorId = EapMethod.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("eapCommon:AuthorId"));
                    }

                    var Config = EapHostConfig.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("Config"));
                    if (Config != null)
                    {
                        var BaseEapEap = Config.ChildNodes.Cast<XmlNode>().First(n => n.Name.Equals("baseEap:Eap"));
                        if (BaseEapEap != null)
                        {
                            var BaseEapType = BaseEapEap.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("baseEap:Type"));
                            #region Ms Peap
                            var msPeapEapType = BaseEapEap.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("msPeap:EapType"));
                            if (msPeapEapType != null)
                            {
                                var msPeapServerValidation = msPeapEapType.ChildNodes.Cast<XmlNode>().First(n=>n.Name.Equals("msPeap:ServerValidation"));
                                if (msPeapServerValidation != null)
                                {
                                    var msPeapDisableUserPromptForServerValidation = msPeapServerValidation.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("msPeap:DisableUserPromptForServerValidation"));
                                    var msPeapTrustedRootCA = msPeapServerValidation.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("msPeap:TrustedRootCA"));
                                }
                                var msPeapFastReconnect = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("msPeap:FastReconnect"));
                                var msPeapInnerEapOptional = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("msPeap:InnerEapOptional"));
                                var BaseEapEap2 = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("baseEap:Eap"));
                                if (BaseEapEap2 != null)
                                {
                                    var BaseEapType2 = BaseEapEap2.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("baseEap:Type"));
                                    var msChapV2EapType = BaseEapEap2.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("msChapV2:EapType"));
                                    if(msChapV2EapType !=null)
                                    {
                                        var msChapV2UseWinLogonCredentials = msChapV2EapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("msChapV2:UseWinLogonCredentials"));
                                    }
                                }
                                var msPeapEnableQuarantineChecks = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("msPeap:EnableQuarantineChecks"));
                                var msPeapRequireCryptoBinding = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("msPeap:RequireCryptoBinding"));
                                var msPeapPeapExtensions = msPeapEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("msPeap:PeapExtensions"));
                            }
                            #endregion

                            #region Eap Tls
                            var EapTlsEapType = BaseEapEap.ChildNodes.Cast<XmlNode>().FirstOrDefault(n=>n.Name.Equals("eapTls:EapType"));
                            if (EapTlsEapType != null)
                            {
                                var EapTlsCredentialsSource = EapTlsEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:CredentialsSource"));
                                if (EapTlsCredentialsSource != null)
                                {
                                    var EapCertificateStore = EapTlsCredentialsSource.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:CertificateStore"));
                                }
                                var EapTlsServerValidation = EapTlsEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:CredentialsSource"));
                                if (EapTlsServerValidation != null)
                                {
                                    var eapTlsDisableUserPromptForServerValidation = EapTlsServerValidation.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:DisableUserPromptForServerValidation"));
                                    var eapTlsServerNames = EapTlsServerValidation.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:ServerNames"));
                                }
                                var EapTlsDifferentUserName = EapTlsEapType.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("eapTls:DifferentUsername"));
                            }
                            
                            #endregion
                        }
                    }
                }

                var sharedKey = security.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("sharedKey"));
                if (sharedKey != null)
                {
                    var keyType = sharedKey.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("keyType"));
                    var isprotected = sharedKey.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("protected"));
                    var keyMaterial = sharedKey.ChildNodes.Cast<XmlNode>().FirstOrDefault(n => n.Name.Equals("keyMaterial"));

                    info.KeyType = keyType?.InnerText;
                    if (isprotected != null && !string.IsNullOrEmpty(isprotected.InnerText) && bool.TryParse(isprotected.InnerText, out temp))
                        info.Protected = temp;
                    info.KeyMaterial = keyMaterial?.InnerText;
                }
                
            }
            catch (System.NullReferenceException ex)
            {
                //its not profile xml file.
                return null;
            }
            return info;
        }
        
    }
    public class ParsedWlanProfileInfo //: IEquatable<ParsedWlanProfileInfo>
    {
        /// <summary>
        /// Profile Name
        /// </summary>
        /// <remarks>WLANProfile > name</remarks>
        public string LogicaName { get; set; }
        /// <summary>
        /// SSID Name
        /// </summary>
        /// <remarks>WLANProfile > SSIDConfig > SSID > name</remarks>
        public string Name { get; set; }
        /// <summary>
        /// SSID Hex
        /// </summary>
        /// <remarks>WLANProfile > SSIDConfig > SSID > hex</remarks>
        public string Hex { get; set; }
        /// <summary>
        /// is Non Broadcast
        /// </summary>
        /// <remarks>WLANProfile > SSIDConfig > nonBroadcast</remarks>
        public bool? NonBroadcast { get; set; }
        /// <summary>
        /// Connection Type
        /// </summary>
        /// <remarks>WLANProfile > connectionType</remarks>
        public string ConnectionType { get; set; }
        /// <summary>
        /// Connection Mode
        /// </summary>
        /// <remarks>WLANProfile > connectionMode</remarks>
        public string ConnectionMode { get; set; }
        /// <summary>
        /// Auto Swtich
        /// </summary>
        /// <remarks>WLANProfile > autoSwitch</remarks>
        public bool? AutoSwitch { get; set; }

        /// <summary>
        /// Security Authentication Type
        /// </summary>
        /// <remarks>WLANProfile > MSM > security > authEncryption > authentication</remarks>
        public string Authentication { get; set; }
        /// <summary>
        /// Security Encryption Type
        /// </summary>
        /// <remarks>WLANProfile > MSM > security > authEncryption > encryption</remarks>
        public string Encryption { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>WLANProfile > MSM > security > authEncryption > useOneX</remarks>
        public bool? useOneX { get; set; }
        /// <summary>
        /// Key Type
        /// </summary>
        public string KeyType { get; set; }
        /// <summary>
        /// Is Key Protected
        /// </summary>
        public bool? Protected { get; set; }
        /// <summary>
        /// Key Material (Password)
        /// </summary>
        public string KeyMaterial { get; set; }
        /// <summary>
        /// Is Enable Randomization
        /// </summary>
        public bool? EnableRandomization { get; set; }

        //// Overriding Equals member method, which will call the IEquatable implementation
        //// if appropriate.
        //public override bool Equals(Object obj)
        //{
        //    var other = obj as ParsedWlanProfileInfo;
        //    if (other == null) return false;

        //    return Equals(other);
        //}
        //public override int GetHashcode()
        //{
        //    // Provide own implementation
        //}

        //public bool Equals(ParsedWlanProfileInfo other)
        //{
        //    if (other == null)
        //        return false;
        //    if(ReferenceEquals(this,other))
        //    {
        //        return true;
        //    }
        //    return true;
        //}
    }
}
