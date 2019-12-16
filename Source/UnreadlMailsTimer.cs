using AE.Net.Mail;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace UnreadMails
{
    [PluginActionId("cc.xlnt.unreadmails")]
    public class UnreadlMailsTimer : PluginBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                return new PluginSettings();
            }

            [JsonProperty(PropertyName = "interval")]
            public int Interval { get; set; }

            [JsonProperty(PropertyName = "hostname")]
            public string Hostname { get; set; }

            [JsonProperty(PropertyName = "username")]
            public string Username { get; set; }

            [JsonProperty(PropertyName = "password")]
            public string Password { get; set; }

            [JsonProperty(PropertyName = "port")]
            public int Port { get; set; }

            [JsonProperty(PropertyName = "auth")]
            public int Auth { get; set; }

            [JsonProperty(PropertyName = "secure")]
            public bool Secure { get; set; }

            [JsonProperty(PropertyName = "skipCertVal")]
            public bool SkipCertVal { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "mailClient")]
            public string MailClient { get; set; }
        }

        private readonly PluginSettings Settings;
        private int TickWaitRemaining = 1;
        private int LastUnreadMailCount;
        private bool TitleToggle;

        public UnreadlMailsTimer(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(Settings));
            }
            else
            {
                Settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public async override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            // New in StreamDeck-Tools v2.0:
            Tools.AutoPopulateSettings(Settings, payload.Settings);

            // Return fixed filename back to the Property Inspector
            await Connection.SetSettingsAsync(JObject.FromObject(Settings)).ConfigureAwait(false);
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        public override void KeyPressed(KeyPayload payload)
        {
            try
            {
                string Filename = Settings.MailClient;

                if (!String.IsNullOrEmpty(Filename))
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = Filename.Replace(@"C:\fakepath\", String.Empty);
                        p.Start();
                    }
                }
            }
            catch
            {
            }
        }

        public override void KeyReleased(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Released");
        }

        public async override void OnTick()
        {
            if (--TickWaitRemaining < 0)
            {
                TickWaitRemaining = Settings.Interval;
                try
                {
                    using (ImapClient ic = new ImapClient(Settings.Hostname, Settings.Username, Settings.Password, (AuthMethods)Settings.Auth, Settings.Port, Settings.Secure, Settings.SkipCertVal))
                    {
                        ic.SelectMailbox("INBOX");
                        LastUnreadMailCount = ic.Search(SearchCondition.Unseen()).Length;

                        // Logger.Instance.LogMessage(TracingLevel.INFO, $"New mail count {ic.GetMessageCount()}");

                        /*
                        if (UnreadMailCount > 0 && !UnreadMails)
                        {
                            UnreadMails = true;

                            if (!String.IsNullOrEmpty(UnreadMailsImageString))
                            {
                                Logger.Instance.LogMessage(TracingLevel.INFO, "Setting new UnreadMailsImageString");
                                await Connection.SetImageAsync(UnreadMailsImageString, true);
                            }
                        }
                        else if (UnreadMails)
                        {
                            UnreadMails = false;

                            if (!String.IsNullOrEmpty(NoUnreadMailsImageString))
                            {
                                Logger.Instance.LogMessage(TracingLevel.INFO, "Setting new NoUnreadMailsImageString");
                                await Connection.SetImageAsync(NoUnreadMailsImageString, true);
                            }
                        }
                        */
                    }
                }
                catch (Exception Ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, Ex.ToString());
                }
            }

            string Title = $"{Settings.Username?.Remove(Settings.Username.IndexOf('@'))}\n{LastUnreadMailCount}";

            // Blink on unread mails
            if (LastUnreadMailCount > 0)
            {
                if (TitleToggle)
                {
                    TitleToggle = false;
                    Title += "\nNEW";
                }
                else
                {
                    TitleToggle = true;
                }
            }

            await Connection.SetTitleAsync(Title).ConfigureAwait(false);
        }
        /*
        private void LoadCustomImages()
        {
            UnreadMailsImageString = Tools.FileToBase64(Settings.UnreadMailsImage, true);
            NoUnreadMailsImageString = Tools.FileToBase64(Settings.NoUnreadMailsImage, true);
        }
        */
        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }
    }
}
