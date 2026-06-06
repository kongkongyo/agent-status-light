using System;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Text;
using System.Web.Script.Serialization;

namespace WorkStatusLight
{
    internal sealed partial class StatusLightForm
    {

        private void ReadSettings()
        {
            if (!File.Exists(settingsPath))
            {
                currentSkin = "system";
                currentLightOrientation = LightOrientationHorizontalValue;
                return;
            }

            try
            {
                var serializer = new JavaScriptSerializer();
                SettingsPayload settings = serializer.Deserialize<SettingsPayload>(File.ReadAllText(settingsPath, Encoding.UTF8));
                currentSkin = NormalizeSkin(settings == null ? null : settings.skin);
                currentLightOrientation = NormalizeLightOrientation(settings == null ? null : settings.lightOrientation);
                barkServerUrl = NormalizeBarkServerUrl(settings == null ? null : settings.barkServerUrl);
                barkDeviceKey = settings == null ? String.Empty : (settings.barkDeviceKey ?? String.Empty).Trim();
                barkEnabled = settings != null && settings.barkEnabled && !String.IsNullOrWhiteSpace(barkDeviceKey);
                barkNotifyConfirm = settings != null && settings.barkNotifyConfirm;
                barkNotifyDone = settings == null || !settings.barkNotifyDone.HasValue || settings.barkNotifyDone.Value;
                pushPlusToken = settings == null ? String.Empty : (settings.pushPlusToken ?? String.Empty).Trim();
                pushPlusEnabled = settings != null && settings.pushPlusEnabled && !String.IsNullOrWhiteSpace(pushPlusToken);
                pushPlusNotifyConfirm = settings != null && settings.pushPlusNotifyConfirm;
                pushPlusNotifyDone = settings == null || !settings.pushPlusNotifyDone.HasValue || settings.pushPlusNotifyDone.Value;
                telegramBotToken = settings == null ? String.Empty : (settings.telegramBotToken ?? String.Empty).Trim();
                telegramChatId = settings == null ? String.Empty : (settings.telegramChatId ?? String.Empty).Trim();
                telegramProxyUrl = NormalizeTelegramProxyUrl(settings == null ? null : settings.telegramProxyUrl);
                telegramEnabled = settings != null && settings.telegramEnabled && !String.IsNullOrWhiteSpace(telegramBotToken) && !String.IsNullOrWhiteSpace(telegramChatId);
                telegramNotifyConfirm = settings != null && settings.telegramNotifyConfirm;
                telegramNotifyDone = settings == null || !settings.telegramNotifyDone.HasValue || settings.telegramNotifyDone.Value;
                notificationTitleTemplate = NormalizeNotificationTemplate(settings == null ? null : settings.notificationTitleTemplate, T.NotificationDefaultTitleTemplate);
                notificationBodyTemplate = NormalizeNotificationTemplate(settings == null ? null : settings.notificationBodyTemplate, T.NotificationDefaultBodyTemplate);
                soundEnabled = settings != null && settings.soundEnabled;
                string legacySoundFilePath = settings == null ? String.Empty : (settings.soundFilePath ?? String.Empty).Trim();
                confirmSoundFilePath = settings == null ? String.Empty : (settings.confirmSoundFilePath ?? legacySoundFilePath).Trim();
                doneSoundFilePath = settings == null ? String.Empty : (settings.doneSoundFilePath ?? legacySoundFilePath).Trim();
                confirmLightColor = ReadColor(settings == null ? null : settings.confirmColor, DefaultConfirmLightColor);
                workingLightColor = ReadColor(settings == null ? null : settings.workingColor, DefaultWorkingLightColor);
                doneLightColor = ReadColor(settings == null ? null : settings.doneColor, DefaultDoneLightColor);
                waitingLightColor = ReadOptionalColor(settings == null ? null : settings.waitingColor);
                hasSavedWindowLocation = settings != null && settings.windowX.HasValue && settings.windowY.HasValue;
                if (hasSavedWindowLocation)
                {
                    savedWindowLocation = new Point(settings.windowX.Value, settings.windowY.Value);
                }
            }
            catch (Exception ex)
            {
                currentSkin = "system";
                currentLightOrientation = LightOrientationHorizontalValue;
                barkServerUrl = "https://api.day.app";
                barkDeviceKey = String.Empty;
                barkEnabled = false;
                barkNotifyConfirm = false;
                barkNotifyDone = true;
                pushPlusToken = String.Empty;
                pushPlusEnabled = false;
                pushPlusNotifyConfirm = false;
                pushPlusNotifyDone = true;
                telegramBotToken = String.Empty;
                telegramChatId = String.Empty;
                telegramProxyUrl = String.Empty;
                telegramEnabled = false;
                telegramNotifyConfirm = false;
                telegramNotifyDone = true;
                notificationTitleTemplate = T.NotificationDefaultTitleTemplate;
                notificationBodyTemplate = T.NotificationDefaultBodyTemplate;
                soundEnabled = false;
                confirmSoundFilePath = String.Empty;
                doneSoundFilePath = String.Empty;
                ResetLightColorsToDefaults();
                hasSavedWindowLocation = false;
                Logger.Write("Settings read failed: " + ex.Message);
            }
        }


        private void WriteSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                var serializer = new JavaScriptSerializer();
                File.WriteAllText(settingsPath, serializer.Serialize(new SettingsPayload
                {
                    skin = currentSkin,
                    lightOrientation = currentLightOrientation,
                    barkEnabled = barkEnabled,
                    barkServerUrl = barkServerUrl,
                    barkDeviceKey = barkDeviceKey,
                    barkNotifyConfirm = barkNotifyConfirm,
                    barkNotifyDone = barkNotifyDone,
                    pushPlusEnabled = pushPlusEnabled,
                    pushPlusToken = pushPlusToken,
                    pushPlusNotifyConfirm = pushPlusNotifyConfirm,
                    pushPlusNotifyDone = pushPlusNotifyDone,
                    telegramEnabled = telegramEnabled,
                    telegramBotToken = telegramBotToken,
                    telegramChatId = telegramChatId,
                    telegramProxyUrl = telegramProxyUrl,
                    telegramNotifyConfirm = telegramNotifyConfirm,
                    telegramNotifyDone = telegramNotifyDone,
                    notificationTitleTemplate = notificationTitleTemplate,
                    notificationBodyTemplate = notificationBodyTemplate,
                    soundEnabled = soundEnabled,
                    soundFilePath = confirmSoundFilePath,
                    confirmSoundFilePath = confirmSoundFilePath,
                    doneSoundFilePath = doneSoundFilePath,
                    confirmColor = ColorToHex(confirmLightColor),
                    workingColor = ColorToHex(workingLightColor),
                    doneColor = ColorToHex(doneLightColor),
                    waitingColor = waitingLightColor.HasValue ? ColorToHex(waitingLightColor.Value) : null,
                    windowX = Location.X,
                    windowY = Location.Y
                }), Encoding.UTF8);
                Logger.Write("Settings saved skin=" + currentSkin + " lightOrientation=" + currentLightOrientation + " barkEnabled=" + barkEnabled + " barkKey=" + MaskSecret(barkDeviceKey) + " pushPlusEnabled=" + pushPlusEnabled + " pushPlusToken=" + MaskSecret(pushPlusToken) + " telegramEnabled=" + telegramEnabled + " telegramToken=" + MaskSecret(telegramBotToken) + " telegramProxy=" + (String.IsNullOrWhiteSpace(telegramProxyUrl) ? "none" : "set") + " soundEnabled=" + soundEnabled);
            }
            catch (Exception ex)
            {
                Logger.Write("Settings save failed: " + ex.Message);
            }
        }


        private void SaveWindowLocation()
        {
            savedWindowLocation = Location;
            hasSavedWindowLocation = true;
            WriteSettings();
        }


        private SkinPalette GetSkinPalette()
        {
            string effectiveSkin = String.Equals(currentSkin, "system", StringComparison.OrdinalIgnoreCase)
                ? (IsSystemLightTheme() ? "light" : "dark")
                : currentSkin;

            if (String.Equals(effectiveSkin, "light", StringComparison.OrdinalIgnoreCase))
            {
                return SkinPalette.Light();
            }
            if (String.Equals(effectiveSkin, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                return SkinPalette.Transparent();
            }

            return SkinPalette.Dark();
        }


        private static string NormalizeSkin(string skin)
        {
            skin = (skin ?? String.Empty).Trim().ToLowerInvariant();
            if (skin == "dark" || skin == "light" || skin == "transparent")
            {
                return skin;
            }

            return "system";
        }


        private static string NormalizeNotificationTemplate(string template, string fallback)
        {
            string normalized = (template ?? String.Empty).Trim();
            return String.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }


        private static string NormalizeLightOrientation(string orientation)
        {
            orientation = (orientation ?? String.Empty).Trim().ToLowerInvariant();
            if (orientation == LightOrientationVerticalValue)
            {
                return LightOrientationVerticalValue;
            }

            return LightOrientationHorizontalValue;
        }


        private static string NormalizeTelegramProxyUrl(string value)
        {
            string proxyUrl;
            return TryNormalizeTelegramProxyUrl(value, out proxyUrl) ? proxyUrl : String.Empty;
        }


        private static bool IsSystemLightTheme()
        {
            try
            {
                object value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                if (value is int)
                {
                    return (int)value != 0;
                }
            }
            catch
            {
            }

            return true;
        }
    }
}
