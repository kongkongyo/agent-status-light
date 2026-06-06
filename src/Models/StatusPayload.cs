using System;
using System.Globalization;

namespace WorkStatusLight
{

    internal sealed class StatusPayload
    {
        public string state { get; set; }
        public string label { get; set; }
        public string color { get; set; }
        public string message { get; set; }
        public string updatedAt { get; set; }
        public string source { get; set; }
        public int workingCount { get; set; }
        public int confirmCount { get; set; }
        public int doneCount { get; set; }

        public static StatusPayload For(string value, string message)
        {
            string state = Normalize(value);
            string label = T.IdleLabel;
            string color = "idle";
            string defaultMessage = T.CodexWaiting;

            if (state == "working")
            {
                label = T.Working;
                color = "yellow";
                defaultMessage = T.CodexWorking;
            }
            else if (state == "confirm")
            {
                label = T.NeedsConfirmationLabel;
                color = "red";
                defaultMessage = T.NeedsConfirmation;
            }
            else if (state == "done")
            {
                label = T.Done;
                color = "green";
                defaultMessage = T.WorkJustFinished;
            }

            var payload = new StatusPayload
            {
                state = state,
                label = label,
                color = color,
                message = String.IsNullOrWhiteSpace(message) ? defaultMessage : message,
                updatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                source = "auto"
            };

            if (state == "working")
            {
                payload.workingCount = 1;
            }
            else if (state == "confirm")
            {
                payload.confirmCount = 1;
            }
            else if (state == "done")
            {
                payload.doneCount = 1;
            }

            return payload;
        }

        private static string Normalize(string value)
        {
            value = (value ?? String.Empty).Trim().ToLowerInvariant();
            if (value == "working" || value == "work" || value == "busy" || value == "yellow") return "working";
            if (value == "confirm" || value == "confirmation" || value == "approval" || value == "input" || value == "red" || value == "blue") return "confirm";
            if (value == "done" || value == "complete" || value == "completed" || value == "finish" || value == "finished" || value == "green") return "done";
            return "waiting";
        }
    }
}
