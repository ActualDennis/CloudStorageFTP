using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CloudStorage.Server.Data;
using CloudStorage.Server.Misc;

namespace CloudStorage.Server.Logging
{
    public class AutomaticFileLogger : FileLogger
    {
        public AutomaticFileLogger()
        {
            ActionsTracker.OnConnectionSecurityChanged += ConnectionSecurityChanged;
            ActionsTracker.OnUserConnected += UserConnected;
            ActionsTracker.OnUserDisconnected += UserDisconnected;
            ActionsTracker.OnUserAuthenticated += UserAuthenticated;
        }

        private static readonly object padlock = new object();

        private static AutomaticFileLogger _instance;
        
        public static AutomaticFileLogger Instance
        {
            get
            {
                lock (padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new AutomaticFileLogger();
                    }
                    return _instance;
                }
            }
        }
        public new void Log(string message, RecordKind kindOfRecord)
        {
            base.Log(message, kindOfRecord);
        }

        public void EnableLogging()
        {
            if (_isLoggingEnabled)
                return;

            ActionsTracker.OnConnectionSecurityChanged += ConnectionSecurityChanged;
            ActionsTracker.OnUserConnected += UserConnected;
            ActionsTracker.OnUserDisconnected += UserDisconnected;
            ActionsTracker.OnUserAuthenticated += UserAuthenticated;
            _isLoggingEnabled = true;

        }

        public void DisableLogging()
        {
            if (!_isLoggingEnabled)
                return;

            ActionsTracker.OnConnectionSecurityChanged -= ConnectionSecurityChanged;
            ActionsTracker.OnUserConnected -= UserConnected;
            ActionsTracker.OnUserDisconnected -= UserDisconnected;
            ActionsTracker.OnUserAuthenticated -= UserAuthenticated;
            _isLoggingEnabled = false;
        }

        public void UserConnected(object sender, EndPoint e)
        {
            base.Log($"User connected: {((IPEndPoint)e).ToString()}",RecordKind.Status);
        }
        public void UserDisconnected(object sender, EndPoint e)
        {
            base.Log($"User disconnected: {((IPEndPoint)e).ToString()}", RecordKind.Status);
        }

        public void UserAuthenticated(object sender, UserAuthenticatedEventArgs e)
        {
            base.Log($"Endpoint {((IPEndPoint)e.EndPoint).ToString()} authenticated as : {e.UserName}", RecordKind.Status);
        }

        public void ConnectionSecurityChanged(object sender, ConnectionSecurityChangedEventArgs e)
        {
            base.Log($"Connection security of {((IPEndPoint)e.EndPoint).ToString()} is now: '{e.Security.ToString()}'", RecordKind.Status);
        }
    }
}
