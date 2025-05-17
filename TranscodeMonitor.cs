using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;

namespace TranscodeNotifier
{
    public static class TranscodeMonitor
    {
        private static ISessionManager _sessionManager;
        private static bool _isRunning;

        private static readonly ConcurrentDictionary<string, bool> _hasNotified = new ConcurrentDictionary<string, bool>();

        public static void Start(ISessionManager sessionManager)
        {
            if (_isRunning)
                return;

            _sessionManager = sessionManager;
            _sessionManager.PlaybackStart += OnPlaybackStart;
            _hasNotified.Clear();

            _isRunning = true;
        }

        public static void Stop()
        {
            if (!_isRunning)
                return;

            _sessionManager.PlaybackStart -= OnPlaybackStart;
            _hasNotified.Clear();

            _isRunning = false;
        }

        private static async void OnPlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var config = Plugin.Instance.CurrentConfiguration;
                var session = e.Session;

                if (session == null || session.TranscodingInfo == null)
                    return; // Not transcoding

                var excludedUsers = (config.ExcludedUserNames ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrEmpty(u))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (excludedUsers.Contains(session.UserName))
                    return;

                if (_hasNotified.ContainsKey(session.Id))
                    return;

                _hasNotified[session.Id] = true;

                await Task.Delay(config.InitialDelaySeconds * 1000).ConfigureAwait(false);

                int maxNotifications = config.EnableConfirmationButton ? 1 : config.MaxNotifications;
                int delayBetweenMessages = config.EnableConfirmationButton ? 0 : config.DelayBetweenMessagesSeconds;

                for (int i = 0; i < maxNotifications; i++)
                {
                    var message = new MessageCommand
                    {
                        Header = "Transcode Warning",
                        Text = config.MessageText,
                        TimeoutMs = config.EnableConfirmationButton ? (int?)null : 5000
                    };

                    await _sessionManager.SendMessageCommand(
                        null,
                        session.Id,
                        message,
                        CancellationToken.None).ConfigureAwait(false);

                    if (i < maxNotifications - 1 && delayBetweenMessages > 0)
                    {
                        await Task.Delay(delayBetweenMessages * 1000).ConfigureAwait(false);
                    }
                }

                _hasNotified.TryRemove(session.Id, out _);
            }
            catch
            {
                // Optional: add logging here
            }
        }
    }
}