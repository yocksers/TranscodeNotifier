using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Timer = System.Timers.Timer;

namespace TranscodeNotifier
{
    public static class TranscodeMonitor
    {
        private static Timer _timer;
        private const int TimerIntervalMs = 5000;
        private const int DefaultMessageDurationMs = 5000;

        // Track notification counts per session per video playback
        private static readonly ConcurrentDictionary<string, int> _notificationCounts = new ConcurrentDictionary<string, int>();

        // Track last video ID per session to detect new playback starts
        private static readonly ConcurrentDictionary<string, string> _lastVideoIdPerSession = new ConcurrentDictionary<string, string>();

        public static void Start()
        {
            if (_timer != null)
                return;

            _timer = new Timer(TimerIntervalMs);
            _timer.Elapsed += async (_, __) => await CheckSessions().ConfigureAwait(false);
            _timer.AutoReset = true;
            _timer.Start();

            _notificationCounts.Clear();
            _lastVideoIdPerSession.Clear();
        }

        public static void Stop()
        {
            if (_timer == null)
                return;

            _timer.Stop();
            _timer.Dispose();
            _timer = null;

            _notificationCounts.Clear();
            _lastVideoIdPerSession.Clear();
        }

        private static async Task CheckSessions()
        {
            var pluginInstance = Plugin.Instance;
            if (pluginInstance == null)
                return;

            var sessionManager = pluginInstance.SessionManager;
            var config = pluginInstance.CurrentConfiguration;

            if (sessionManager == null || config == null)
                return;

            var allSessions = sessionManager.Sessions;
            if (allSessions == null || !allSessions.Any())
                return;

            // Cache excluded usernames once per check to reduce allocations
            var excludedUsers = (config.ExcludedUserNames ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter sessions early to reduce work
            var filteredSessions = allSessions.Where(s =>
                s.NowPlayingItem != null &&
                s.PlayState?.IsPaused == false &&
                !excludedUsers.Contains(s.UserName)
            ).ToList();

            if (!filteredSessions.Any())
                return;

            // Track currently active session IDs that are transcoding
            var activeTranscodingSessionIds = new ConcurrentBag<string>();

            foreach (var session in filteredSessions)
            {
                if (session.TranscodingInfo == null)
                    continue; // Not transcoding

                activeTranscodingSessionIds.Add(session.Id);

                string currentVideoId = session.NowPlayingItem?.Id ?? string.Empty;

                // Reset notification count if new video started or first time tracking this session
                if (!_lastVideoIdPerSession.TryGetValue(session.Id, out var lastVideoId) || lastVideoId != currentVideoId)
                {
                    _notificationCounts[session.Id] = 0;
                    _lastVideoIdPerSession[session.Id] = currentVideoId;
                }

                _notificationCounts.TryGetValue(session.Id, out int sentCount);
                if (sentCount >= config.MaxNotifications)
                    continue;

                var message = new MessageCommand
                {
                    Header = "Transcoding Detected",
                    Text = config.MessageText,
                    TimeoutMs = DefaultMessageDurationMs
                };

                try
                {
                    await sessionManager.SendMessageCommand(
                        session.Id,
                        session.Id,
                        message,
                        CancellationToken.None).ConfigureAwait(false);

                    _notificationCounts.AddOrUpdate(session.Id, 1, (_, old) => old + 1);
                }
                catch
                {
                    // Optionally log errors here
                }
            }

            // Remove sessions that no longer are transcoding or have ended playback
            var activeIdsSet = activeTranscodingSessionIds.ToHashSet();

            foreach (var key in _notificationCounts.Keys.Except(activeIdsSet).ToList())
            {
                _notificationCounts.TryRemove(key, out _);
                _lastVideoIdPerSession.TryRemove(key, out _);
            }
        }
    }
}