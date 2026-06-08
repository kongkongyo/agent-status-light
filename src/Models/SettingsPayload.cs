namespace WorkStatusLight
{

    internal sealed class SettingsPayload
    {
        public string skin { get; set; }
        public string lightOrientation { get; set; }
        public bool? breathingLightEnabled { get; set; }
        public bool windowsNativeEnabled { get; set; }
        public bool windowsNativeNotifyConfirm { get; set; }
        public bool? windowsNativeNotifyDone { get; set; }
        public bool barkEnabled { get; set; }
        public string barkServerUrl { get; set; }
        public string barkDeviceKey { get; set; }
        public bool barkNotifyConfirm { get; set; }
        public bool? barkNotifyDone { get; set; }
        public bool pushPlusEnabled { get; set; }
        public string pushPlusToken { get; set; }
        public bool pushPlusNotifyConfirm { get; set; }
        public bool? pushPlusNotifyDone { get; set; }
        public bool telegramEnabled { get; set; }
        public string telegramBotToken { get; set; }
        public string telegramChatId { get; set; }
        public string telegramProxyUrl { get; set; }
        public bool telegramNotifyConfirm { get; set; }
        public bool? telegramNotifyDone { get; set; }
        public string notificationTitleTemplate { get; set; }
        public string notificationBodyTemplate { get; set; }
        public bool soundEnabled { get; set; }
        public string soundFilePath { get; set; }
        public string confirmSoundFilePath { get; set; }
        public string doneSoundFilePath { get; set; }
        public string confirmColor { get; set; }
        public string workingColor { get; set; }
        public string doneColor { get; set; }
        public string waitingColor { get; set; }
        public int? windowX { get; set; }
        public int? windowY { get; set; }
    }
}
