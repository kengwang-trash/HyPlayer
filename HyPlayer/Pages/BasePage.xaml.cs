﻿#region

using HyPlayer.Classes;
using HyPlayer.Controls;
using HyPlayer.HyPlayControl;
using HyPlayer.NeteaseApi.ApiContracts;
using Microsoft.UI.Xaml.Controls;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationViewDisplayModeChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode;
using NavigationViewSelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;

#endregion


// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

/// <summary>
///     可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class BasePage : Page
{
    private string nowplid;
    private string nowqrkey;

    public BasePage()
    {
        InitializeComponent();
        Common.PageBase = this;
        Common.GlobalTip = TheTeachingTip;
        HyPlayList.OnTimerTicked += () => Common.RollTeachingTip();
        HyPlayList.OnTimerTicked += Common.ChangePlaybarVisibillity;
        if (HyPlayList.Player == null)
            HyPlayList.InitializeHyPlaylist();
        HyPlayList.OnPlayItemChange += OnChangePlayItem;
        HyPlayList.OnSongCoverChanged += HyPlayList_OnSongCoverChanged;
        if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop" && Common.Setting.EnableTitleBarImmerse)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(AppTitleBar);
        }
        else if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
        {
            var result = ApplicationViewScaling.TrySetDisableLayoutScaling(true);
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        }

        ApplicationView.TerminateAppOnFinalViewClose = false;
        Common.BaseFrame = BaseFrame;
        BaseFrame.IsNavigationStackEnabled = !Common.Setting.forceMemoryGarbage;
        Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
        // Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
    }

    private async Task HyPlayList_OnSongCoverChanged(int hashCode, IBuffer coverStream)
    {
        await RefreshNavItemCover(hashCode, coverStream);
    }

    /*
    private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
    {
        if(args.EventType== CoreAcceleratorKeyEventType.KeyDown)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))//判断ctrl是否按下
            {
                switch(args.VirtualKey)
                {
                    case VirtualKey.P:
                        {

                            if(HyPlayList.Player.PlaybackSession.PlaybackState==Windows.Media.Playback.MediaPlaybackState.Playing)
                                HyPlayList.Player.Pause();
                            else HyPlayList.Player.Play();
                            break;
                        }
                    case VirtualKey.Left:
                        {
                            HyPlayList.SongMovePrevious();
                            break;
                        }
                    case VirtualKey.Right:
                        {

                            HyPlayList.SongMoveNext();
                            break;
                        }
                    case VirtualKey.M:
                        {
                            _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                            Common.PageMain.ExpandedPlayer.Navigate(typeof(CompactPlayerPage));
                            break;
                        }
                }
            }
            else if(args.VirtualKey==VirtualKey.Space)
            {
                if (HyPlayList.Player.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                    HyPlayList.Player.Pause();
                else HyPlayList.Player.Play();
            }
        }
        args.Handled = true;
    }
    */
    private async void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
    {
        if (args.CurrentPoint.Properties.IsXButton1Pressed)
            if (Common.isExpanded)
                await Common.BarPlayBar.CollapseExpandedPlayer();
            else
                Common.NavigateBack();
    }

    private async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
    {
        if (args.VirtualKey == VirtualKey.GamepadB)
        {
            if (Common.isExpanded)
                await Common.BarPlayBar.CollapseExpandedPlayer();
            else
                Common.NavigateBack();
            args.Handled = true;
        }

        if (args.VirtualKey == VirtualKey.GamepadY)
            if (HyPlayList.IsPlaying)
                HyPlayList.Player.Pause();
            else if (!HyPlayList.IsPlaying) HyPlayList.Player.Play();

        if (args.VirtualKey == VirtualKey.Escape)
            if (Common.isExpanded)
                await Common.BarPlayBar.CollapseExpandedPlayer();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!Common.Setting.DisablePopUp)
        {
            var dialog = new ContentDialog();
            dialog.Title = "重要提示";
            dialog.Content = "本软件仅供学习交流使用，下载后请在 24 小时内删除。\r\n请勿使用此软件登录网易云音乐或进行违反网易云音乐用户协议的行为";
            dialog.CloseButtonText = "我已知晓";
            dialog.PrimaryButtonText = "退出软件";
            dialog.IsPrimaryButtonEnabled = true;
            dialog.CloseButtonStyle = (Style)App.Current.Resources["AccentButtonStyle"];
            dialog.PrimaryButtonClick += (_, _) => _ = ApplicationView.GetForCurrentView().TryConsolidateAsync();
            _ = dialog.ShowAsync();
        }

        // 不要阻塞页面加载
        _ = UpdateManager.PopupVersionCheck(true);
        // Fire and Forget
        _ = LoadLoginData();
        /*
        if (e.Parameter is string)
            LoginDone();
        */
    }

    private async Task LoadLoginData()
    {
        try
        {
            if (Common.Setting.LoadCookies())
            {
                try
                {
                    await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginStatusApi);
                    await LoginDone();
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                Common.NavigatePage(typeof(Welcome));
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            LastFMManager.InitializeLastFMManager();
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists("登录Last.FM登录失败", ex.Message);
        }
    }

    private async void ButtonLogin_OnClick(object sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(TextBoxAccount.Text) || string.IsNullOrWhiteSpace(TextBoxPassword.Password))
        {
            InfoBarLoginHint.IsOpen = true;
            InfoBarLoginHint.Message = "用户名或密码不能为空";
            return;
        }

        DialogLogin.IsPrimaryButtonEnabled = false;
        DialogLogin.PrimaryButtonText = "登录中......";
        try
        {
            var queries = new Dictionary<string, object>();
            var account = TextBoxAccount.Text;
            var isPhone = Regex.Match(account, "^[0-9]+$").Success;
            var contryCode = string.Empty;
            if (account.StartsWith('+'))
            {
                isPhone = true;
                // get the string between '+' and ' '
                contryCode = account.Substring(1, account.IndexOf(' ') - 1);
                account = account.Substring(account.IndexOf(' ') + 1);
            }
            if (isPhone)
            {

                var response = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginCellphoneApi,
                    new LoginCellphoneRequest() { Cellphone = account, CountryCode = string.IsNullOrEmpty(contryCode) ? null : contryCode, Password = TextBoxPassword.Password });
                if (response.IsError)
                {
                    InfoBarLoginHint.IsOpen = true;
                    InfoBarLoginHint.Title = "登录失败";
                    DialogLogin.PrimaryButtonText = "登录";
                    DialogLogin.IsPrimaryButtonEnabled = true;
                    InfoBarLoginHint.Severity = InfoBarSeverity.Warning;
                    InfoBarLoginHint.Message = "登录失败 " + response.Error.Message;
                }
                else
                {
                    await LoginDone();
                }
            }
            else
            {
                var response = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginEmailApi,
                    new LoginEmailRequest() { Email = account, Password = TextBoxPassword.Password });
                if (response.IsError)
                {
                    InfoBarLoginHint.IsOpen = true;
                    InfoBarLoginHint.Title = "登录失败";
                    DialogLogin.PrimaryButtonText = "登录";
                    DialogLogin.IsPrimaryButtonEnabled = true;
                    InfoBarLoginHint.Severity = InfoBarSeverity.Warning;
                    InfoBarLoginHint.Message = "登录失败 " + response.Error.Message;
                }
                else
                {
                    await LoginDone();
                }
            }
        }
        catch (Exception ex)
        {
            DialogLogin.IsPrimaryButtonEnabled = true;
            InfoBarLoginHint.IsOpen = true;
            InfoBarLoginHint.Severity = InfoBarSeverity.Error;
            InfoBarLoginHint.Message = "登录失败 " + ex;
        }
    }

    private void ButtonCloseLoginForm_Click(object sender, ContentDialogButtonClickEventArgs args)
    {
        DialogLogin.Hide();
        NavViewBack();
    }

    private void NavViewBack()
    {
        Common.NavigateBack();
    }

    public async Task<bool> LoginDone()
    {
        LoginStatusResponse LoginStatus;
        try
        {
            var result = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginStatusApi);
            if (result.IsError)
            {
                Common.AddToTeachingTipLists("登录失败", result.Error.Message);
                return false;
            }
            LoginStatus = result.Value;
        }
        catch (Exception e)
        {
            Common.AddToTeachingTipLists(e.Message, (e.InnerException ?? new Exception()).Message);
            return false;
        }

        if (LoginStatus.Account == null) return false;
        InfoBarLoginHint.IsOpen = true;
        InfoBarLoginHint.Title = "登录成功";
        //存储Cookie
        Common.Setting.SaveCookies();
        if (LoginStatus.Profile != null)
            Common.LoginedUser = LoginStatus.Profile.MapToNcUser();
        else
            Common.LoginedUser = new NCUser
            {
                avatar = "ms-appx:///Assets/icon.png",
                id = LoginStatus.Account.Id,
                name = LoginStatus.Account.UserName,
                signature = "此账号未进行手机号验证, 请使用网易云音乐客户端登录后再继续操作"
            };

        Common.Logined = true;
        NavItemLogin.Content = Common.LoginedUser.name;
        NavItemLogin.Icon = new BitmapIcon
        {
            UriSource = new Uri(Common.LoginedUser.avatar + "?param=" +
                                                    StaticSource.PICSIZE_NAVITEM_USERAVATAR),
            ShowAsMonochrome = false
        };
        InfoBarLoginHint.Severity = InfoBarSeverity.Success;
        InfoBarLoginHint.Message = "欢迎 " + Common.LoginedUser.name;
        DialogLogin.Hide();
        //加载我喜欢的歌
        _ = LoadMyLikelist();
        _ = LoadSongList();

        // 执行签到操作
        // DoDailySign();

        // 播放信息记录
        HyPlayList.OnMediaEnd += Scrobble;

        HyPlayList.LoginDoneCall();
        _ = ((App)Application.Current).InitializeJumpList();
        NavMain.SelectedItem = NavItemLogin;
        return true;
    }

    public async void Scrobble(HyPlayItem item)
    {
        // 播放数据记录
        if (item.ItemType != HyPlayItemType.Netease) return;
        try
        {
            await LastFMManager.ScrobbleAsync(item);
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists("记录上传至Last.FM时发生错误", ex.Message);
        }
    }

    private static async Task LoadMyLikelist()
    {
        try
        {
            var js = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LikelistApi, new LikelistRequest() { Uid = Common.LoginedUser.id });
            if (js.IsError)
            {
                Common.AddToTeachingTipLists("获取喜欢列表失败", js.Error.Message);
                return;
            }
            Common.LikedSongs = js.Value.TrackIds.ToList();
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    public async Task LoadSongList()
    {
        //加载用户歌单
        var nowitem = NavItemsMyList;
        try
        {
            var json = await Common.NeteaseAPI.RequestAsync(NeteaseApis.UserPlaylistApi,
                                                        new UserPlaylistRequest() { Uid = Common.LoginedUser.id });
            if (json.IsError)
            {
                Common.AddToTeachingTipLists("获取歌单列表失败", json.Error.Message);
                return;
            }

            NavItemsLikeList.MenuItems.Clear();
            NavItemsMyList.MenuItems.Clear();
            NavItemsLikeList.Visibility = Visibility.Visible;
            NavItemsAddPlaylist.Visibility = Visibility.Visible;
            NavItemsMyList.Visibility = Visibility.Visible;
            NavItemsMyLovedPlaylist.Visibility = Visibility.Visible;
            Common.MySongLists.Clear();
            var isliked = false;
            foreach (var jToken in json.Value.Playlists)
                if (jToken.Subscribed)
                {
                    var item = new NavigationViewItem
                    {
                        Content = jToken.Name,
                        Tag = "Playlist" + jToken.Id,
                        IsRightTapEnabled = true,
                        Icon = new FontIcon
                        {
                            Glyph = "\uE142"
                        }
                    };
                    item.RightTapped += (_, __) =>
                    {
                        nowplid = jToken.Id;
                        ItemPublicPlayList.Visibility = Visibility.Collapsed;
                        PlaylistFlyout.ShowAt((FrameworkElement)_);
                    };
                    NavItemsLikeList.MenuItems.Add(item);
                }
                else
                {
                    Common.MySongLists.Add(jToken.MapToNCPlayList());
                    if (!isliked)
                    {
                        isliked = true;
                        continue;
                    }

                    var item = new NavigationViewItem
                    {
                        Icon = new FontIcon
                        {
                            Glyph = jToken.Privacy == 0 ? "\uE142" : "\uE72E"
                        },
                        Content = jToken.Name,
                        Tag = "Playlist" + jToken.Id,
                        IsRightTapEnabled = true
                    };
                    if (jToken.Privacy == 0)
                        item.Icon.Foreground = new SolidColorBrush(Color.FromArgb(255, 211, 39, 100));

                    item.RightTapped += (_, __) =>
                    {
                        nowplid = jToken.Id.ToString();
                        ItemPublicPlayList.Visibility = jToken.Privacy == 0
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                        PlaylistFlyout.ShowAt((FrameworkElement)_);
                    };
                    NavItemsMyList.MenuItems.Add(item);
                }

        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    private async void NavMain_OnSelectionChanged(NavigationView sender,
                                                  NavigationViewSelectionChangedEventArgs args)
    {
        if (Common.NavigatingBack) return;
        var nowitem = sender.SelectedItem as NavigationViewItem;
        if (Common.NavigationHistory.Count > 1)
            NavMain.IsBackEnabled = true;
        if (nowitem.Tag is null) return;

        if (nowitem.Tag.ToString() == "PageMe" && !Common.Logined)
        {
            Common.NeteaseAPI.Option.Cookies.Clear();//清一遍Cookie防止出错
            await DialogLogin.ShowAsync();
            return;
        }

        if (nowitem.Tag.ToString() == "MusicCloud") Common.NavigatePage(typeof(MusicCloudPage));

        if (nowitem.Tag.ToString() == "DailyRcmd")
            Common.NavigatePage(typeof(SongListDetail), new NCPlayList
            {
                cover = "ms-appx:/Assets/icon.png",
                creater = new NCUser
                {
                    avatar =
                                                                              "https://p1.music.126.net/KxePid7qTvt6V2iYVy-rYQ==/109951165050882728.jpg",
                    id = "1",
                    name = "网易云音乐",
                    signature = "网易云音乐官方账号 "
                },
                plid = "-666",
                subscribed = false,
                name = "每日歌曲推荐",
                desc = "根据你的口味生成，每天6:00更新"
            });

        if (nowitem.Tag.ToString() == "SonglistMyLike")
        {
            Common.NavigatePage(typeof(SongListDetail), Common.MySongLists[0].plid,
                                new EntranceNavigationTransitionInfo());
            return;
        }

        if (nowitem.Tag.ToString().StartsWith("Playlist"))
            Common.NavigatePage(typeof(SongListDetail), nowitem.Tag.ToString().Substring(8),
                                new EntranceNavigationTransitionInfo());

        switch (nowitem.Tag.ToString())
        {
            case "PageMe":
                Common.NavigatePage(typeof(Me), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageSearch":
                Common.NavigatePage(typeof(Search), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageHome":
                Common.NavigatePage(typeof(Home), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageSettings":
                Common.NavigatePage(typeof(Settings), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageLocal":
                Common.NavigatePage(typeof(LocalMusicPage), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageHistory":
                Common.NavigatePage(typeof(History), null, new EntranceNavigationTransitionInfo());
                break;
            case "PageFavorite":
                Common.NavigatePage(typeof(PageFavorite), null, new EntranceNavigationTransitionInfo());
                break;
        }
    }

    // Invoked events of not-for-navigation items can be handled separately.
    // Meanwhile we set "SelectsOnInvoked" property of these items "False" to avoid the navigation pane indicator being set to them.
    private async void NavMain_ItemInvoked(NavigationView sender,
                                           NavigationViewItemInvokedEventArgs args)
    {
        var invokedItemTag = (args.InvokedItemContainer as NavigationViewItem)?.Tag?.ToString();
        if (invokedItemTag is null || invokedItemTag == string.Empty) return;
        switch (invokedItemTag)
        {
            case "SonglistCreate":
                {
                    if (Common.Setting.EnableSonglistCreate)
                        await new CreateSonglistDialog().ShowAsync();
                    else
                        Common.AddToTeachingTipLists("歌单创建功能被禁用", "由于网易云音乐风控升级, 默认禁用歌单创建功能, 如需启用请至\"设置-实验室\"启用歌单创建功能");
                    break;
                }
            case "PersonalFM":
                {
                    PersonalFM.InitPersonalFM();
                    break;
                }
            case "HeartBeat":
                _ = LoadHeartBeat();
                break;
        }
    }

    private async Task LoadHeartBeat()
    {
        await Api.EnterIntelligencePlay();
    }

    private void OnNavigateBack(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        try
        {
            NavViewBack();
        }
        catch (Exception)
        {
            //ignore
        }
    }

    private void TextBoxAccount_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter) TextBoxPassword.Focus(FocusState.Keyboard);
    }

    private void TextBoxPassword_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter) ButtonLogin_OnClick(null, null);
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as Pivot).SelectedIndex == 1)
            LoadQr(null, null);
        else
            InfoBarLoginHint.Title = "登录代表你同意相关条款";
    }

    private async void LoadQr(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
    {
        try
        {
            var key = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginQrCodeUnikeyApi, new LoginQrCodeUnikeyRequest());
            if (key.IsError)
            {
                Common.AddToTeachingTipLists("获取UniKey失败", key.Error.Message);
                return;
            }
            _ = ReFreshQr(key.Value.Unikey);
            nowqrkey = key.Value.Unikey;
            while (!Common.Logined && nowqrkey == key.Value.Unikey)
            {
                var res = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginQrCodeCheckApi,
                                                           new LoginQrCodeCheckRequest() { Unikey = key.Value.Unikey });
                if (res.Value.Code == 800)
                {
                    key = await Common.NeteaseAPI.RequestAsync(NeteaseApis.LoginQrCodeUnikeyApi, new LoginQrCodeUnikeyRequest());
                    if (key.IsError)
                    {
                        Common.AddToTeachingTipLists("获取UniKey失败", key.Error.Message);
                        return;
                    }
                    try
                    {
                        _ = ReFreshQr(key.Value.Unikey);
                    }
                    catch (Exception ex)
                    {
                        Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
                    }
                }
                else if (res.Value.Code == 801)
                {
                    if (!InfoBarLoginHint.IsOpen)
                    {
                        InfoBarLoginHint.IsOpen = true;
                    }

                    InfoBarLoginHint.Title = "请扫描上方二维码登录";
                }
                else if (res.Value.Code == 803)
                {
                    if (!InfoBarLoginHint.IsOpen)
                    {
                        InfoBarLoginHint.IsOpen = true;
                    }

                    InfoBarLoginHint.Title = "登录成功";
                    DialogLogin.PrimaryButtonText = "登录成功";
                    await LoginDone();
                    break;
                }
                else if (res.Value.Code == 802)
                {
                    if (!InfoBarLoginHint.IsOpen)
                    {
                        InfoBarLoginHint.IsOpen = true;
                    }

                    InfoBarLoginHint.Title = "请在手机上授权登录";
                }

                await Task.Delay(2000);
            }
        }
        catch
        {
            InfoBarLoginHint.Title = "请点击二维码刷新";
        }
    }

    private async Task ReFreshQr(string key)
    {
        var QrUri = new Uri("https://music.163.com/login?codekey=" + key);
        var img = new BitmapImage();

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(QrUri.ToString(), QRCodeGenerator.ECCLevel.M);
        var qrCode = new BitmapByteQRCode(qrData);
        var qrImage = qrCode.GetGraphic(20);
        using (var stream = new InMemoryRandomAccessStream())
        {
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(qrImage);
                await writer.StoreAsync();
            }

            await img.SetSourceAsync(stream);
            QrContainer.Source = img;
        }

        InfoBarLoginHint.Title = "请扫描上方二维码登录";
    }

    private void ThirdPartyLogin_Click(object sender, RoutedEventArgs e)
    {
        DialogLogin.Hide();
        BaseFrame.Navigate(typeof(ThirdPartyLogin), (sender as Button).Tag.ToString());
    }

    private void NavigationViewItem_Tapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
    {
        NavMain.SelectedItem = sender;
    }


    private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Common.NavigatePage(typeof(Search), sender.Text, new EntranceNavigationTransitionInfo());
    }

    private void SearchAutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender,
                                                         AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        sender.Text = (string)args.SelectedItem;
    }


    private void BtnScaleQrCode_Click(object sender, RoutedEventArgs e)
    {
        DialogLogin.Width = 550;
        DialogLogin.Height = Window.Current.Bounds.Height;
        QrContainer.Height = 500;
        QrContainer.Width = QrContainer.Height;
    }

    private void NavMain_DisplayModeChanged(NavigationView sender,
                                            NavigationViewDisplayModeChangedEventArgs args)
    {
        const int topIndent = 16;
        const int expandedIndent = 48;
        var minimalIndent = 104;
        if (NavMain.IsBackButtonVisible.Equals(NavigationViewBackButtonVisible
                                                   .Collapsed))
            minimalIndent = 48;

        var currMargin = AppTitleBar.Margin;
        if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            AppTitleBar.Margin = new Thickness(topIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
        else if (sender.DisplayMode == NavigationViewDisplayMode.Minimal)
            AppTitleBar.Margin = new Thickness(minimalIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
        else
            AppTitleBar.Margin = new Thickness(expandedIndent, currMargin.Top, currMargin.Right, currMargin.Bottom);
    }

    private async void ItemPublicPlayList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await Common.NeteaseAPI.RequestAsync(NeteaseApis.PlaylistPrivacyApi,
                                             new PlaylistPrivacyRequest() { Id = nowplid });
            if (result.IsError)
            {
                Common.AddToTeachingTipLists("公开歌单失败", result.Error.Message);
                return;
            }

            Common.AddToTeachingTipLists("成功公开歌单");
            _ = LoadSongList();
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists("公开歌单失败", ex.Message);
        }
    }

    private async void ItemDelPlayList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var json = await Common.NeteaseAPI.RequestAsync(NeteaseApis.PlaylistDeleteApi,
                                             new PlaylistDeleteRequest() { Id = nowplid });
            if (json.IsError)
            {
                Common.AddToTeachingTipLists("删除失败", json.Error.Message);
                return;
            }
            Common.AddToTeachingTipLists("成功删除");
            _ = LoadSongList();
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists("删除失败", ex.Message);
        }
    }


    private void TheTeachingTip_OnCloseButtonClick(TeachingTip sender, object args)
    {
        Common.TeachingTipList.Clear();
    }


    private void SearchAutoSuggestBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ((AutoSuggestBox)sender).ItemsSource = null;
    }

    private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        if (string.IsNullOrEmpty(sender.Text))
        {
            sender.ItemsSource = null;
            return;
        }

        try
        {
            var json = await Common.NeteaseAPI.RequestAsync(NeteaseApis.SearchSuggestionApi,
                                                        new SearchSuggestionRequest() { Keyword = sender.Text });
            if (json.IsError)
            {
                Common.AddToTeachingTipLists("获取推荐词失败", json.Error.Message);
                return;
            }
            sender.ItemsSource = json.Value.Result.AllMatch.Select(t => t.Keyword).ToList();
        }
        catch (Exception ex)
        {
            Common.AddToTeachingTipLists(ex.Message, (ex.InnerException ?? new Exception()).Message);
        }
    }

    private Visibility SetVisiblePreview(int updateSource)
    {
        return updateSource == 2 ? Visibility.Visible : Visibility.Collapsed; //Canary更新就设置预览显示
    }

    private void OnChangePlayItem(HyPlayItem item)
    {
        _ = Common.Invoke(() =>
        {
            if (item.PlayItem != null)
            {
                NavItemSongName.Text = item.PlayItem.Name;
                NavItemArtist.Text = item.PlayItem.ArtistString;
            }
        });
    }

    public async Task RefreshNavItemCover(int hashCode, IBuffer coverStream)
    {
        if (HyPlayList.CoverStream.Size == 0) return;
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(coverStream);
            stream.Seek(0);
            if (NavItemBlank.Opacity != 0 && !Common.isExpanded && !Common.Setting.noImage && stream.Size != 0)
            {
                try
                {
                    if (hashCode != HyPlayList.NowPlayingHashCode) return;
                    await NavItemImageSource.SetSourceAsync(stream);
                }
                catch
                {
                }
            }
        });
    }

    public async Task RefreshNavItemCover(double collapseTime, int hashCode, IRandomAccessStream coverStream)
    {
        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        {
            using var stream = coverStream.CloneStream();
            var time = TimeSpan.FromSeconds(collapseTime + 0.25);
            await Task.Delay(time);
            if (NavItemBlank.Opacity != 0 && !Common.isExpanded && !Common.Setting.noImage && stream.Size != 0)
            {
                try
                {
                    if (hashCode != HyPlayList.NowPlayingHashCode) return;
                    await NavItemImageSource.SetSourceAsync(stream);
                }
                catch
                {
                }
            }
        });
    }

    private async void BaseFrame_Navigated(object sender, NavigationEventArgs e)
    {
        await Task.Delay(1000);
        _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                try
                {
                    NavMain.SelectionChanged -= NavMain_OnSelectionChanged;
                    Bindings.Update();
                    NavMain.SelectionChanged += NavMain_OnSelectionChanged;
                }
                catch
                {
                    // ignored
                }
            }
        );
    }
}