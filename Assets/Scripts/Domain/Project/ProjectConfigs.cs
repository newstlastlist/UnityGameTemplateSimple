using UnityEngine;
using Sirenix.OdinInspector;
using Infrastructure;
using UnityEngine.Serialization;

namespace Domain.Project
{
	[CreateAssetMenu(fileName = "ProjectConfigs", menuName = "Project/Project Configs")]
	public sealed class ProjectConfigs : ScriptableObject
	{
		[BoxGroup("CommonProjectSettings")]
		[BoxGroup("CommonProjectSettings/LoadingScreenSettings")]
		[SerializeField] private float _startupLoadingMinSeconds = 5f;
        
		public float StartupLoadingMinSeconds => _startupLoadingMinSeconds;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Start Levels")]
		[LabelText("Interstitial Start Level")]
		[MinValue(1)]
		[SerializeField] private int _interstitialStartLevel = 1;

		public int InterstitialStartLevel => _interstitialStartLevel;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Start Levels")]
		[LabelText("Banner Start Level")]
		[MinValue(1)]
		[SerializeField] private int _bannerStartLevel = 1;

		public int BannerStartLevel => _bannerStartLevel;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Start Levels")]
		[LabelText("Rewarded Booster Start Level")]
		[MinValue(1)]
		[SerializeField] private int _rewardedBoosterStartLevel = 1;

		public int RewardedBoosterStartLevel => _rewardedBoosterStartLevel;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/Adjust")]
		[LabelText("App Token")]
		[SerializeField] private string _adjustAppToken;

		public string AdjustAppToken => _adjustAppToken;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/AppMetrica")]
		[LabelText("App Id")]
		[SerializeField] private string _appMetricaAppId;

		public string AppMetricaAppId => _appMetricaAppId;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Ad Unit Ids")]
		[PropertySpace(10)]
		[LabelText("Banner Ad Unit Id")]
		[SerializeField] private string _maxBannerAdUnitId;

		public string MaxBannerAdUnitId => _maxBannerAdUnitId;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Ad Unit Ids")]
		[LabelText("Interstitial Ad Unit Id")]
		[SerializeField] private string _maxInterstitialAdUnitId;

		public string MaxInterstitialAdUnitId => _maxInterstitialAdUnitId;

		[BoxGroup("SDK")]
		[BoxGroup("SDK/MAX Ads")]
		[BoxGroup("SDK/MAX Ads/Ad Unit Ids")]
		[LabelText("Rewarded Ad Unit Id")]
		[SerializeField] private string _maxRewardedAdUnitId;

		public string MaxRewardedAdUnitId => _maxRewardedAdUnitId;

		[FormerlySerializedAs("_currentLevel")]
        [BoxGroup("CommonProjectSettings")]
        [BoxGroup("CommonProjectSettings/Levels")]
		[MinValue(1)]
		[InlineButton(nameof(SetCurrentLevel), "SetCurrentLevel")]
		[SerializeField] private int _desiredLevel = 1;

		public int DesiredLevel => _desiredLevel;

		[BoxGroup("CommonProjectSettings")]
		[BoxGroup("CommonProjectSettings/Levels")]
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

		[BoxGroup("CommonProjectSettings")]
		[BoxGroup("CommonProjectSettings/WinStreak")]
		[MinValue(0)]
		[InlineButton(nameof(SetWinStreak), "SetWinStreak")]
		[SerializeField] private int _desiredWinStreak = 0;

		public int DesiredWinStreak => _desiredWinStreak;

		[BoxGroup("CommonProjectSettings")]
		[BoxGroup("CommonProjectSettings/WinStreak")]
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

		// Reserved for future per-project settings.
		[BoxGroup("AssotiationJam")]
		[ShowInInspector, ReadOnly, HideLabel]
		[PropertySpace]
		private string AssotiationJamSettings => string.Empty;
	}
}
