using System;
using TwitchLib.Client.Enums;

namespace ZerdaStream {
	public class SubEventArgs : EventArgs {
		public string Username { get; }
		public string? Message { get; }
		public int CumulativeMonths { get; }
		public int StreakMonths { get; }
		public SubscriptionPlan Plan { get; }

		public SubEventArgs(string username, string? message, string cumulativeMonths, string streakMonths, SubscriptionPlan plan) {
			Username = username;
			Message = message;
			CumulativeMonths = int.Parse(cumulativeMonths);
			StreakMonths = int.Parse(streakMonths);
			Plan = plan;
		}
	}
}
