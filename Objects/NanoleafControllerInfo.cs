using System.Collections.Generic;

namespace NanoTwitchLeafs.Objects
{
    public class NanoleafControllerInfo
    {
        public string name { get; set; }
        public string serialNo { get; set; }
        public string manufacturer { get; set; }
        public string firmwareVersion { get; set; }
        public string hardwareVersion { get; set; }
        public string model { get; set; }
        public CloudHash cloudHash { get; set; }
        public Discovery discovery { get; set; }
        public Effects effects { get; set; }
        public FirmwareUpgrade firmwareUpgrade { get; set; }
        public PanelLayout panelLayout { get; set; }
        public Rhythm rhythm { get; set; }
        public Schedules schedules { get; set; }
        public State state { get; set; }

        public class CloudHash
        {
        }

        public class Discovery
        {
        }

        public class Effects
        {
            public List<string> effectsList { get; set; }
            public string select { get; set; }
        }

        public class FirmwareUpgrade
        {
        }

        public class GlobalOrientation
        {
            public int value { get; set; }
            public int max { get; set; }
            public int min { get; set; }
        }

        public class PositionData
        {
            public int panelId { get; set; }
            public int x { get; set; }
            public int y { get; set; }
            public int o { get; set; }
            public int shapeType { get; set; }
        }

        public class Layout
        {
            public int numPanels { get; set; }
            public int sideLength { get; set; }
            public List<PositionData> positionData { get; set; }
        }

        public class PanelLayout
        {
            public GlobalOrientation globalOrientation { get; set; }
            public Layout layout { get; set; }
        }

        public class Rhythm
        {
            public object auxAvailable { get; set; }
            public object firmwareVersion { get; set; }
            public object hardwareVersion { get; set; }
            public object rhythmActive { get; set; }
            public bool rhythmConnected { get; set; }
            public object rhythmId { get; set; }
            public object rhythmMode { get; set; }
            public object rhythmPos { get; set; }
        }

        public class Schedules
        {
        }

        public class Brightness
        {
            public int value { get; set; }
            public int max { get; set; }
            public int min { get; set; }
        }

        public class Ct
        {
            public int value { get; set; }
            public int max { get; set; }
            public int min { get; set; }
        }

        public class Hue
        {
            public int value { get; set; }
            public int max { get; set; }
            public int min { get; set; }
        }

        public class On
        {
            public bool value { get; set; }
        }

        public class Sat
        {
            public int value { get; set; }
            public int max { get; set; }
            public int min { get; set; }
        }

        public class State
        {
            public Brightness brightness { get; set; }
            public string colorMode { get; set; }
            public Ct ct { get; set; }
            public Hue hue { get; set; }
            public On on { get; set; }
            public Sat sat { get; set; }
        }
    }
}