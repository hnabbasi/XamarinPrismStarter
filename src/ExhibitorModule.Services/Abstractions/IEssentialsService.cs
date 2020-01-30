using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ExhibitorModule.Services.Abstractions
{
    public interface IEssentialsService
    {
        Task ShareUri(string uri, string title);
        Task<bool> OpenBrowser(string link);
        bool PreferenceExists(string key);
        object GetPreference(string key, object value = null);
        void SavePreference(string key, object value = null);
        bool IsNetworkAvailable();
        DevicePlatform DevicePlatform { get; }
        string AppVersion { get; }
    }
}
