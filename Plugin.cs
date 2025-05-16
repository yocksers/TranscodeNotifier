using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;

namespace TranscodeNotifier
{
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>, IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        public static Plugin Instance { get; private set; }
        public ISessionManager SessionManager => _sessionManager;

        // Constructor: initialize fields and set the singleton instance
        public Plugin(IApplicationHost applicationHost, ISessionManager sessionManager)
            : base(applicationHost)
        {
            Instance = this;
            _sessionManager = sessionManager;
        }

        // Expose the plugin configuration via GetOptions()
        public PluginConfiguration CurrentConfiguration => GetOptions();

        public override string Name => "Transcode Notifier";
        public override string Description => "Sends a toast message to users when transcoding is detected.";

        public void Run() => TranscodeMonitor.Start();
        public void Dispose() => TranscodeMonitor.Stop();
    }
}