using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleWifi.Win32;

namespace SimpleWifi.IO
{
    public class XML
    {
        public static string Path = System.IO.Path.Combine(Environment.CurrentDirectory, "Profiles");

        public IEnumerable<string> EnumerateXmlFilesName()
        {
            if (CheckExistsPath(true))
            {
                foreach (var filename in Directory.EnumerateFiles(Path, "*.xml"))
                {
                    var sr = new StreamReader(System.IO.Path.HasExtension(filename) ? filename : System.IO.Path.Combine(Path, filename) + ".xml", Encoding.UTF8);

                    if (XMLParser.isProfileXml(sr.ReadToEnd()))
                    {
                        sr.Close();
                        yield return filename;
                    }
                    else
                    {
                        sr.Close();
                    }
                }
            }
            else
            {
                yield return null;
            }
        }

        public IEnumerable<ParsedWlanProfileInfo> EnumerateParsedWlanProfileInfos()
        {
            if (CheckExistsPath(true))
            {
                foreach (var filename in Directory.EnumerateFiles(Path, "*.xml"))
                {
                    var sr = new StreamReader(System.IO.Path.HasExtension(filename) ? filename : System.IO.Path.Combine(Path, filename) + ".xml", Encoding.UTF8);
                    var info = XMLParser.ProfileXmlParsing(sr.ReadToEnd());

                    sr.Close();

                    if (info != null)
                    {
                        yield return info;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isCreate">if true, Create Folder if dont exists Path, it always retrun true.</param>
        private bool CheckExistsPath(bool isCreate)
        {
            if (!System.IO.Directory.Exists(Path))
            {
                if (isCreate)
                {
                    System.IO.Directory.CreateDirectory(Path);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ap"></param>
        /// <param name="isProtected"></param>
        /// <returns></returns>
        public bool SaveProfileXml(AccessPoint ap,bool isProtected = true)
        {
            if (CheckExistsPath(true))
            {
                string path = System.IO.Path.Combine(Path, ap.Name + ".xml");

                string xml = ap.GetProfileXML(isProtected);

                if (!string.IsNullOrEmpty(xml))
                {
                    try
                    {
                        File.WriteAllText(path, xml);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool SaveProfileXml(string profileName, WlanInterface wlanIface, bool isProtected = true)
        {
            if (CheckExistsPath(true))
            {
                string path = System.IO.Path.Combine(Path, profileName + ".xml");

                var xml = wlanIface.GetProfileXml(profileName, isProtected);

                if (!string.IsNullOrEmpty(xml))
                {
                    try
                    {
                        File.WriteAllText(path, xml);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool DeleteProfile(string profileName)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path);
                var files = di.GetFiles();

                files.AsParallel().Where(f => f.Name.Contains(profileName)).ForAll((f) => f.Delete());
                return true;
            }
            catch
            {
                return false;
            }

            
        }
    }
}
