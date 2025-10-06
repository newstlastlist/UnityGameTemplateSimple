using UnityEngine;
using AudioSettings = Domain.Audio.AudioSettings;

namespace Infrastructure.Audio
{
	public sealed class PlayerPrefsAudioSettingsService
	{
		private const string KeyMuted = "Audio_IsMuted";
		private const string KeyMaster = "Audio_Master";
		private const string KeyMusic = "Audio_Music";
		private const string KeySfx = "Audio_Sfx";

		public AudioSettings Load()
		{
			AudioSettings settings = new AudioSettings
			{
				IsMuted = PlayerPrefs.GetInt(KeyMuted, 0) == 1,
				MasterVolume = PlayerPrefs.GetFloat(KeyMaster, 1f),
				MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 1f),
				SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f)
			};
			return settings;
		}

		public void Save(AudioSettings settings)
		{
			PlayerPrefs.SetInt(KeyMuted, settings.IsMuted ? 1 : 0);
			PlayerPrefs.SetFloat(KeyMaster, settings.MasterVolume);
			PlayerPrefs.SetFloat(KeyMusic, settings.MusicVolume);
			PlayerPrefs.SetFloat(KeySfx, settings.SfxVolume);
			PlayerPrefs.Save();
		}
	}
}
