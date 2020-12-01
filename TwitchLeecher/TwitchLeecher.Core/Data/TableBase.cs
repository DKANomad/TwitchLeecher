using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TwitchLeecher.Core.Data
{
    public class TableBase
    {
        public Int64 ID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }
}
