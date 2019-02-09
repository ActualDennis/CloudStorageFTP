namespace CloudStorage.Server.Data
{
    public static class FtpCommands
    {
        public const string DeleteFile = "DELE";

        public const string RemoveDirectory = "RMD";

        public const string CreateDirectory = "MKD";

        public const string PrintDirectory = "PWD";

        public const string UserLogin = "USER";

        public const string UserPassword = "PASS";

        public const string ActiveConnection = "PORT";

        public const string PassiveConnection = "PASV";

        public const string ExtendedPassiveConnection = "EPSV";

        public const string Quit = "QUIT";

        public const string ChangeTransferType = "TYPE";

        public const string DownloadFile = "RETR";

        public const string UploadFile = "STOR";

        public const string ChangeWorkingDirectory = "CWD";

        public const string DirectoryListing = "LIST";

        public const string KeepAlive = "NOOP";

        public const string EnableEncryption = "AUTH";

        public const string SystemType = "SYST";

        public const string FeatureList = "FEAT";

        public const string TransmissionMode = "MODE";

        public const string RenameFrom = "RNFR";

        public const string RenameTo = "RNTO";

        public const string GoUp = "CDUP";

        public const string SiteSpecific = "SITE";

        public const string Size = "SIZE";

        public const string Options = "OPTS";

        public const string DataChannelProtection = "PROT";

        public const string DataChannelBufferSize = "PBSZ";

        public const string ExtendedDirectoryListing = "MLSD";
        
        public const string FileorDirectoryInfo = "MLST";

        public const string ClientInfo = "CLNT";

        public const string NameListing = "NLST";

        public const string FileLastModifiedTime = "MDTM";
    }
}