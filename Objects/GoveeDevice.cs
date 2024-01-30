using System.Collections.Generic;
using System.Text.Json.Serialization;

public record struct GoveeDevice
{
    [JsonPropertyName("device")]
    public string DeviceId { get; set; }
    public string Model { get; set; }
    public string DeviceName { get; set; }
    public bool Controllable { get; set; }
    public bool Retrievable { get; set; }
    [JsonPropertyName("supportCmds")]
    public List<string> SupportedCommands { get; set; }
    public Properties Properties { get; set; }
}
public record ApiResponseBase
{
    public string? Message { get; set; }
    public int Code { get; set; }
}

public record GoveeResponse : ApiResponseBase
{
    public Data Data { get; set; }
}
public record struct Data
{
    public List<GoveeDevice> Devices { get; set; }
}