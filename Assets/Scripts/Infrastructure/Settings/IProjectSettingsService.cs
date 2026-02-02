namespace Infrastructure.Settings
{
	public interface IProjectSettingsService
	{
		float StartupLoadingMinSeconds { get; }
		int InterstitialStartLevel { get; }
		int BannerStartLevel { get; }
		int RewardedBoosterStartLevel { get; }
		string AdjustAppToken { get; }
		string AppMetricaAppId { get; }
		string MaxBannerAdUnitId { get; }
		string MaxInterstitialAdUnitId { get; }
		string MaxRewardedAdUnitId { get; }
	}
}
