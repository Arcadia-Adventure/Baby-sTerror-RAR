using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif
namespace Ommy.Notifications
{
    public class NotificationManager : MonoBehaviour
    {
        // Set the notification title and message here
        public string title = "Daily Reminder";
        [TextArea(3, 6)]
        public string message = "Don't forget to take a break!";

        // Set the time for the daily notification here (in 24-hour format)
        public int hour = 12;
        public int minute = 0;
        public int second = 0;

        void Start()
        {
            // Request permission to send notifications
            RequestNotificationPermission();

            // Schedule the daily notification
            ScheduleNotification();
        }

        void RequestNotificationPermission()
        {
#if UNITY_IOS
        //// Request permission to send notifications on iOS
        //var options = AuthorizationOption.Alert | AuthorizationOption.Badge;
        //iOSNotificationCenter.RequestAuthorization(options);
#elif UNITY_ANDROID
            // Request permission to send notifications on Android
            //AndroidNotificationCenter.RequestPermission();
#endif
        }

        void ScheduleNotification()
        {
#if UNITY_IOS
        // Create the notification trigger
        var trigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = new TimeSpan(hour, minute, second),
            Repeats = true
        };

        // Create the notification request
        var request = new iOSNotification
        {
            Identifier = "daily_notification",
            Title = title,
            Body = message,
            Trigger = trigger
        };

        // Schedule the notification
      //  iOSNotificationCenter.AddNotification(request);
        iOSNotificationCenter.ScheduleNotification(request);
#elif UNITY_ANDROID

            // Create the notification channel
            var channel = new AndroidNotificationChannel
            {
                Id = "daily_notification_channel",
                Name = "Daily Notification",
                Importance = Importance.High,
                Description = "Daily reminder to take a break"
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);

            // Create the notification
            var notification = new AndroidNotification
            {
                Title = title,
                Text = message,
                SmallIcon = "notification_icon",
                LargeIcon = "app_icon",
                FireTime = DateTime.Now.AddHours(hour).AddMinutes(minute)
            };

            // Schedule the notification
            AndroidNotificationCenter.SendNotification(notification, channel.Id);
#endif
        }
    }
}