using System;
using System.Collections.Generic;
using TwitchLib.Client.Enums;

namespace ZerdaStream {
	public class GiftSubEventArgs : EventArgs {
		public string? GifterName { get; }
		public IReadOnlyCollection<string> Recipients { get; }
		public SubscriptionPlan Plan { get; }

		public GiftSubEventArgs(string? gifterName, IReadOnlyCollection<string> recipients, SubscriptionPlan plan) {
			GifterName = gifterName;
			Recipients = recipients;
			Plan = plan;
		}
	}
}
