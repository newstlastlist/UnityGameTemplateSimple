using System;
using UnityEngine;
using WS.Core.SDK.AppLovin;

namespace _Project.Scripts.Runtime.SDK.AppLovinMax
{
    public class AppLovinMaxActivator : MonoBehaviour
    {
        private void Awake()
        {
            MaxSdk.InitializeSdk();
            
            var serviceObject = new GameObject(nameof(AppLovinMaxAdService));
            serviceObject.AddComponent<AppLovinMaxAdService>();
            serviceObject.AddComponent<RewardedAdLimitService>();
            
            DontDestroyOnLoad(gameObject);
        }
    }
}