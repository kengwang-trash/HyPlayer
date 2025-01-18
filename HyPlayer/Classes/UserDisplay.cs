﻿using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace HyPlayer.Classes
{
    public class UserDisplay
    {
        private NCUser user;
        public UserDisplay(NCUser user)
        {
            this.user = user;
        }
        public string UserName => user.name;
        public string Signature => user.signature;
        private Uri avatarUri => Common.Setting.noImage ? new("ms-appx:///Assets/icon.png") : new(user.avatar, UriKind.RelativeOrAbsolute);
        public ImageSource AvatarSource => new BitmapImage(avatarUri);
    }
}
