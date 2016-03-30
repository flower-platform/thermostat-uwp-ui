using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace IotUiApp.Utils
{
    class UiUtils
    {
        public static void ShowNotification(string message)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageDialog msg = new MessageDialog(message);
                msg.ShowAsync();
            });
        }
    }
}
