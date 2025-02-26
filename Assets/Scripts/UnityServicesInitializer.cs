using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Samples.Multiplayer.UNET.Runtime
{
    ///<summary>
    ///Initializes all the Unity Services managers
    ///</summary>
    internal class UnityServicesInitializer : MonoBehaviour
    {
        public const string k_ServerID = "SERVER";
        public static UnityServicesInitializer Instance { get; private set; }

        public const string k_Environment = "production";
        public void Awake()
        {
            if (Instance && Instance != this)
            {
                return;
            }
            Instance = this;
        }

        async public Task Initialize(bool asDedicatedServer)
        {
            await Initialize(asDedicatedServer ? k_ServerID : string.Empty);
        }

        async public Task Initialize(string externalPlayerID)
        {
            string serviceProfileName = "MainProfile";
            //this allows testing Relay without builds, using ParrelSync, which speeds up iteration time.
#if UNITY_EDITOR && HAS_PARRELSYNC
            if (ParrelSync.ClonesManager.IsClone())
            {
                serviceProfileName = "CloneProfile";
            }
#endif
            if (!string.IsNullOrEmpty(externalPlayerID))
            {
                UnityServices.ExternalUserId = externalPlayerID;
            }

            bool signedIn = await UnityServiceAuthenticator.TrySignInAsync(k_Environment, serviceProfileName);
            if (!signedIn)
            {
                return;
            }
            if (externalPlayerID != k_ServerID)
            {
                InitializeClientOnlyServices();
            }
        }

        void InitializeClientOnlyServices()
        {
            //this si a good spot where to initialize client-only services here (I.E: matchmaker, if you're using it)
        }
    }
}