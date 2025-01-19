namespace FluentSettingControls.Common;
internal static partial class ControlHelpers
{
    internal static bool IsXamlRootAvailable { get; } = Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "XamlRoot");
}
