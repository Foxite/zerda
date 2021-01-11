using System;
using System.Threading.Tasks;
using Qmmands;
using RoosterBot;

namespace ZerdaStream.RoosterBot {
	public class ZerdaModule : RoosterModule {
		public Random Random { get; set; } = null!;

		[Command("test gift subs")]
		public async Task TestGiftSubs(int count, bool anonymous) {
			for (int i = 0; i < count; i++) {
				ZerdaStream.TestGiftSub(anonymous ? null : Context.User.DisplayName, $"User{i}", TwitchLib.Client.Enums.SubscriptionPlan.Tier1);
				await Task.Delay(Random.Next(50, 750));
			}
		}
	}
}
