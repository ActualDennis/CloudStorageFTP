using CloudStorage.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Server.Misc
{
    /// <summary>
    /// Class used to track users' state
    /// </summary>
    public static class ActionsTracker
    {
        static ActionsTracker()
        {
        }

        public static Dictionary<EndPoint, UserInformation> UsersInfo { get; private set; } = new Dictionary<EndPoint, UserInformation>();

        /// <summary>
        /// Typically , other classes should subscribe to these events
        /// and another classes should call methods to trigger these events
        /// </summary>
        public static event EventHandler<EndPoint> OnUserConnected;

        public static event EventHandler<EndPoint> OnUserDisconnected;

        public static event EventHandler<UserAuthenticatedEventArgs> OnUserAuthenticated;
        
        public static event EventHandler<ConnectionSecurityChangedEventArgs> OnConnectionSecurityChanged;

        public static bool IsUserOnline(EndPoint e) => UsersInfo.ContainsKey(e) ? true : false;

        public static void UserConnected(object sender, EndPoint e)
        {
            OnUserConnected?.Invoke(null, e);

            if (!UsersInfo.ContainsKey(e))
            {
                UsersInfo.Add(e, new UserInformation() { Security = ConnectionSecurity.NonSecure });
            }
        }
        public static void UserDisconnected(object sender, EndPoint e)
        {
            OnUserDisconnected?.Invoke(null, e);

            if (UsersInfo.ContainsKey(e))
                UsersInfo.Remove(e);
        }

        public static void UserAuthenticated(object sender, UserAuthenticatedEventArgs e)
        {
            OnUserAuthenticated?.Invoke(null, e);

            var userInfo = UsersInfo[e.EndPoint];
            userInfo.IsAuthenticated = true;
            userInfo.UserName = e.UserName;
            UsersInfo[e.EndPoint] = userInfo;
        }

        public static void ConnectionSecurityChanged(object sender, ConnectionSecurityChangedEventArgs e)
        {
            OnConnectionSecurityChanged?.Invoke(null, e);

            var userInfo = UsersInfo[e.EndPoint];
            userInfo.Security = e.Security;
            UsersInfo[e.EndPoint] = userInfo;
        }


    }
}
