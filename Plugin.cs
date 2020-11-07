using System.Reflection;
using EnhancedStreamChat.Zenject.Installers;
using IPA;
using IPA.Config;
using IPA.Loader;
using SemVer;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace EnhancedStreamChat
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        private static PluginMetadata? _metadata;
        private static string? _name;
        private static Version? _version;

        public static string Name => _name ??= _metadata?.Name ?? Assembly.GetExecutingAssembly().GetName().Name;
        public static Version Version => _version ??= _metadata?.Version ?? new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString());

        [Init]
        public void Init(IPALogger logger, Config config, PluginMetadata pluginMetadata, Zenjector zenject)
        {
            _metadata = pluginMetadata;

            zenject.OnApp<CoreChatInstaller>().WithParameters(logger, config);
            zenject.OnMenu<MenuChatInstaller>();
            zenject.OnGame<GameChatInstaller>();
        }

        [OnEnable]
        public void OnEnable()
        {
            // Nope, this will be handled by SiraUtil
        }

        [OnDisable]
        public void OnDisable()
        {
            // NOP, this will be handled by SiraUtil
        }
    }
}
