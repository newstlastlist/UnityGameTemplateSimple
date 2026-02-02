using Domain.Project;
using Infrastructure;

namespace Infrastructure.Settings
{
	public sealed class ProjectSettingsService : IProjectSettingsService
	{
		private const int AdsDisabledStartLevel = 99999;
		private readonly ProjectConfigs _configs;
		public float StartupLoadingMinSeconds => _configs != null ? _configs.StartupLoadingMinSeconds : 5f;
		public int InterstitialStartLevel => IsAdsDisabledInternal() ? AdsDisabledStartLevel : (_configs != null ? _configs.InterstitialStartLevel : 1);
		public int BannerStartLevel => IsAdsDisabledInternal() ? AdsDisabledStartLevel : (_configs != null ? _configs.BannerStartLevel : 1);
		public int RewardedBoosterStartLevel => IsAdsDisabledInternal() ? AdsDisabledStartLevel : (_configs != null ? _configs.RewardedBoosterStartLevel : 1);
		public string AdjustAppToken => _configs != null ? _configs.AdjustAppToken : string.Empty;
		public string AppMetricaAppId => _configs != null ? _configs.AppMetricaAppId : string.Empty;
		public string MaxBannerAdUnitId => _configs != null ? _configs.MaxBannerAdUnitId : string.Empty;
		public string MaxInterstitialAdUnitId => _configs != null ? _configs.MaxInterstitialAdUnitId : string.Empty;
		public string MaxRewardedAdUnitId => _configs != null ? _configs.MaxRewardedAdUnitId : string.Empty;

		public ProjectSettingsService(ProjectConfigs configs)
		{
			_configs = configs;
		}

		private static bool IsAdsDisabledInternal()
		{
			return PlayerPrefsProgressService.IsAdsDisabled();
		}
	}
}
