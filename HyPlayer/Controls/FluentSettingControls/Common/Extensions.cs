using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Composition;
using Windows.System;
using System;
using System.Numerics;

namespace FluentSettingControls;

public static partial class Extensions
{
    private static readonly Random rnd = new Random(new Guid().GetHashCode());
    internal static void BindCenterPoint(this Visual target)
    {
        var exp = target.Compositor.CreateExpressionAnimation(
            "Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0f)");
        target.StartAnimation("CenterPoint", exp);
    }

    internal static void BindSize(this Visual target, Visual source)
    {
        var exp = target.Compositor.CreateExpressionAnimation("host.Size");
        exp.SetReferenceParameter("host", source);
        target.StartAnimation("Size", exp);
    }

    internal static Vector2 ToVector2(this Vector3 value)
    {
        return new Vector2(value.X, value.Y);
    }
    internal static Vector3 NextVector3(this Random rnd, Vector3 value)
    {
        return new Vector3(rnd.Next(Convert.ToInt32(value.X)), rnd.Next(Convert.ToInt32(value.Y)), rnd.Next(Convert.ToInt32(value.Z)));
    }

    internal static double RandomNegative(this double value)
    {
        if (rnd.Next(0, 2) == 0) return -value;
        else return value;
    }
}
