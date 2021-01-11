using System;

namespace ZerdaStream {
	public class BitsEventArgs : EventArgs {
		public string Username { get; }
		public int Amount { get; }

		public BitsEventArgs(string username, int amount) {
			Username = username;
			Amount = amount;
		}
	}
}
