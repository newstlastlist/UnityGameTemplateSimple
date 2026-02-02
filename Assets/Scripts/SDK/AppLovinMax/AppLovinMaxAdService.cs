using System;
using AdjustSdk;
using Io.AppMetrica;
using Shared;
using Infrastructure.Settings;
using UnityEngine;

namespace WS.Core.SDK.AppLovin
{
    public enum InterType
    {
        none,
        start,
        mid
    }

    public enum RewardType
    {
        None,
        SecondChanse,
        AddBooster
    }

    public sealed class AppLovinMaxAdService : MonoBehaviour
    {
        private const string _ADJUST_REVENUE_SOURCE = "applovin_max_sdk";
        private const string _REWARDED_DEFAULT_PLACEMENT = "default";

        private string _rewardedAdUnitId;
        private string _interstitialAdUnitId;
        private string _bannerAdUnitId;

        private bool _loggedMissingSettingsService;
        private bool _loggedMissingBannerId;
        private bool _loggedMissingInterstitialId;
        private bool _loggedMissingRewardedId;

        // Кулдауны интеров:
        // - _generalInterstitialCooldownSeconds: 45 секунд для всех интеров, кроме mid-геймплейного (пункт 6).
        // - _midGameplayInterstitialCooldownSeconds: 240 секунд для mid-геймплейного интерstitial (пункт 6).
        // Изначально general == 0 (интер можно показать сразу), mid == 240 (mid-интер нельзя показать, пока не было
        // хотя бы одного другого интера и не прошло 240 секунд после последнего показа).
        private float _generalInterstitialCooldownSeconds;
        private float _midGameplayInterstitialCooldownSeconds = 240f;

        private InterType _lastInterType = InterType.none;
        private RewardType _lastRewardType = RewardType.None;


        public float InterCooldownSeconds => _generalInterstitialCooldownSeconds;

        // private AppMetricaAnalytics _appMetricaAnalytics;
        // private RateUsWindow _rateUsWindow;
        // private Purchases _purchases;


        private bool _isInterstitialReady;
        private bool _isLoadingInterstitial;

        private bool _isRewardedReady;
        private bool _isLoadingRewarded;
        private bool once;
        private Action _rewardedEarned;
        private Action _rewardedClosed;
        private Action _interstitialClosed;
        private string _rewardedPlacement = _REWARDED_DEFAULT_PLACEMENT;
        private bool _isBannerInitialized;
        private bool _isBannerVisible;

        public bool IsBannerVisible => _isBannerVisible;
        
        // private string _noADSProductID => _purchases.productIdentifiers.First(p => p.Contains("noads"));
        // public bool HasNoADS => IAPStoreListener.GetProductPurchased(_noADSProductID);

        private void Awake()
        {
            Services.Register(this);
            DontDestroyOnLoad(gameObject);

            ReadAdUnitIdsFromProjectConfigsOrLog();
            AttachCallbacks();
            InitializeBanner();
            TryApplyInitialBannerVisibilityFromProjectSettingsInternal();
            TryLoadInterstitial();
            TryLoadRewarded();
        }

        private void Start()
        {
            // _appMetricaAnalytics = Service<AppMetricaAnalytics>.Get();
            // _rateUsWindow = FB.Windows.GetWindow<RateUsWindow>();
            // _purchases = FindObjectOfType<Purchases>();
        }

        private void Update()
        {
            // Используем unscaledDeltaTime, чтобы таймеры тикали даже во время паузы/сплэшей.
            float deltaTime = Time.unscaledDeltaTime;

            if (_generalInterstitialCooldownSeconds > 0f)
            {
                _generalInterstitialCooldownSeconds = Mathf.Max(0f, _generalInterstitialCooldownSeconds - deltaTime);
            }

            if (_midGameplayInterstitialCooldownSeconds > 0f)
            {
                _midGameplayInterstitialCooldownSeconds = Mathf.Max(0f, _midGameplayInterstitialCooldownSeconds - deltaTime);
            }
        }

        // private void LateUpdate()
        // {
        //     if (!once && IAPStoreListener.IsInitialized())
        //     {
        //         once = true;
        //         UpdateNoADS(_noADSProductID);
        //     }
        // }

        // public void UpdateNoADS(string boughtItem)
        // {
        //     if (boughtItem != _noADSProductID) return;
        //     var isPurchased = IAPStoreListener.GetProductPurchased(_noADSProductID);
        //     if (isPurchased)
        //     {
        //         MaxSdk.StopBannerAutoRefresh(_BANNER_AD_UNIT_ID);
        //         MaxSdk.HideBanner(_BANNER_AD_UNIT_ID);
        //         MaxSdk.DestroyBanner(_BANNER_AD_UNIT_ID);
        //         GameObject.Find("BannerBottom(Clone)")?.gameObject.SetActive(false);
        //     }
        //     else
        //     {
        //         MaxSdk.CreateBanner(_BANNER_AD_UNIT_ID, MaxSdkBase.BannerPosition.BottomCenter);
        //         MaxSdk.StartBannerAutoRefresh(_BANNER_AD_UNIT_ID);
        //         MaxSdk.ShowBanner(_BANNER_AD_UNIT_ID);
        //     }
        // }

        private void AttachCallbacks()
        {
            //banner
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += HandleBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += HandleBannerLoadFailed;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += HandleBannerCollapsed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += HandleBannerRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdReviewCreativeIdGeneratedEvent += HandleBannerReviewCreativeIdGeneratedEvent;
            // MaxSdkCallbacks.Banner.OnAdExpandedEvent += HandleBannerExpanded;

            //rewarded
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += HandleRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += HandleRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += HandleRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += HandleRewardedHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += HandleRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += HandleRewardedReceivedReward;
            // MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += HandleRewardedRevenuePaid;

            //inter
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += HandleInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += HandleInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += HandleInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += HandleInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += HandleInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += HandleInterstitialRevenuePaid;
        }

        private void InitializeBanner()
        {
            if (_isBannerInitialized)
            {
                return;
            }

            if (!HasBannerIdOrLog())
            {
                return;
            }

            _isBannerInitialized = true;
            _isBannerVisible = false;

            // Баннер создаётся один раз. Показ/скрытие — через ShowBanner/HideBanner.
            MaxSdk.CreateBanner(_bannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.StartBannerAutoRefresh(_bannerAdUnitId);
            MaxSdk.HideBanner(_bannerAdUnitId);
        }

        public void ShowBanner()
        {
            if (!HasBannerIdOrLog())
            {
                return;
            }

            if (!_isBannerInitialized)
            {
                InitializeBanner();
            }

            _isBannerVisible = true;
            MaxSdk.ShowBanner(_bannerAdUnitId);
        }

        public void HideBanner()
        {
            if (!HasBannerIdOrLog())
            {
                return;
            }

            if (!_isBannerInitialized)
            {
                return;
            }

            _isBannerVisible = false;
            MaxSdk.HideBanner(_bannerAdUnitId);
        }

        private void TryApplyInitialBannerVisibilityFromProjectSettingsInternal()
        {
            if (!Services.TryGet<Infrastructure.Settings.IProjectSettingsService>(out var projectSettings) || projectSettings == null)
            {
                return;
            }

            if (!Services.TryGet<Infrastructure.IProgressService>(out var progressService) || progressService == null)
            {
                return;
            }

            int currentLevelNumber = progressService.LastCompletedLevelIndex + 2;
            bool shouldShowBanner = currentLevelNumber >= projectSettings.BannerStartLevel;
            if (shouldShowBanner)
            {
                ShowBanner();
            }
            else
            {
                HideBanner();
            }
        }

        // private void ReportAdEvent(string folder, string name, string adUnitId, MaxSdkBase.AdInfo adInfo = null, MaxSdkBase.ErrorInfo error = null,
        //     MaxSdkBase.Reward? reward = null, string creativeId = null)
        // {
        //     ELytics.New()
        //         .Add(
        //             "ad_unit", adUnitId,
        //             "placement", adInfo?.Placement,
        //             "network", adInfo?.NetworkName,
        //             "revenue", adInfo?.Revenue,
        //             "applovin_unit", adInfo?.AdUnitIdentifier,
        //             "creative", adInfo?.CreativeIdentifier,
        //             "error_code", error?.Code,
        //             "error", error?.Message,
        //             "reward_amount", reward?.Amount,
        //             "reward_label", reward?.Label,
        //             "creative_id", creativeId
        //         )
        //         .Path(folder)
        //         .Send(name);
        // }

        #region Interstitial

        public bool CanShowInterstitial_SDKCondition()
        {
            return HasInterstitialIdOrLog() && MaxSdk.IsInterstitialReady(_interstitialAdUnitId);
        }

        private bool CanShowInterstitial_GameCondition(bool useMidGameplayCooldown)
        {
            // if (HasNoADS) return false;

            // if (_rateUsWindow == null) _rateUsWindow = FB.Windows.GetWindow<RateUsWindow>();
            // if (_rateUsWindow != null && _rateUsWindow.IsActive) return false;

            if (useMidGameplayCooldown)
            {
                // Спец-таймер для mid-геймплейного интера (пункт 6).
                return _midGameplayInterstitialCooldownSeconds <= 0f;
            }

            // Общий таймер для всех остальных интеров.
            return _generalInterstitialCooldownSeconds <= 0f;
        }

        public void TryShowInter(InterType type, bool forceAds = false)
        {
            // Старый API: теперь маппим на общий интер с 45-секундным кулдауном.
            TryShowGeneralInterstitial(null, forceAds);
        }

        public bool TryShowGeneralInterstitial(Action onClosed, bool forceAds = false)
        {
            return TryShowInterstitialInternal(false, onClosed, forceAds);
        }

        public bool TryShowMidGameplayInterstitial(Action onClosed, bool forceAds = false)
        {
            return TryShowInterstitialInternal(true, onClosed, forceAds);
        }

        private bool TryShowInterstitialInternal(bool useMidGameplayCooldown, Action onClosed, bool forceAds)
        {
            if (forceAds)
            {
                if (!CanShowInterstitial_SDKCondition())
                {
                    return false;
                }

                _interstitialClosed = onClosed;
                ShowInterstitial();
                return true;
            }

            if (!CanShowInterstitial_GameCondition(useMidGameplayCooldown))
            {
                return false;
            }

            if (!CanShowInterstitial_SDKCondition())
            {
                return false;
            }

            _interstitialClosed = onClosed;
            ShowInterstitial();
            return true;
        }

        public void ShowInterstitial()
        {
            if (!HasInterstitialIdOrLog())
            {
                return;
            }

            if (!MaxSdk.IsInterstitialReady(_interstitialAdUnitId))
                return;
            MaxSdk.ShowInterstitial(_interstitialAdUnitId);
        }

        private void ResetInterstitialCooldowns()
        {
            // Любой показ интерstitial сбрасывает оба таймера:
            // - общий на 45 секунд;
            // - mid-геймплейный на 240 секунд.
            _generalInterstitialCooldownSeconds = 45f;
            _midGameplayInterstitialCooldownSeconds = 240f;
        }

        private void TryLoadInterstitial()
        {
            if (_isLoadingInterstitial) return;
            if (!HasInterstitialIdOrLog()) return;
            _isLoadingInterstitial = true;
            MaxSdk.LoadInterstitial(_interstitialAdUnitId);
        }

        private void HandleInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("loaded", "interstitial_ads", adUnitId, info);
            _isLoadingInterstitial = false;
            _isInterstitialReady = true;
        }

        private void HandleInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            // ReportAdEvent("load_failed", "interstitial_ads", adUnitId, error: error);
            _isLoadingInterstitial = false;
            _isInterstitialReady = false;
            ScheduleInterstitialReload();
        }

        private void HandleInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("displayed", "interstitial_ads", adUnitId, info);
            _isInterstitialReady = false;
        }

        private void HandleInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("hidden", "interstitial_ads", adUnitId, info);
            _isInterstitialReady = false;
            // Кулдаун интеров должен начинаться с момента закрытия рекламы, а не с момента показа.
            ResetInterstitialCooldowns();
            ScheduleInterstitialReload();
            InvokeInterstitialClosed();
        }

        private void HandleInterstitialRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // ReportAdEvent("revenue_paid", "interstitial_ads", adUnitId, adInfo);
            ReportAdjustAndAppmetricaRevenue(adInfo);
            Io.AppMetrica.AppMetrica.ReportEvent("Inter_Paid");
        }

        private void HandleInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("display_failed", "interstitial_ads", adUnitId, info, error);
            _isInterstitialReady = false;
            ScheduleInterstitialReload();
            InvokeInterstitialClosed();
            DebugLogger.LogWarning("[AppLovin][Interstitial] Display failed – scheduling reload.");
        }

        private void InvokeInterstitialClosed()
        {
            var closed = _interstitialClosed;
            _interstitialClosed = null;
            closed?.Invoke();
        }

        private void ScheduleInterstitialReload()
        {
            Invoke("TryLoadInterstitial", (float) 10.0f);
        }

        #endregion

        #region Rewarded

        public bool CanShowRewarded_SDKCondition() => HasRewardedIdOrLog() && MaxSdk.IsRewardedAdReady(_rewardedAdUnitId);

        private void TryLoadRewarded()
        {
            if (_isLoadingRewarded)
            {
                return;
            }

            if (!HasRewardedIdOrLog())
            {
                return;
            }

            _isLoadingRewarded = true;
            MaxSdk.LoadRewardedAd(_rewardedAdUnitId);
        }

        public bool TryShowRewarded(Action onRewardEarned, Action onClosed, RewardType type, string boosterKey = null)
        {
            // Тут аналитика, что пробовали показать

            var SDKCondition = CanShowRewarded_SDKCondition();
            // var el = ELytics.New().Add("CanShow", SDKCondition);
            // if (type == RewardType.addBooster && !string.IsNullOrEmpty(boosterKey))
            // {
            //     el.Add("type", ELytics.New()
            //         .Add("name", type.ToString())
            //         .Add("booster_key", boosterKey));
            // }
            // else
            // {
            //     el.Add("type", type.ToString());
            // }
            //
            // el.Send("Rewarded_TryShow");
            
            if (!SDKCondition)
            {
                TryLoadRewarded();
                return false;
            }

            _rewardedEarned = onRewardEarned;
            _rewardedClosed = onClosed;
            _lastRewardType = type;
            _isRewardedReady = false;

            if (!HasRewardedIdOrLog())
            {
                return false;
            }

            MaxSdk.ShowRewardedAd(_rewardedAdUnitId);
            return true;
        }

        private void HandleRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("loaded", "rewarded_ads", adUnitId, info);
            _isLoadingRewarded = false;
            _isRewardedReady = true;
        }

        private void HandleRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            // ReportAdEvent("load_failed", "rewarded_ads", adUnitId, error: error);
            _isLoadingRewarded = false;
            _isRewardedReady = false;
            ScheduleRewardedReload();
        }

        private void HandleRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("displayed", "rewarded_ads", adUnitId, info);
        }

        private void HandleRewardedHidden(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("hidden", "rewarded_ads", adUnitId, info);
            _isRewardedReady = false;
            ScheduleRewardedReload();
            InvokeRewardedClosed();
        }

        private void HandleRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("display_failed", "rewarded_ads", adUnitId, info, error);
            _isRewardedReady = false;
            ScheduleRewardedReload();
            InvokeRewardedClosed();
        }

        private void HandleRewardedReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("reward_received", "rewarded_ads", adUnitId, info, reward: reward);
            var earned = _rewardedEarned;
            _rewardedEarned = null;
            earned?.Invoke();
        }

        // private void HandleRewardedRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        // {
        //     // ReportAdEvent("revenue_paid", "rewarded_ads", adUnitId, adInfo);
        //     ReportAdjustAndAppmetricaRevenue(adInfo);
        //     AppMetrica.ReportEvent("Rewarded_Paid");
        // }

        private void ScheduleRewardedReload()
        {
            Invoke("TryLoadRewarded", (float) 10.0f);
        }

        private void InvokeRewardedClosed()
        {
            var closed = _rewardedClosed;
            _rewardedClosed = null;
            _rewardedEarned = null;
            _rewardedPlacement = _REWARDED_DEFAULT_PLACEMENT;
            closed?.Invoke();
        }

        #endregion

        #region Banner

        private void HandleBannerRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("revenue_paid", "banner_ads", adUnitId, info);
            ReportAdjustAndAppmetricaRevenue(info);
        }

        private void HandleBannerReviewCreativeIdGeneratedEvent(string adUnitId, string creativeId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("creative_id_generated", "banner_ads", adUnitId, info, creativeId: creativeId);
        }

        private void HandleBannerLoaded(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("loaded", "banner_ads", adUnitId, info);
        }

        private void HandleBannerCollapsed(string adUnitId, MaxSdkBase.AdInfo info)
        {
            // ReportAdEvent("collapsed", "banner_ads", adUnitId, info);
        }

        private void HandleBannerLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            // ReportAdEvent("load_failed", "banner_ads", adUnitId, error: error);
        }

        #endregion

        private void ReadAdUnitIdsFromProjectConfigsOrLog()
        {
            if (!Services.TryGet<IProjectSettingsService>(out var projectSettings) || projectSettings == null)
            {
                if (!_loggedMissingSettingsService)
                {
                    _loggedMissingSettingsService = true;
                    DebugLogger.LogError($"{nameof(AppLovinMaxAdService)}: {nameof(IProjectSettingsService)} not found. MAX ad unit ids must come from ProjectConfigs.");
                }

                return;
            }

            _bannerAdUnitId = projectSettings.MaxBannerAdUnitId;
            _interstitialAdUnitId = projectSettings.MaxInterstitialAdUnitId;
            _rewardedAdUnitId = projectSettings.MaxRewardedAdUnitId;
        }

        private bool HasBannerIdOrLog()
        {
            if (!string.IsNullOrWhiteSpace(_bannerAdUnitId))
            {
                return true;
            }

            // Lazy refresh (service might be registered later than this MonoBehaviour Awake).
            ReadAdUnitIdsFromProjectConfigsOrLog();
            if (!string.IsNullOrWhiteSpace(_bannerAdUnitId))
            {
                return true;
            }

            if (!_loggedMissingBannerId)
            {
                _loggedMissingBannerId = true;
                DebugLogger.LogError($"{nameof(AppLovinMaxAdService)}: MAX Banner Ad Unit Id is empty. Set it in ProjectConfigs.");
            }

            return false;
        }

        private bool HasInterstitialIdOrLog()
        {
            if (!string.IsNullOrWhiteSpace(_interstitialAdUnitId))
            {
                return true;
            }

            ReadAdUnitIdsFromProjectConfigsOrLog();
            if (!string.IsNullOrWhiteSpace(_interstitialAdUnitId))
            {
                return true;
            }

            if (!_loggedMissingInterstitialId)
            {
                _loggedMissingInterstitialId = true;
                DebugLogger.LogError($"{nameof(AppLovinMaxAdService)}: MAX Interstitial Ad Unit Id is empty. Set it in ProjectConfigs.");
            }

            return false;
        }

        private bool HasRewardedIdOrLog()
        {
            if (!string.IsNullOrWhiteSpace(_rewardedAdUnitId))
            {
                return true;
            }

            ReadAdUnitIdsFromProjectConfigsOrLog();
            if (!string.IsNullOrWhiteSpace(_rewardedAdUnitId))
            {
                return true;
            }

            if (!_loggedMissingRewardedId)
            {
                _loggedMissingRewardedId = true;
                DebugLogger.LogError($"{nameof(AppLovinMaxAdService)}: MAX Rewarded Ad Unit Id is empty. Set it in ProjectConfigs.");
            }

            return false;
        }

        private void ReportAdjustAndAppmetricaRevenue(MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null || adInfo.Revenue <= 0f)
            {
                return;
            }

            try
            {
                var revenue = new AdjustAdRevenue(_ADJUST_REVENUE_SOURCE);
                revenue.SetRevenue(adInfo.Revenue, "USD");
                revenue.AdRevenueNetwork = adInfo.NetworkName;
                revenue.AdRevenueUnit = adInfo.AdUnitIdentifier;
                revenue.AdRevenuePlacement = adInfo.Placement;
                AdjustSdk.Adjust.TrackAdRevenue(revenue);

                var appmetricaAdRevenue = new AdRevenue((long) (adInfo.Revenue * 1_000_000), "USD");
                appmetricaAdRevenue.AdNetwork = adInfo.NetworkName;
                appmetricaAdRevenue.AdPlacementName = adInfo.Placement;
                appmetricaAdRevenue.AdPlacementId = adInfo.CreativeIdentifier;
                appmetricaAdRevenue.AdUnitId = adInfo.AdUnitIdentifier;
                Io.AppMetrica.AppMetrica.ReportAdRevenue(appmetricaAdRevenue);
            }
            catch (Exception exception)
            {
                DebugLogger.LogWarning($"[AppLovin][Adjust][Appmetrica] Failed to report revenue: {exception}");
            }
        }
    }
}