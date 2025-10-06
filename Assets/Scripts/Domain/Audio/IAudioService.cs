using System;

namespace Domain.Audio
{
	public interface IAudioService
	{
		bool IsMuted { get; }
		float MasterVolume { get; }
		float MusicVolume { get; }
		float SfxVolume { get; }

		void Initialize(AudioSettings settings, Func<AudioId, string> resolveClipPathHandler);

		void PlayMusic(AudioId audioId, bool loop);
		void StopMusic();

		void PlaySfx(AudioId audioId);

		void SetMuted(bool isMuted);
		void SetMasterVolume(float masterVolume);
		void SetMusicVolume(float musicVolume);
		void SetSfxVolume(float sfxVolume);
	}
}
