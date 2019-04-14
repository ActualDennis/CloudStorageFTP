using System.ComponentModel.DataAnnotations;

namespace DenCloud.Core
{
    public class FtpUser
    {
        [Key] public string Id { get; set; }

        public string Name { get; set; }

        public string PasswordHash { get; set; }

        public bool IsDisabled { get; set; }

        public long StorageBytesTotal { get; set; }
    }
}