using System;

namespace ZerdaStream {
	public class RaidEventArgs : EventArgs {
		public string Username { get; }
		public int Party { get; }

		public RaidEventArgs(string username, string party) {
			Username = username;
			Party = int.Parse(party);
		}
	}
}
