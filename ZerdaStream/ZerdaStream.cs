// Planned features
// - Alerts overlay powered by Unity (that can be used in OBS or other programs)
// - Open redemptions view (rewards redeemed through channel points/etc are listed here until acknowledged)
// - Soundpad integration (viewers could play sounds in various ways)
// - Android stream deck for tablets
// - Chat commands powered by RoosterBot
// - Platform-agnosticism (not limited to Twitch)
// 
// For features that are powered by external programs (like RoosterBot or Unity) I want a single instance of ZerdaStream to be coordinating events within the stream.
// External programs will communicate to the ZS instance. Ideally, they will be .NET programs and they can seamlessly make use of this library's objects as if it's within the same runtime. But maybe that's a pipe dream.
// A Windows service could maybe let me do this, but I don't want to restrict this program to Windows, or any OS for that matter. (Though maybe the previous ideal is mutually incompatible with non-Windows OS.)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;

namespace ZerdaStream {
	public static class ZerdaStream {
		private static TwitchClient? s_Client;
		private static TwitchPubSub? s_PubSub;

		/// <summary>
		/// Chat message
		/// New sub; prime/tier
		/// Resub; prime/tier, months (streak or cumulative?)
		/// Incoming raid
		/// Incoming host
		/// Gift sub/community sub (see remarks)
		/// </summary>
		/// <remarks>
		/// OnGiftSubscription event fires when someone received a gift sub. It tells you who received the sub, if the gift was anonymous, as well as details about the sub itself.
		/// OnCommunitySubscription event fires when someone gifts a sub. It tells you how many subs were gifted, and about the sub itself.
		/// If you want to get a list of gift sub recipients, your best bet is to listen for OnGiftSubscription and wait a second for another one to appear.
		/// That way you can get a single event for all gift sub recipients.
		/// </remarks>
		public static TwitchClient Client {
			get => s_Client ?? throw new InvalidOperationException($"Library has not been initialized. Call {nameof(Setup)} first.");
			private set => s_Client = value;
		}

		/// <summary>
		/// Bits
		/// Follow
		/// </summary>
		public static TwitchPubSub PubSub {
			get => s_PubSub ?? throw new InvalidOperationException($"Library has not been initialized. Call {nameof(Setup)} first.");
			private set => s_PubSub = value;
		}

		public static event EventHandler<MessageEventArgs>? Message;
		public static event EventHandler<string>? Follow;
		public static event EventHandler<BitsEventArgs>? Bits;
		public static event EventHandler<SubEventArgs>? Sub;
		public static event EventHandler<GiftSubEventArgs>? GiftSub;
		public static event EventHandler<RaidEventArgs>? Raid;
		public static event EventHandler<string>? Host;

		public static void Setup(string channelId, string username, string oauth) {
			const int GiftSubEventDelayMillis = 1000;

			Client = new TwitchClient();
			Client.SetConnectionCredentials(new ConnectionCredentials(username, oauth));
			Client.Connect();

			PubSub = new TwitchPubSub();
			PubSub.Connect();
			PubSub.ListenToBitsEvents(channelId);
			PubSub.ListenToFollows(channelId);

			Client.OnMessageReceived += (o, e) => Message?.Invoke(null, new MessageEventArgs(e.ChatMessage.Id, e.ChatMessage.Username, e.ChatMessage.IsModerator, e.ChatMessage.Message));
			PubSub.OnFollow += (o, e) => Follow?.Invoke(null, e.DisplayName);
			PubSub.OnBitsReceived += (o, e) => Bits?.Invoke(null, new BitsEventArgs(e.Username, e.BitsUsed));

			Client.OnNewSubscriber += (o, e) => Sub?.Invoke(null, new SubEventArgs(e.Subscriber.DisplayName, e.Subscriber.ResubMessage, e.Subscriber.MsgParamCumulativeMonths, e.Subscriber.MsgParamStreakMonths, e.Subscriber.SubscriptionPlan)); // TODO MsgParamShouldShareStreak
			Client.OnReSubscriber  += (o, e) => Sub?.Invoke(null, new SubEventArgs(e.ReSubscriber.DisplayName, e.ReSubscriber.ResubMessage, e.ReSubscriber.MsgParamCumulativeMonths, e.ReSubscriber.MsgParamStreakMonths, e.ReSubscriber.SubscriptionPlan));

			Client.OnRaidNotification += (o, e) => Raid?.Invoke(null, new RaidEventArgs(e.RaidNotification.DisplayName, e.RaidNotification.MsgParamViewerCount));
			Client.OnHostingStarted += (o, e) => Host?.Invoke(null, e.HostingStarted.HostingChannel);

			Client.OnGiftedSubscription += (o, e) => TestGiftSub(e.GiftedSubscription.IsAnonymous ? null : e.GiftedSubscription.DisplayName, e.GiftedSubscription.MsgParamRecipientDisplayName, e.GiftedSubscription.MsgParamSubPlan);

			Task.Run(async () => {
				var notifiedGiftSubs = new List<string?>();
				while (true) {
					await Task.Delay(TimeSpan.FromSeconds(1));

					lock (s_GiftSubQueueLock) {
						foreach (KeyValuePair<string?, (DateTime LastReceived, SubscriptionPlan Plan, LinkedList<string> Recipients)> kvp in s_GiftSubQueue) {
							if ((DateTime.Now - kvp.Value.LastReceived).TotalMilliseconds > GiftSubEventDelayMillis) {
								notifiedGiftSubs.Add(kvp.Key);
							}
						}
					}

					foreach (string? key in notifiedGiftSubs) {
						GiftSub?.Invoke(null, new GiftSubEventArgs(key, s_GiftSubQueue[key].Recipients, s_GiftSubQueue[key].Plan));
						s_GiftSubQueue.Remove(key);
					}
					notifiedGiftSubs.Clear();
				}
			});
		}

		public static void TestGiftSub(string? username, string recipient, SubscriptionPlan plan) {
			lock (s_GiftSubQueueLock) {
				LinkedList<string> recipients;
				if (s_GiftSubQueue.ContainsKey(username)) {
					recipients = s_GiftSubQueue[username].Recipients;
				} else {
					recipients = new LinkedList<string>();
				}

				s_GiftSubQueue[username].Recipients.AddLast(recipient);
				s_GiftSubQueue[username] = (DateTime.Now, plan, recipients); // Do it like this because LastReceived gets updated, and the tuple is a value type
			}
		}

		// Identify multiple gift subs by person who gifted them, and the time in which they were received.
		// Every second, we check this dict, and if there's an item with a LastReceived more than one second ago,
		//  we fire OnGiftSub with all the people who were gifted a sub.
		// This works around the unreliability that many existing stream overlay programs have in identifying gift subs.
		// The only downside I can think of, is if multiple anonymous users gift subs at the same time, they will be seen as one.
		// But the only time that ever happens is if the streamer is being literally showered in cash, so I really don't care about fixing it.
		private static readonly Dictionary<string?, (DateTime LastReceived, SubscriptionPlan Plan, LinkedList<string> Recipients)> s_GiftSubQueue = new Dictionary<string?, (DateTime LastReceived, SubscriptionPlan Plan, LinkedList<string> Recipients)>();
		private static readonly object s_GiftSubQueueLock = new object();
	}
}
