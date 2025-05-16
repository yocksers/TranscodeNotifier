# Transcode Notifier for Emby Server

Transcode Notifier is an Emby Server plugin that detects when a user's playback session is transcoding and sends a toast notification to the user’s client. It helps raise awareness of transcoding events, which can impact playback quality and server performance.

## Features

- Detects active transcoding sessions on your Emby Server.
- Sends customizable toast notifications to users when transcoding starts.
- Limits notifications to a configurable maximum number per video playback.
- Supports excluding specific users from receiving notifications.

## Installation

1. **Build or download**
   - Build the plugin DLL from the source code.
   - Or download a compiled release (if available).

2. **Copy files**
   - Copy the plugin folder (containing the `DLL`) into your Emby Server plugin directory:
     - **Windows**: `%AppData%\Emby-Server\plugins`
     - **Linux**: `/var/lib/emby/plugins` (or similar)

3. **Restart Emby Server**
   - Restart once to load the new plugin.

4. **Verify installation**
   - Open the Emby Dashboard > **Plugins** and confirm "Transcode Notifier" is listed.

## Configuration

Configure the plugin under **Dashboard > Plugins > Transcode Notifier**. All changes take effect immediately without restarting the server.

| Setting              | Description                                                       | Default                                  |
| -------------------- | ----------------------------------------------------------------- | ---------------------------------------- |
| **Message Text**     | Text shown in the toast notification when transcoding starts.     | "This video is being transcoded."        |
| **Max Notifications**| Maximum notifications per video playback.                         | `1`                                      |
| **Excluded User Names** | Comma-separated list of usernames to exclude from notifications. | *(empty)*                              |

---

**Do with it what you will, change it in any way you want but keep it open source!**

---

If you like the plugin and want to [Buy me a coffee](https://www.paypal.com/donate/?hosted_button_id=KEXBXYM4KFPE8)
