using UnityEngine;

namespace Domain.Project
{
	[CreateAssetMenu(fileName = "ProjectSettings", menuName = "Project/Project Settings")]
	public sealed class ProjectSettings : ScriptableObject
	{
		[Header("Loading Screen Settings")]
		[SerializeField] private float _startupLoadingMinSeconds = 5f;

		public float StartupLoadingMinSeconds => _startupLoadingMinSeconds;
	}
}
