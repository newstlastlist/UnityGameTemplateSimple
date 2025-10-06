using System;
using System.Collections.Generic;
using Domain;
using Domain.Audio;
using UnityEngine;

namespace Shared
{
	[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Audio/Audio Database")]
	public sealed class AudioDatabase : ScriptableObject
	{
		[SerializeField] private List<Entry> _entries = new List<Entry>();
		private readonly Dictionary<AudioId, AudioClip> _clipsById = new Dictionary<AudioId, AudioClip>();

		public void Initialize()
		{
			_clipsById.Clear();
			foreach (Entry entry in _entries)
			{
				if (entry.Clip != null)
				{
					_clipsById[entry.Id] = entry.Clip;
				}
			}
		}

		public AudioClip GetClip(AudioId audioId)
		{
			return _clipsById.TryGetValue(audioId, out AudioClip clip) ? clip : null;
		}

		[Serializable]
		private struct Entry
		{
			public AudioId Id;
			public AudioClip Clip;
		}
	}
}
