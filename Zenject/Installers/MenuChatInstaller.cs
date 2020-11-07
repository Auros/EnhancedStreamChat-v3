using EnhancedStreamChat.Chat;
using SiraUtil;
using SiraUtil.Tools;
using Zenject;

namespace EnhancedStreamChat.Zenject.Installers
{
	public class MenuChatInstaller : Installer<MenuChatInstaller>
	{
		private readonly SiraLog _logger;

		public MenuChatInstaller(SiraLog logger)
		{
			_logger = logger;
		}

		public override void InstallBindings()
		{
			_logger.Info($"Installing MenuChatInstaller");
			Container.BindInstance(false).WithId("InGame").AsSingle().Lazy();
			Container.BindViewController<ChatViewController>();
			Container.BindInterfacesTo<ChatScreen>().AsSingle();
			/*Container.BindViewController<ChatDisplay>(BeatSaberUI.CreateViewController<ChatDisplay>());*/
		}
	}
}