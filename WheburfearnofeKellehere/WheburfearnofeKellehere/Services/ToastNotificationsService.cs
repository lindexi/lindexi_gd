using Microsoft.Toolkit.Uwp.Notifications;

using WheburfearnofeKellehere.Contracts.Services;

using Windows.UI.Notifications;

namespace WheburfearnofeKellehere.Services
{
    public partial class ToastNotificationsService : IToastNotificationsService
    {
        public ToastNotificationsService()
        {
        }

        public void ShowToastNotification(ToastNotification toastNotification)
        {
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);
        }
    }
}
