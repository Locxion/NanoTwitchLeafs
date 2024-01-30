using System.Text.Json.Serialization;
using System.Windows.Forms;
using NanoTwitchLeafs.Colors;

public class GoveeState
{
    [JsonPropertyName("device")]
    public string DeviceId { get; set; }

    public string Model { get; set; }
    
    public string Name { get; set; }
    
    [Newtonsoft.Json.JsonIgnore]
    public Properties Properties { get; set; }
};
public class Properties
{
    public bool Online { get; set; }
    public PowerState PowerState { get; set; }
    public int Brightness { get; set; }
    public int? ColorTemp { get; set; }
    public RgbColor Color { get; set; }
}