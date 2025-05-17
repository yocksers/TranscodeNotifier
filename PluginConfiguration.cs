using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace TranscodeNotifier
{
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorDescription =>
            "<h2 style='color:red; font-weight:bold;'>⚠️ Not all clients support these messages!</h2><br/>";

        public override string EditorTitle => "Transcode Notifier Configuration";

        [DisplayName("Message Text")]
        [Description("The text to display when transcoding starts.")]
        public string MessageText { get; set; } = "This video is being transcoded.";

        [DisplayName("Max Notifications")]
        [Description("How many times to show the toast per session.")]
        public int MaxNotifications { get; set; } = 1;

        [DisplayName("Users to not be notified, separate names with comma, forexample Bob, Charlie, Michael.")]
        [Description("Comma-separated list of usernames that should NOT receive the message.")]
        public string ExcludedUserNames { get; set; } = string.Empty;

        [DisplayName("Initial Delay (seconds)")]
        [Description("How many seconds to wait after playback starts before showing the first toast.")]
        public int InitialDelaySeconds { get; set; } = 2;

        [DisplayName("Delay Between Messages (seconds)")]
        [Description("How many seconds to wait between showing each toast.")]
        public int DelayBetweenMessagesSeconds { get; set; } = 5;

        [DisplayName("Enable Confirmation Button")]
        [Description("If enabled, notification requires manual dismissal (no timeout).")]
        public bool EnableConfirmationButton { get; set; } = false;
    }
}