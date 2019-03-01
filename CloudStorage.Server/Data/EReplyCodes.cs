namespace CloudStorage.Server.Data
{
    public enum FtpReplyCode
    {
        Okay = 200,
        SystemStatus = 211,
        FileStatus = 213,
        CommandUnrecognized = 500,
        SyntaxErrorInParametersOrArguments = 501,
        NotImplemented = 502,
        ParameterNotImplemented = 504,
        BadSequence = 503,
        ServiceReady = 220,
        UserLoggedIn = 230,
        NotLoggedIn = 530,
        NeedPassword = 331,
        LocalError = 451,
        PathCreated = 257,
        TransferStarting = 125,
        SuccessClosingDataConnection = 226,
        FileActionOk = 250,
        FileBusy = 450,
        FileNoAccess = 550,
        FileSpaceInsufficient = 452,
        EnteringPassiveMode = 227,
        EnteringExtendedPassiveMode = 229,
        AboutToOpenDataConnection = 150,
        SystemTypeName = 215,
        FileActionPendingInfo = 350,
        NotSupportedProtocol = 522
    }
}