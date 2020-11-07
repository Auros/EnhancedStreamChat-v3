using EnhancedStreamChat.Chat;
using SiraUtil;
using SiraUtil.Tools;
using Zenject;

namespace EnhancedStreamChat.Zenject.Installers
{
	public class GameChatInstaller : Installer<GameChatInstaller>
	{
		private readonly SiraLog _logger;

		public GameChatInstaller(SiraLog logger)
		{
			_logger = logger;
		}
		
		public override void InstallBindings()
		{
			_logger.Info($"Installing GameChatInstaller");
			Container.BindInstance(true).WithId("InGame").AsSingle().Lazy();
			Container.BindViewController<ChatViewController>();
			Container.BindInterfacesTo<ChatScreen>().AsSingle();
		}
	}
}