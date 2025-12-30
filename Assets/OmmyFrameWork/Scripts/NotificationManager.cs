using System;
using System.Collections.Generic;
using UnityEngine;
using Ommy.Singleton;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace Ommy.Notifications
{
    [System.Serializable]
    public class NotificationData
    {
        public string title;
        [TextArea(2, 4)]
        public string message;
        public string emoji; // Optional emoji to make it more attractive
    }

    public class NotificationManager : Singleton<NotificationManager>
    {
        [Header("=== Notification Settings ===")]
        [Tooltip("Enable/Disable notification system")]
        public bool enableNotifications = true;

        [Header("=== Daily Reminder Time ===")]
        [Range(0, 23)]
        public int dailyHour = 10;
        [Range(0, 59)]
        public int dailyMinute = 0;

        [Header("=== Come Back Notifications ===")]
        [Tooltip("Hours after which to send come back notification")]
        public int comeBackAfterHours = 4;
        public int comeBackAfterDays1 = 1;
        public int comeBackAfterDays3 = 3;
        public int comeBackAfterDays7 = 7;

        [Header("=== Spooky Daily Messages ===")]
        public List<NotificationData> dailyMessages = new List<NotificationData>()
        {
            new NotificationData { title = "👁️ She's Watching...", message = "The Nanny never sleeps. Can you escape her terror tonight?", emoji = "👁️" },
            new NotificationData { title = "🍼 The Baby Awaits...", message = "Shh... the nursery is quiet. Too quiet. Time to escape!", emoji = "🍼" },
            new NotificationData { title = "🚪 Doors Are Unlocking...", message = "The house grows darker. Your escape window is opening...", emoji = "🚪" },
            new NotificationData { title = "💀 Survive Another Night", message = "The Nanny is hunting. Will you make it out alive?", emoji = "💀" },
            new NotificationData { title = "🕯️ Lights Are Flickering...", message = "Something stirs in the shadows. Face your fears!", emoji = "🕯️" }
        };

        [Header("=== Creepy Come Back Messages ===")]
        public List<NotificationData> comeBackMessages = new List<NotificationData>()
        {
            new NotificationData { title = "👻 We've Been Waiting...", message = "The Nanny hasn't forgotten you. She never forgets...", emoji = "👻" },
            new NotificationData { title = "🔪 She Knows You Left...", message = "Running away won't save you. Come back... if you dare.", emoji = "🔪" },
            new NotificationData { title = "😱 Did You Hear That?", message = "Footsteps in the dark... The terror continues without you!", emoji = "😱" },
            new NotificationData { title = "🖤 The Nightmare Misses You", message = "The baby is crying... only you can save it from HER.", emoji = "🖤" },
            new NotificationData { title = "⚰️ Unfinished Horror...", message = "You left the baby behind! The Nanny is getting closer to it...", emoji = "⚰️" }
        };

        [Header("=== Urgent Streak Messages ===")]
        public List<NotificationData> streakMessages = new List<NotificationData>()
        {
            new NotificationData { title = "🔥 Your Courage Fades...", message = "Don't let fear win! Keep your survival streak alive!", emoji = "🔥" },
            new NotificationData { title = "⏰ Time Is Running Out!", message = "The Nanny grows stronger each day you're gone. Return NOW!", emoji = "⏰" }
        };

        private const string LAST_PLAY_TIME_KEY = "LastPlayTime";
        private const string NOTIFICATION_SCHEDULED_KEY = "NotificationsScheduled";

        void Start()
        {
            if (!enableNotifications) return;

            // Cancel all previous notifications to reschedule fresh ones
            CancelAllNotifications();

            // Request permission
            RequestNotificationPermission();

            // Schedule all notification types
            ScheduleAllNotifications();

            // Save current play time
            SaveLastPlayTime();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App is going to background - schedule notifications
                SaveLastPlayTime();
                if (enableNotifications)
                {
                    CancelAllNotifications();
                    ScheduleAllNotifications();
                }
            }
            else
            {
                // App is coming to foreground - cancel notifications
                CancelAllNotifications();
                SaveLastPlayTime();
            }
        }

        void OnApplicationQuit()
        {
            SaveLastPlayTime();
            if (enableNotifications)
            {
                ScheduleAllNotifications();
            }
        }

        void RequestNotificationPermission()
        {
#if UNITY_IOS
            // Request permission to send notifications on iOS
            using var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true);
            while (!req.IsFinished)
            {
                // Wait for user response
            }
#elif UNITY_ANDROID
            // Android 13+ requires runtime permission
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
#endif
        }

        void ScheduleAllNotifications()
        {
#if UNITY_ANDROID
            RegisterNotificationChannels();
#endif
            // Schedule daily notification
            ScheduleDailyNotification();

            // Schedule come back notifications
            ScheduleComeBackNotifications();

            // Schedule streak reminder
            ScheduleStreakReminder();

            PlayerPrefs.SetInt(NOTIFICATION_SCHEDULED_KEY, 1);
            PlayerPrefs.Save();
        }

#if UNITY_ANDROID
        void RegisterNotificationChannels()
        {
            // Daily channel
            var dailyChannel = new AndroidNotificationChannel
            {
                Id = "daily_channel",
                Name = "Daily Reminders",
                Importance = Importance.High,
                Description = "Daily game reminders and rewards",
                EnableVibration = true,
                EnableLights = true,
                LockScreenVisibility = LockScreenVisibility.Public
            };
            AndroidNotificationCenter.RegisterNotificationChannel(dailyChannel);

            // Come back channel
            var comeBackChannel = new AndroidNotificationChannel
            {
                Id = "comeback_channel",
                Name = "We Miss You",
                Importance = Importance.High,
                Description = "Reminders to come back and play",
                EnableVibration = true,
                EnableLights = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(comeBackChannel);

            // Streak channel
            var streakChannel = new AndroidNotificationChannel
            {
                Id = "streak_channel",
                Name = "Streak Alerts",
                Importance = Importance.High,
                Description = "Keep your daily streak alive",
                EnableVibration = true,
                EnableLights = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(streakChannel);
        }
#endif
        void ScheduleDailyNotification()
        {
            var randomMessage = dailyMessages[UnityEngine.Random.Range(0, dailyMessages.Count)];
            DateTime fireTime = GetNextDailyFireTime();

#if UNITY_IOS
            var calendarTrigger = new iOSNotificationCalendarTrigger
            {
                Hour = dailyHour,
                Minute = dailyMinute,
                Repeats = true
            };

            var notification = new iOSNotification
            {
                Identifier = "daily_notification",
                Title = randomMessage.title,
                Body = randomMessage.message,
                Trigger = calendarTrigger,
                ShowInForeground = false,
                Badge = 1
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = randomMessage.title,
                Text = randomMessage.message,
                SmallIcon = "notification_icon",
                LargeIcon = "app_icon",
                FireTime = fireTime,
                RepeatInterval = TimeSpan.FromDays(1),
                Style = NotificationStyle.BigTextStyle,
                Color = new Color(1f, 0.5f, 0f) // Orange color
            };
            AndroidNotificationCenter.SendNotification(notification, "daily_channel");
#endif
        }

        void ScheduleComeBackNotifications()
        {
            // After 4 hours
            ScheduleSingleComeBackNotification(TimeSpan.FromHours(comeBackAfterHours), "comeback_4h");

            // After 1 day
            ScheduleSingleComeBackNotification(TimeSpan.FromDays(comeBackAfterDays1), "comeback_1d");

            // After 3 days
            ScheduleSingleComeBackNotification(TimeSpan.FromDays(comeBackAfterDays3), "comeback_3d");

            // After 7 days
            ScheduleSingleComeBackNotification(TimeSpan.FromDays(comeBackAfterDays7), "comeback_7d");
        }

        void ScheduleSingleComeBackNotification(TimeSpan delay, string identifier)
        {
            var randomMessage = comeBackMessages[UnityEngine.Random.Range(0, comeBackMessages.Count)];
            DateTime fireTime = DateTime.Now.Add(delay);

#if UNITY_IOS
            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = delay,
                Repeats = false
            };

            var notification = new iOSNotification
            {
                Identifier = identifier,
                Title = randomMessage.title,
                Body = randomMessage.message,
                Trigger = trigger,
                ShowInForeground = false,
                Badge = 1
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = randomMessage.title,
                Text = randomMessage.message,
                SmallIcon = "notification_icon",
                LargeIcon = "app_icon",
                FireTime = fireTime,
                Style = NotificationStyle.BigTextStyle,
                Color = new Color(0.9f, 0.3f, 0.3f) // Red color for urgency
            };
            AndroidNotificationCenter.SendNotification(notification, "comeback_channel");
#endif
        }

        void ScheduleStreakReminder()
        {
            var randomMessage = streakMessages[UnityEngine.Random.Range(0, streakMessages.Count)];

            // Schedule for 20 hours after last play (before 24h streak reset)
            DateTime fireTime = DateTime.Now.AddHours(20);

#if UNITY_IOS
            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = TimeSpan.FromHours(20),
                Repeats = false
            };

            var notification = new iOSNotification
            {
                Identifier = "streak_reminder",
                Title = randomMessage.title,
                Body = randomMessage.message,
                Trigger = trigger,
                ShowInForeground = false,
                Badge = 1
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = randomMessage.title,
                Text = randomMessage.message,
                SmallIcon = "notification_icon",
                LargeIcon = "app_icon",
                FireTime = fireTime,
                Style = NotificationStyle.BigTextStyle,
                Color = new Color(1f, 0.6f, 0f) // Orange for streak
            };
            AndroidNotificationCenter.SendNotification(notification, "streak_channel");
#endif
        }

        DateTime GetNextDailyFireTime()
        {
            DateTime now = DateTime.Now;
            DateTime scheduledTime = new DateTime(now.Year, now.Month, now.Day, dailyHour, dailyMinute, 0);

            // If the scheduled time has already passed today, schedule for tomorrow
            if (scheduledTime <= now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            return scheduledTime;
        }

        void SaveLastPlayTime()
        {
            PlayerPrefs.SetString(LAST_PLAY_TIME_KEY, DateTime.Now.ToString());
            PlayerPrefs.Save();
        }

        public void CancelAllNotifications()
        {
#if UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            iOSNotificationCenter.ApplicationBadge = 0;
#elif UNITY_ANDROID
            AndroidNotificationCenter.CancelAllNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
        }

        /// <summary>
        /// Schedule a custom notification with specific time delay
        /// </summary>
        public void ScheduleCustomNotification(string title, string message, TimeSpan delay, string channelId = "daily_channel")
        {
#if UNITY_IOS
            var trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = delay,
                Repeats = false
            };

            var notification = new iOSNotification
            {
                Identifier = "custom_" + DateTime.Now.Ticks,
                Title = title,
                Body = message,
                Trigger = trigger,
                ShowInForeground = false
            };
            iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
            var notification = new AndroidNotification
            {
                Title = title,
                Text = message,
                SmallIcon = "notification_icon",
                LargeIcon = "app_icon",
                FireTime = DateTime.Now.Add(delay),
                Style = NotificationStyle.BigTextStyle
            };
            AndroidNotificationCenter.SendNotification(notification, channelId);
#endif
        }

        /// <summary>
        /// Schedule a notification for level completion reminder
        /// </summary>
        public void ScheduleLevelCompleteReminder(int levelNumber)
        {
            string title = "🎉 Great Job!";
            string message = $"You completed Level {levelNumber}! Ready for the next challenge?";
            ScheduleCustomNotification(title, message, TimeSpan.FromHours(2));
        }

        /// <summary>
        /// Schedule a notification for achievement unlock
        /// </summary>
        public void ScheduleAchievementReminder(string achievementName)
        {
            string title = "🏆 Achievement Unlocked!";
            string message = $"You earned '{achievementName}'! Come see your rewards!";
            ScheduleCustomNotification(title, message, TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// Toggle notifications on/off
        /// </summary>
        public void SetNotificationsEnabled(bool enabled)
        {
            enableNotifications = enabled;
            PlayerPrefs.SetInt("NotificationsEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!enabled)
            {
                CancelAllNotifications();
            }
            else
            {
                ScheduleAllNotifications();
            }
        }

        /// <summary>
        /// Check if notifications are enabled
        /// </summary>
        public bool AreNotificationsEnabled()
        {
            return PlayerPrefs.GetInt("NotificationsEnabled", 1) == 1;
        }
    }
}