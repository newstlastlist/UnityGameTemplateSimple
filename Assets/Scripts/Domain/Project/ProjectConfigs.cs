using UnityEngine;
using Sirenix.OdinInspector;
using Infrastructure;
using UnityEngine.Serialization;

namespace Domain.Project
{
	[CreateAssetMenu(fileName = "ProjectConfigs", menuName = "Project/Project Configs")]
	public sealed class ProjectConfigs : ScriptableObject
	{
		[Header("Loading Screen Settings")]
		[SerializeField] private float _startupLoadingMinSeconds = 5f;
        
		public float StartupLoadingMinSeconds => _startupLoadingMinSeconds;

		[TitleGroup("Ads")]
		[MinValue(1)]
		[SerializeField] private int _interstitialStartLevel = 1;

		public int InterstitialStartLevel => _interstitialStartLevel;

		[TitleGroup("Ads")]
		[MinValue(1)]
		[SerializeField] private int _bannerStartLevel = 1;

		public int BannerStartLevel => _bannerStartLevel;

		[TitleGroup("Ads")]
		[MinValue(1)]
		[SerializeField] private int _rewardedBoosterStartLevel = 1;

		public int RewardedBoosterStartLevel => _rewardedBoosterStartLevel;

		[TitleGroup("Ads/Ids")]
		[LabelText("Adjust App Token")]
		[SerializeField] private string _adjustAppToken;

		public string AdjustAppToken => _adjustAppToken;

		[TitleGroup("Ads/Ids")]
		[LabelText("AppMetrica App Id")]
		[SerializeField] private string _appMetricaAppId;

		public string AppMetricaAppId => _appMetricaAppId;

		[FormerlySerializedAs("_currentLevel")]
        [TitleGroup("Levels")]
		[MinValue(1)]
		[InlineButton(nameof(SetCurrentLevel), "SetCurrentLevel")]
		[SerializeField] private int _desiredLevel = 1;

		public int DesiredLevel => _desiredLevel;

		[TitleGroup("Levels")]
		[ShowInInspector, ReadOnly, LabelText("Current Level (index+1)")]
		private int CurrentLevelHumanPreview
		{
			get
			{
				var progress = new PlayerPrefsProgressService();
				progress.Load();
				// Текущий «игровой» уровень = следующий после последнего пройденного (human: index + 2)
				int value = progress.LastCompletedLevelIndex + 2;
				return value < 1 ? 1 : value;
			}
		}

		[DisableInPlayMode]
		private void SetCurrentLevel()
		{
			#if UNITY_EDITOR
			var progress = new PlayerPrefsProgressService();
			int newLastCompleted = _desiredLevel - 2;
			if (newLastCompleted < -1)
			{
				newLastCompleted = -1;
			}
			progress.LastCompletedLevelIndex = newLastCompleted;
			progress.LoopLevelIndex = Mathf.Max(0, _desiredLevel - 1);
			progress.Save();
			PlayerPrefs.Save();
			#endif
		}

		[TitleGroup("Win Streak")]
		[MinValue(0)]
		[InlineButton(nameof(SetWinStreak), "SetWinStreak")]
		[SerializeField] private int _desiredWinStreak = 0;

		public int DesiredWinStreak => _desiredWinStreak;

		[TitleGroup("Win Streak")]
		[ShowInInspector, ReadOnly, LabelText("Current Win Streak")]
		private int CurrentWinStreak => new PlayerPrefsProgressService().GetWinStreak();

		[DisableInPlayMode]
		private void SetWinStreak()
		{
			#if UNITY_EDITOR
			var progress = new PlayerPrefsProgressService();
			progress.SetWinStreak(_desiredWinStreak);
			#endif
		}
	}
}
