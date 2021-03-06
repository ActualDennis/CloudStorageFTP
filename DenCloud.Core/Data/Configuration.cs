﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DenCloud.Core.Data {
    public class Configuration {
        public string CertificateLocation;
        public string BaseDirectory;
        public int FtpControlPort;
        public string ServerExternalIP;
        public string LoggingPath;
        public int MaxPort;
        public int MinPort;
        public int PassiveConnectionRetryFor;
    }
}
