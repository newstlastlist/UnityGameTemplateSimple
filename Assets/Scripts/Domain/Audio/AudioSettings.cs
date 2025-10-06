namespace Domain.Audio
{
	public sealed class AudioSettings
	{
		public bool IsMuted { get; set; }
		public float MasterVolume { get; set; } = 1f;
		public float MusicVolume { get; set; } = 1f;
		public float SfxVolume { get; set; } = 1f;
	}
}
