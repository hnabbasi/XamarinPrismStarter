using System;
using ExhibitorModule.Common;
using ExhibitorModule.Helpers;
using ExhibitorModule.Services.Abstractions;
using ExhibitorModule.Views;
using Prism.Commands;

namespace ExhibitorModule.ViewModels
{
    public class AboutPageViewModel : ViewModelBase
    {
        private readonly IEssentialsService _essentialsService;

        public AboutPageViewModel(IBase @base, IEssentialsService essentialsService)
            : base(@base)
        {
            Title = Strings.Resources.AboutPageTitle;

            InitializeCommand();
            _essentialsService = essentialsService;
        }

        private void InitializeCommand()
        {
            GoToWebsiteCommand = new DelegateCommand(GoToWebsiteExecute);
            ViewMapCommand = new DelegateCommand(ViewMapExecute);
            CreditsCommand = new DelegateCommand(CreditsExecute);
        }

        private void CreditsExecute()
        {
            NavigationService.NavigateAsync(nameof(CreditsPage));
        }

        private void ViewMapExecute()
        {
            DeviceService.OpenUri(new Uri(AppConstants.XDSMapLink));
        }

        private void GoToWebsiteExecute()
        {
            DeviceService.OpenUri(new Uri(AppConstants.XDSWebsiteLink));
        }

        public DelegateCommand GoToWebsiteCommand { get; private set; }
        public DelegateCommand CreditsCommand { get; private set; }
        public DelegateCommand ViewMapCommand { get; private set; }
        public string AppVersion => $"Version {_essentialsService.AppVersion}"; 
    }
}
