using System;
using Domain.Audio;
using Shared;
using UnityEngine;
using AudioSettings = Domain.Audio.AudioSettings;

namespace Infrastructure.Audio
{
    /*
     * Как это работает:
        IAudioService (Domain): единая точка управления музыкой и эффектами.
        PlayMusic(AudioId id, bool loop), StopMusic()
        PlaySfx(AudioId id)
        SetMuted(bool), SetMasterVolume(float), SetMusicVolume(float), SetSfxVolume(float)
        Свойства IsMuted, MasterVolume, MusicVolume, SfxVolume
        AudioDatabase (Shared, ScriptableObject): карта AudioId -> AudioClip. Инициализируется один раз, хранит кэш. В GameApp добавлен сериализуемый референс.
        UnityAudioService (Infrastructure): 2 AudioSource (music/sfx) на [AudioService] GameObject в DontDestroyOnLoad. Управляет громкостями и воспроизведением.
        PlayerPrefsAudioSettingsService (Infrastructure): загрузка/сохранение настроек громкости/мута.
        GameApp (App): регистрирует IAudioService, грузит настройки из PlayerPrefs и сохраняет их при завершении.
    Что нужно сделать в редакторе:
        Создать ScriptableObject AudioDatabase в Project: Create → Audio → Audio Database.
        Заполнить список клипов и их AudioId.
        Присвоить созданный AudioDatabase в поле _audioDatabase у GameApp в сцене.
    Мини-FAQ:
        Включить музыку: Services.Get<IAudioService>().PlayMusic(AudioId.MainMenuMusic, true).
        Проиграть SFX: Services.Get<IAudioService>().PlaySfx(AudioId.ClickSfx).
        Заглушить звуки: Services.Get<IAudioService>().SetMuted(true).
        Изменить громкость музыки: Services.Get<IAudioService>().SetMusicVolume(0.5f).
        Завершил: домен, SO, инфраструктура, регистрация и валидация.
     */
	public sealed class UnityAudioService : IAudioService
	{
		private readonly AudioDatabase _audioDatabase;
		private readonly GameObject _audioRoot;
		private readonly AudioSource _musicSource;
		private readonly AudioSource _sfxSource;

		private AudioSettings _settings;

		public bool IsMuted => _settings != null && _settings.IsMuted;
		public float MasterVolume => _settings != null ? _settings.MasterVolume : 1f;
		public float MusicVolume => _settings != null ? _settings.MusicVolume : 1f;
		public float SfxVolume => _settings != null ? _settings.SfxVolume : 1f;

		public UnityAudioService(AudioDatabase audioDatabase)
		{
			_audioDatabase = audioDatabase;
			_audioDatabase.Initialize();

			_audioRoot = new GameObject("[AudioService]");
			UnityEngine.Object.DontDestroyOnLoad(_audioRoot);

			_musicSource = _audioRoot.AddComponent<AudioSource>();
			_sfxSource = _audioRoot.AddComponent<AudioSource>();

			_musicSource.playOnAwake = false;
			_musicSource.loop = true;
			_sfxSource.playOnAwake = false;
		}

		public void Initialize(AudioSettings settings, Func<AudioId, string> resolveClipPathHandler)
		{
			_settings = settings ?? new AudioSettings();
			ApplyVolumes();
		}

		public void PlayMusic(AudioId audioId, bool loop)
		{
			AudioClip clip = _audioDatabase.GetClip(audioId);
			if (clip == null)
			{
				return;
			}

			_musicSource.loop = loop;
			_musicSource.clip = clip;
			_musicSource.Play();
		}

		public void StopMusic()
		{
			_musicSource.Stop();
			_musicSource.clip = null;
		}

		public void PlaySfx(AudioId audioId)
		{
			AudioClip clip = _audioDatabase.GetClip(audioId);
			if (clip == null)
			{
				return;
			}
			_sfxSource.PlayOneShot(clip, IsMuted ? 0f : _settings.SfxVolume * _settings.MasterVolume);
		}

		public void SetMuted(bool isMuted)
		{
			_settings.IsMuted = isMuted;
			ApplyVolumes();
		}

		public void SetMasterVolume(float masterVolume)
		{
			_settings.MasterVolume = Mathf.Clamp01(masterVolume);
			ApplyVolumes();
		}

		public void SetMusicVolume(float musicVolume)
		{
			_settings.MusicVolume = Mathf.Clamp01(musicVolume);
			ApplyVolumes();
		}

		public void SetSfxVolume(float sfxVolume)
		{
			_settings.SfxVolume = Mathf.Clamp01(sfxVolume);
			ApplyVolumes();
		}

		private void ApplyVolumes()
		{
			float master = IsMuted ? 0f : _settings.MasterVolume;
			_musicSource.volume = master * _settings.MusicVolume;
			_sfxSource.volume = master * _settings.SfxVolume;
		}
	}
}
