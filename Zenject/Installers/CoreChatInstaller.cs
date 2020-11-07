using EnhancedStreamChat.Services;
using IPA.Config.Stores;
using SiraUtil;
using Zenject;
using Config = IPA.Config.Config;

namespace EnhancedStreamChat.Zenject.Installers
{
	public class CoreChatInstaller : Installer<IPA.Logging.Logger, CoreChatInstaller>
	{
		private readonly IPA.Logging.Logger _logger;
		private readonly Config _config;

		public CoreChatInstaller(IPA.Logging.Logger logger, Config config)
		{
			_logger = logger;
			_config = config;
		}

		public override void InstallBindings()
		{
			Container.BindLoggerAsSiraLogger(_logger);
			
			Container.BindInstance(ChatConfig.Instance ??= _config.Generated<ChatConfig>()).AsSingle().Lazy();

			Container.Bind<ChatImageProvider>().AsSingle().Lazy();
			Container.BindInterfacesAndSelfTo<ChatManager>().AsSingle().NonLazy();

			//Container.BindMemoryPool<EnhancedTextMeshProUGUIWithBackground>().WithInitialSize(25).ExpandByOneAtATime();
		}
	}
}