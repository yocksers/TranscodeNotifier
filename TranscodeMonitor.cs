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

        // Tracks which sessions are currently sending notifications
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

                // Prevent overlapping notifications for the same session
                if (_hasNotified.ContainsKey(session.Id))
                    return;

                _hasNotified[session.Id] = true;

                await Task.Delay(config.InitialDelaySeconds * 1000).ConfigureAwait(false);

                for (int i = 0; i < config.MaxNotifications; i++)
                {
                    var message = new MessageCommand
                    {
                        Header = "Transcoding Detected",
                        Text = config.MessageText,
                        TimeoutMs = 5000
                    };

                    await _sessionManager.SendMessageCommand(
                        session.Id,
                        session.Id,
                        message,
                        CancellationToken.None).ConfigureAwait(false);

                    if (i < config.MaxNotifications - 1)
                    {
                        await Task.Delay(config.DelayBetweenMessagesSeconds * 1000).ConfigureAwait(false);
                    }
                }

                // Allow future messages again after completion
                _hasNotified.TryRemove(session.Id, out _);
            }
            catch
            {
                // Optional: add logging here
            }
        }
    }
}
