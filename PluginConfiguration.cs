using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace TranscodeNotifier
{
    public class PluginConfiguration : EditableOptionsBase
    {
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
    }
}