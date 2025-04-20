using System;
using NanoTwitchLeafs.Enums;

namespace NanoTwitchLeafs.Objects
{
    public class AnalyticsPing
    {
        public int Id { get; set; }
        public Guid InstanceId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public PingType PingType { get; set; }
        public AppInformation AppInformation { get; set; }
    }
}