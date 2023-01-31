namespace NanoTwitchLeafs.Objects
{
    public class Response
    {
    }

    public class Payload
    {
        public Response response { get; set; }
        public string status { get; set; }
        public int hr { get; set; }
        public string id { get; set; }
    }

    public class HeartRateMessage
    {
        public string @event { get; set; }
        public Payload payload { get; set; }
        public int @ref { get; set; }
        public string topic { get; set; }
    }
}