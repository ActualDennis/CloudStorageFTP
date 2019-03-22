using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CloudStorage.Server.Helpers {
    public static class XmlConfigParser {
        public static string ConfigDefaultLocation => "Configuration.xml";
        public static Configuration ParseSettings()
        {
            var settingsFile = new XmlDocument();
            settingsFile.Load(ConfigDefaultLocation);

            return new Configuration()
            {
                BaseDirectory = settingsFile["ConfigVariables"]["BaseServerDirectory"].FirstChild.Value,
                CertificateLocation = settingsFile["ConfigVariables"]["SSLCertificatePath"].FirstChild.Value,
                FtpControlPort = int.Parse(settingsFile["ConfigVariables"]["CommandsFtpPort"].FirstChild.Value),
                LoggingPath = settingsFile["ConfigVariables"]["LoggingPath"].FirstChild.Value,
                MinPort = int.Parse(settingsFile["ConfigVariables"]["PortRangeMaximum"].FirstChild.Value),
                MaxPort = int.Parse(settingsFile["ConfigVariables"]["PortRangeMinimum"].FirstChild.Value),
                ServerExternalIP = settingsFile["ConfigVariables"]["ServerExternalIP"].FirstChild.Value
            };
        }
    }
}
