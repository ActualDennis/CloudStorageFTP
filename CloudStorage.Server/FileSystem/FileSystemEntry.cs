using System;

namespace CloudStorage.Server.FileSystem
{
    public class FileSystemEntry
    {
        public FileSystemEntryType EntryType;

        public long OccupiedSpace;

        public bool IsReadOnly;

        public DateTime LastWriteTime;
        public string Name;
    }
}