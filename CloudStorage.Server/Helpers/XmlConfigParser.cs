using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CloudStorage.Server.Helpers {
    public static class XmlConfigHelper {
        public static string ConfigDefaultLocation => "Configuration.xml";
        public static Configuration ParseSettings()
        {
            var settingsFile = new XmlDocument();
            settingsFile.Load(ConfigDefaultLocation);

            return new Configuration()
            {
                BaseDirectory = settingsFile["ConfigVariables"]["BaseServerDirectory"].FirstChild.Value,
                CertificateLocation = settingsFile["ConfigVariables"]["SSLCertificatePath"].FirstChild?.Value,
                FtpControlPort = int.Parse(settingsFile["ConfigVariables"]["CommandsFtpPort"].FirstChild.Value),
                LoggingPath = settingsFile["ConfigVariables"]["LoggingPath"].FirstChild?.Value,
                MaxPort = int.Parse(settingsFile["ConfigVariables"]["PortRangeMaximum"].FirstChild.Value),
                MinPort = int.Parse(settingsFile["ConfigVariables"]["PortRangeMinimum"].FirstChild.Value),
                ServerExternalIP = settingsFile["ConfigVariables"]["ServerExternalIP"].FirstChild.Value
            };
        }

        public static void GenerateConfigFile(
            string serverBaseDir,
            string SslPath,
            string ftpPort,
            string loggingPath,
            string PortMax,
            string PortMin,
            string ServerIP)
        {
            using (XmlWriter writer = XmlWriter.Create(ConfigDefaultLocation))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("ConfigVariables");
                writer.WriteElementString("BaseServerDirectory", serverBaseDir);
                writer.WriteElementString("SSLCertificatePath", SslPath);
                writer.WriteElementString("CommandsFtpPort", ftpPort);
                writer.WriteElementString("LoggingPath", loggingPath);
                writer.WriteElementString("PortRangeMaximum", PortMax);
                writer.WriteElementString("PortRangeMinimum", PortMin);
                writer.WriteElementString("ServerExternalIP", ServerIP);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

    }
}
