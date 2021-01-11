using System;

namespace ZerdaStream {
	public class MessageEventArgs : EventArgs {
		public string Id { get; }
		public string Username { get; }
		public bool IsMod { get; }
		public string Content { get; }

		public MessageEventArgs(string id, string username, bool isMod, string content) {
			Id = id;
			Username = username;
			IsMod = isMod;
			Content = content;
		}
	}
}
