using SQLite;

namespace NanoTwitchLeafs.Objects
{
	public class Trigger
	{
		[PrimaryKey]
		public int ID { get; set; }

		public bool? IsActive { get; set; } = true;
		public string Type { get; set; }
		public string CMD { get; set; }
		public bool IsColor { get; set; } = false;
		public string Effect { get; set; }
		public byte R { get; set; } = 255;
		public byte G { get; set; } = 255;
		public byte B { get; set; } = 255;
		public string SoundFilePath { get; set; }
		public int? Volume { get; set; } = 50;
		public double Amount { get; set; } = 0;
		public double Duration { get; set; } = 10;
		public int Brightness { get; set; } = 50;
		public double Cooldown { get; set; } = 0;
		public bool VipOnly { get; set; } = false;
		public bool SubscriberOnly { get; set; } = false;
		public bool ModeratorOnly { get; set; } = false;
		public string ChannelPointsGuid { get; set; } = "{00000000-0000-0000-0000-000000000000}";
	}
}