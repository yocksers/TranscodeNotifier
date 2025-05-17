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
        private const int DefaultMessageDurationMs = 5000;

        private static ISessionManager _sessionManager;
        private static PluginConfiguration _config;
        private static bool _isRunning;

        private static readonly ConcurrentDictionary<string, int> _notificationCounts = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, string> _lastVideoIdPerSession = new ConcurrentDictionary<string, string>();

        public static void Start(ISessionManager sessionManager, PluginConfiguration config)
        {
            if (_isRunning)
                return;

            _sessionManager = sessionManager;
            _config = config;

            // Subscribe to PlaybackProgress, not PlaybackStart
            _sessionManager.PlaybackProgress += OnPlaybackProgress;

            _notificationCounts.Clear();
            _lastVideoIdPerSession.Clear();

            _isRunning = true;
        }

        public static void Stop()
        {
            if (!_isRunning)
                return;

            _sessionManager.PlaybackProgress -= OnPlaybackProgress;

            _notificationCounts.Clear();
            _lastVideoIdPerSession.Clear();

            _isRunning = false;
        }

        private static async void OnPlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var session = e.Session;
                if (session == null || session.TranscodingInfo == null)
                    return; // Not transcoding

                // Only notify near the start of playback (e.g., position < 5 seconds)
                if (e.PlaybackPositionTicks > TimeSpan.FromSeconds(5).Ticks)
                    return;

                var excludedUsers = (_config.ExcludedUserNames ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .Where(u => !string.IsNullOrEmpty(u))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (excludedUsers.Contains(session.UserName))
                    return;

                string currentVideoId = session.NowPlayingItem?.Id ?? string.Empty;

                if (!_lastVideoIdPerSession.TryGetValue(session.Id, out var lastVideoId) || lastVideoId != currentVideoId)
                {
                    _notificationCounts[session.Id] = 0;
                    _lastVideoIdPerSession[session.Id] = currentVideoId;
                }

                _notificationCounts.TryGetValue(session.Id, out int sentCount);
                if (sentCount >= _config.MaxNotifications)
                    return;

                var message = new MessageCommand
                {
                    Header = "Transcoding Detected",
                    Text = _config.MessageText,
                    TimeoutMs = DefaultMessageDurationMs
                };

                await _sessionManager.SendMessageCommand(
                    session.Id,
                    session.Id,
                    message,
                    CancellationToken.None).ConfigureAwait(false);

                _notificationCounts.AddOrUpdate(session.Id, 1, (_, old) => old + 1);
            }
            catch
            {
                // Optionally log errors here or ignore
            }
        }
    }
}
