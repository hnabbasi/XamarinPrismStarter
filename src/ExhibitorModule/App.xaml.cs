﻿using System;
using System.Threading.Tasks;
using ExhibitorModule.Services;
using ExhibitorModule.Views;
using Prism;
using Prism.Ioc;
using Prism.Plugin.Popups;
using Acr.UserDialogs;
using Unity;
using Prism.Unity;
using ExhibitorModule.Helpers;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Distribute;
using Prism.Logging;
using Xamarin.Forms;
using DebugLogger = ExhibitorModule.Services.DebugLogger;
using ExhibitorModule.Services.Abstractions;
//using Plugin.DeviceInfo;
//using Plugin.DeviceInfo.Abstractions;
using Plugin.Permissions.Abstractions;
using Plugin.Permissions;
using ExhibitorModule.Common;
using ExhibitorModule.Data.Abstractions;
using ExhibitorModule.Data;
using Xamarin.Essentials;
using Prism.Events;
using ExhibitorModule.Models;
using System.Reflection;
using Prism.Navigation;
using Plugin.FirebasePushNotification.Abstractions;
using Plugin.FirebasePushNotification;
using ExhibitorModule.ViewModels;
using System.Collections.Generic;

namespace ExhibitorModule
{
    public partial class App : PrismApplication, IDisposable
    {
        /* 
         * NOTE: 
         * The Xamarin Forms XAML Previewer in Visual Studio uses System.Activator.CreateInstance.
         * This imposes a limitation in which the App class must have a default constructor. 
         * App(IPlatformInitializer initializer = null) cannot be handled by the Activator.
         */
        public App() : this(null) {}

        public App(IPlatformInitializer initializer) : base(initializer)
        {
            // https://docs.microsoft.com/en-us/mobile-center/sdk/distribute/xamarin
            Distribute.ReleaseAvailable = OnReleaseAvailable;

            // https://docs.microsoft.com/en-us/mobile-center/sdk/push/xamarin-forms
            var fb = Container.Resolve<IFirebasePushNotification>();
            fb.OnNotificationReceived += OnPushNotificationReceived;
            fb.OnTokenRefresh += OnTokenRefreshed;
            fb.OnNotificationOpened += Handle_OnNotificationOpened;
            
            // NOTE: Make sure to build MyApp project to generate Secrets.cs in obj folder.
            // Handle when your app starts
            var appId = Container.Resolve<IEssentialsService>().DevicePlatform == DevicePlatform.Android ?
                $"android={Secrets.AppCenter_Android_Secret};" :
                $"ios={Secrets.AppCenter_iOS_Secret};";

            AppCenter.Start(appId, typeof(Analytics), typeof(Crashes), typeof(Distribute));
            SetDefault();
        }

        private void SetDefault()
        {
            var cache = Container.Resolve<ICacheService>();

            if (string.IsNullOrWhiteSpace(cache.Device?.GetObject<string>(CacheKeys.SomeKey)))
                cache?.Device?.AddOrUpdateValue(CacheKeys.SomeKey, Secrets.SomeSecret);
        }

        protected override async void OnInitialized()
        {
            InitializeComponent();
            LogUnobservedTaskExceptions();
            SubsribeEvents();
            await NavigationService.NavigateAsync($"{nameof(RootNavigationPage)}/{nameof(MainPage)}");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterPopupNavigationService();

            containerRegistry.RegisterInstance(CreateLogger());
            containerRegistry.RegisterInstance<IUserDialogs>(UserDialogs.Instance);
            containerRegistry.RegisterInstance<IPermissions>(CrossPermissions.Current);
            containerRegistry.RegisterInstance<IFirebasePushNotification>(CrossFirebasePushNotification.Current);

            containerRegistry.RegisterForNavigation<RootNavigationPage, RootNavigationPageViewModel>();
            containerRegistry.RegisterForNavigation<AboutPage, AboutPageViewModel>();
            containerRegistry.RegisterForNavigation<SettingsPage, SettingsPageViewModel>();
            containerRegistry.RegisterForNavigation<MainPage, MainPageViewModel>();
            containerRegistry.RegisterForNavigation<CreditsPage>();

            containerRegistry.Register<IBase, Base>();
            containerRegistry.Register<IApiService, ApiService>();
            containerRegistry.Register<IEssentialsService, EssentialsService>();
            containerRegistry.Register<INetworkService, NetworkService>();
            containerRegistry.Register<IDatabase, Database>();
            containerRegistry.Register<ICacheRepository, CacheRepository>();
            containerRegistry.Register<ILoggerFacade, DebugLogger>();

            containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
            containerRegistry.RegisterSingleton<IDeviceCache, DeviceCache>();
            containerRegistry.RegisterSingleton<ICacheService, CacheService>();
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            if (await Analytics.IsEnabledAsync())
            {
                System.Diagnostics.Debug.WriteLine("Analytics is enabled");
                //FFImageLoading.ImageService.Instance.Config.Logger = (IMiniLogger)Container.Resolve<ILoggerFacade>();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Analytics is disabled");
            }
        }

        private ILoggerFacade CreateLogger()
        {
            switch (Xamarin.Forms.Device.RuntimePlatform)
            {
                case "Android":
                    if (!string.IsNullOrWhiteSpace(Helpers.Secrets.AppCenter_Android_Secret))
                        return CreateAppCenterLogger();
                    break;
                case "iOS":
                    if (!string.IsNullOrWhiteSpace(Helpers.Secrets.AppCenter_iOS_Secret))
                        return CreateAppCenterLogger();
                    break;
            }
            return new DebugLogger();
        }

        private MCAnalyticsLogger CreateAppCenterLogger()
        {
            var logger = new MCAnalyticsLogger();
            //FFImageLoading.ImageService.Instance.Config.Logger = (IMiniLogger)logger;
            return logger;
        }

        private void LogUnobservedTaskExceptions()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Container.Resolve<ILoggerFacade>().Log(e.Exception.Message, Category.Exception, Priority.High);
            };
        }

        void Handle_OnNotificationOpened(object source, FirebasePushNotificationResponseEventArgs e)
        {
            Console.WriteLine("Push tapped:");
            foreach (var data in e.Data)
            {
                Console.WriteLine($"{data.Key}:{data.Value}");
            }
        }


        private void OnTokenRefreshed(object sender, FirebasePushNotificationTokenEventArgs e)
        {
            Console.WriteLine($"FCM TOKEN refreshed: {e.Token}");
        }

        private void OnPushNotificationReceived(object sender, FirebasePushNotificationDataEventArgs e)
        {
            var summary = $"Push notification received:";
            if (e.Data != null)
            {
                summary += "\n\tCustom data:\n";
                foreach (var key in e.Data.Keys)
                {
                    summary += $"\t\t{key} : {e.Data[key]}\n";
                }
            }

            // Send the notification summary to debug output
            System.Diagnostics.Debug.WriteLine(summary);
            Container.Resolve<ILoggerFacade>().Log(summary, Category.Info, Priority.None);
        }

        private bool OnReleaseAvailable(ReleaseDetails releaseDetails)
        {
            // Look at releaseDetails public properties to get version information, release notes text or release notes URL
            string versionName = releaseDetails.ShortVersion;
            string versionCodeOrBuildNumber = releaseDetails.Version;
            string releaseNotes = releaseDetails.ReleaseNotes;
            Uri releaseNotesUrl = releaseDetails.ReleaseNotesUrl;

            // custom dialog
            var title = "Version " + versionName + " available!";
            Task answer;

            // On mandatory update, user cannot postpone
            if (releaseDetails.MandatoryUpdate)
            {
                answer = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install");
            }
            else
            {
                answer = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install", "Not Now");
            }
            answer.ContinueWith((task) =>
            {
                // If mandatory or if answer was positive
                if (releaseDetails.MandatoryUpdate || (task as Task<bool>).Result)
                {
                    // Notify SDK that user selected update
                    Distribute.NotifyUpdateAction(UpdateAction.Update);
                }
                else
                {
                    // Notify SDK that user selected postpone (for 1 day)
                    // Note that this method call is ignored by the SDK if the update is mandatory
                    Distribute.NotifyUpdateAction(UpdateAction.Postpone);
                }
            });

            // Return true if you are using your own dialog, false otherwise
            return true;
        }

        private void SubsribeEvents()
        {
            Xamarin.Essentials.Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        private void UnsubsribeEvents()
        {
            Xamarin.Essentials.Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        void Connectivity_ConnectivityChanged(object sender, Xamarin.Essentials.ConnectivityChangedEventArgs e)
        {
            if(Connectivity.NetworkAccess == NetworkAccess.None ||
                Connectivity.NetworkAccess == NetworkAccess.Unknown ||
                Connectivity.NetworkAccess == NetworkAccess.Local)
                UserDialogs.Instance.Toast(Strings.Resources.OfflineMessage);
        }

        public void Dispose()
        {
            UnsubsribeEvents();
        }
    }
}
