using CommunityToolkit.Mvvm.ComponentModel;
using HyPlayer.Classes;
using HyPlayer.Contracts.ViewModels;
using HyPlayer.Controls;
using HyPlayer.Pages;
using NeteaseCloudMusicApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyPlayer.ViewModels
{
    public partial class HomeViewModel : ObservableRecipient, IViewModel
    {
        [ObservableProperty] private string _greetings;
        [ObservableProperty] private string _greetingsText;
        [ObservableProperty] private NCUser _currentUser;
        [ObservableProperty] private bool _isLogined;
        [ObservableProperty] private List<NCPlayList> _rankLists;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken;
        private Task _rankListLoaderTask;

        private bool disposedValue = false;

        public HomeViewModel()
        {
            _cancellationToken = _cancellationTokenSource.Token;
        }

        public async Task<bool> GetDataAsync()
        {
            // Get greeting text.
            DateTime currentTime = DateTime.Now;
            int hour = currentTime.Hour;
            if (hour < 11 && hour >= 6)
            {
                Greetings = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingPrefix_Morning").ValueAsString;
                GreetingsText = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingSuffix_Morning").ValueAsString;
            }
            else if (hour >= 11 && hour < 13)
            {
                Greetings = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingPrefix_Noon").ValueAsString;
                GreetingsText = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingSuffix_Noon").ValueAsString;
            }
            else if (hour >= 13 && hour < 17)
            {
                Greetings = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingPrefix_Afternoon").ValueAsString;
                GreetingsText = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingSuffix_Noon").ValueAsString;
            }
            else if (hour >= 17 && hour < 23)
            {
                Greetings = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingPrefix_Night").ValueAsString;
                GreetingsText = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingSuffix_Night").ValueAsString;
            }
            else
            {
                Greetings = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingPrefix_DeepNight").ValueAsString;
                GreetingsText = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/HomePage_GreetingSuffix_DeepNight").ValueAsString;
            }

            // Get current user;
            CurrentUser = Common.LoginedUser;
            IsLogined = Common.Logined;

            // Load rank lists
            RankLists = new List<NCPlayList>();

            if (disposedValue) throw new ObjectDisposedException(nameof(Home));
            _cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var json = await Common.ncapi?.RequestAsync(CloudMusicApiProviders.Toplist);

                foreach (var PlaylistItemJson in json["list"].ToArray())
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var ncp = NCPlayList.CreateFromJson(PlaylistItemJson);
                    //RankList.Children.Add(new PlaylistItem(ncp));
                    RankLists.Add(ncp);
                }
                json.RemoveAll();
                return true;
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(TaskCanceledException) && ex.GetType() != typeof(OperationCanceledException))
                    Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
            }
            return true;
        }
    }
}
