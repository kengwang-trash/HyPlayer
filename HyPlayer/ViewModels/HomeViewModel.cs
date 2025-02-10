using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HyPlayer.Classes;
using HyPlayer.Contracts.ViewModels;
using HyPlayer.Controls;
using HyPlayer.NeteaseApi.ApiContracts;
using HyPlayer.Pages;
using HyPlayer.PlayCore.Abstraction.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using HyPlayer.HyPlayControl;
using System.Diagnostics;
using Windows.UI.Xaml.Input;

namespace HyPlayer.ViewModels;

public partial class HomeViewModel: ObservableRecipient, IViewModel
{
#nullable enable
    public int? ConnectedItemIndex { get; set; }
    public string? ConnectedElementName { get; set; }
    public double? ScrollValue { get; set; }

    [ObservableProperty] private ObservableCollection<NCSong>? _recommendedSongs;
    [ObservableProperty] private ObservableCollection<NCPlayList>? _playLists;
    [ObservableProperty] private ObservableCollection<NCPlayList>? _topLists;
#nullable restore

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private CancellationToken _cancellationToken;

    private bool disposedValue = false;

    public HomeViewModel()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        RecommendedSongs = new ();
        PlayLists = new ();
        TopLists = new ();
    }

    public async Task GetDataAsync()
    {
        if (Common.Logined)
            LoadLoginedContent();
        await LoadRanklist();
        HyPlayList.OnLoginDone += LoadLoginedContent;
    }

    private async Task LoadRanklist()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        _cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var json = await Common.NeteaseAPI.RequestAsync(NeteaseApis.ToplistApi, _cancellationToken);
            if (json.IsError)
            {
                Common.AddToTeachingTipLists("加载榜单出错", json.Error.Message);
                return;
            }
            foreach (var PlaylistItemJson in json.Value.List ?? [])
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var ncp = PlaylistItemJson.MapToNCPlayList();
                TopLists.Add(ncp);
                Debug.WriteLine($"name:{ncp.name}");
            }
        }
        catch (Exception ex)
        {
            if (ex.GetType() != typeof(TaskCanceledException) && ex.GetType() != typeof(OperationCanceledException))
                Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    public async void LoadLoginedContent()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        _ = Common.Invoke(() =>
        {
            _cancellationToken.ThrowIfCancellationRequested();
            //UnLoginedContent.Visibility = Visibility.Collapsed;
            //LoginedContent.Visibility = Visibility.Visible;
            //TbHelloUserName.Text = Common.LoginedUser?.name ?? string.Empty;
            //UserImageRect.ImageSource = Common.Setting.noImage
    //? null
    //: new BitmapImage(new Uri(Common.LoginedUser?.avatar, UriKind.RelativeOrAbsolute));

        });
        //我们直接Batch吧
        try
        {
            var ret = await Common.NeteaseAPI.RequestAsync(NeteaseApis.ToplistApi, _cancellationToken);
            if (ret.IsError)
            {
                Common.AddToTeachingTipLists("加载榜单出错", ret.Error.Message);
            }
            else
            {
                _ = Common.Invoke(() =>
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    TopLists.Clear();
                    foreach (var bditem in ret.Value?.List ?? [])
                        TopLists.Add(bditem.MapToNCPlayList());
                });
            }

            //推荐歌单加载部分 - 优先级稍微靠后下
            try
            {
                var ret1 = await Common.NeteaseAPI.RequestAsync(NeteaseApis.RecommendResourceApi, _cancellationToken);
                if (ret1.IsError)
                {
                    Common.AddToTeachingTipLists("加载推荐歌单出错", ret1.Error.Message);
                }
                else
                {
                    _ = Common.Invoke(() =>
                    {
                        _cancellationToken.ThrowIfCancellationRequested();
                        PlayLists.Clear();
                        foreach (var item in ret1.Value?.Recommends ?? [])
                            PlayLists.Add(item.MapToNCPlayList());
                    });
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(TaskCanceledException) && ex.GetType() != typeof(OperationCanceledException))
                    Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
            }
        }
        catch (Exception ex)
        {
            if (ex.GetType() != typeof(TaskCanceledException) && ex.GetType() != typeof(OperationCanceledException))
                Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    // Commands
    [RelayCommand]
    private void FMTapped()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        PersonalFM.InitPersonalFM();
    }

    [RelayCommand]
    private void dailyRcmTapped()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        Common.NavigatePage(typeof(SongListDetail), new NCPlayList
        {
            cover = "ms-appx:/Assets/icon.png",
            creater = new NCUser
            {
                avatar = "https://p1.music.126.net/KxePid7qTvt6V2iYVy-rYQ==/109951165050882728.jpg",
                id = "1",
                name = "网易云音乐",
                signature = "网易云音乐官方账号 "
            },
            plid = "-666",
            subscribed = false,
            name = "每日歌曲推荐",
            desc = "根据你的口味生成，每天6:00更新"
        });
    }
    [RelayCommand]
    private void LikedSongListTapped()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        Common.NavigatePage(typeof(SongListDetail), Common.MySongLists[0].plid);
    }

    [RelayCommand]
    private void HeartBeatTapped()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        _ = Api.EnterIntelligencePlay(_cancellationToken);
    }

    [RelayCommand]
    private void UserTapped()
    {
        if (disposedValue) throw new ObjectDisposedException(nameof(Home));
        Common.NavigatePage(typeof(Me), null, null);
    }

}
