using Newtonsoft.Json;

namespace NanoTwitchLeafs.Objects
{
    public class NanoLeafState
    {
        public State State { get; set; }
        public string Effect { get; set; }
    }

    public class State
    {
        [JsonProperty("brightness")]
        public Brightness brightness { get; set; }

        [JsonProperty("colorMode")]
        public string colorMode { get; set; }

        [JsonProperty("ct")]
        public Ct ct { get; set; }

        [JsonProperty("hue")]
        public Hue hue { get; set; }

        [JsonProperty("on")]
        public On on { get; set; }

        [JsonProperty("sat")]
        public Sat sat { get; set; }
    }

    public class Brightness
    {
        [JsonProperty("value")]
        public int value { get; set; }

        [JsonProperty("max")]
        public int max { get; set; }

        [JsonProperty("min")]
        public int min { get; set; }
    }

    public class Ct
    {
        [JsonProperty("value")]
        public int value { get; set; }

        [JsonProperty("max")]
        public int max { get; set; }

        [JsonProperty("min")]
        public int min { get; set; }
    }

    public class Hue
    {
        [JsonProperty("value")]
        public int value { get; set; }

        [JsonProperty("max")]
        public int max { get; set; }

        [JsonProperty("min")]
        public int min { get; set; }
    }

    public class On
    {
        [JsonProperty("value")]
        public bool value { get; set; }
    }

    public class Sat
    {
        [JsonProperty("value")]
        public int value { get; set; }

        [JsonProperty("max")]
        public int max { get; set; }

        [JsonProperty("min")]
        public int min { get; set; }
    }
}