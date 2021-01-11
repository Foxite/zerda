using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using RoosterBot;

namespace ZerdaStream.RoosterBot {
	public class ZerdaStreamComponent : Component {
		public override Version ComponentVersion => new Version(0, 1, 0);

		public string m_ChannelId = null!;
		public string m_Username = null!;
		public string m_OauthToken = null!;

		protected override void AddServices(IServiceCollection services, string configPath) {
			var config = Util.LoadJsonConfigFromTemplate(Path.Combine(configPath, "Config.json"), new {
				ChannelId = "",
				Username = "",
				OauthToken = ""
			});

			m_ChannelId = config.ChannelId;
			m_Username = config.Username;
			m_OauthToken = config.OauthToken;
		}

		protected override void AddModules(IServiceProvider services, RoosterCommandService commandService) {
			ZerdaStream.Setup(m_ChannelId, m_Username, m_OauthToken);

			commandService.AddModule<ZerdaModule>();
		}
	}
}
