using System;
using System.Collections.Generic;

using MeasureDeflection.Utils;
using MeasureDeflection.Processor;


namespace MarkerScannerTest.Utils
{
    public class EventSink
    {
        public BlobCentre Anchor;
        public BlobCentre MovingTip;
        public readonly List<string> MyLog = new List<string>();


        public void OnAnchorSetEvent(BlobCentre anchor)
        {
            Anchor = anchor;
        }

        public void OnMovingTipSetEvent(BlobCentre movingTip)
        {
            MovingTip = movingTip;
        }

        public void ResetPoints()
        {
            Anchor = new BlobCentre();
            MovingTip = new BlobCentre();
        }

        /// <summary>
        /// Promt handler for user notifications
        /// </summary>
        /// <param name="type">Notification urgency</param>
        /// <param name="message">Message</param>
        public void PromptNewMessage_Handler(UserPrompt.eNotifyType type, string message)
        {
            var prompt = $"'{type,8}': {message}";
            MyLog.Add(prompt);
        }
    }
}
