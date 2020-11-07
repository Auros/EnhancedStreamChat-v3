using System;
using ChatCore;
using ChatCore.Logging;
using ChatCore.Services;
using IPA.Logging;
using SiraUtil.Tools;
using Zenject;

namespace EnhancedStreamChat.Services
{
	public class ChatManager : IInitializable, IDisposable
	{
		private readonly SiraLog _logger;

		private ChatCoreInstance _chatCoreInstance;
		private ChatServiceMultiplexer _chatMultiplexer;

		public ChatManager(SiraLog logger)
		{
			_logger = logger;

			_chatCoreInstance = ChatCoreInstance.Create(OnChatCoreLogReceived);
			_chatMultiplexer = _chatCoreInstance.RunAllServices();
			/*_chatMultiplexer.OnJoinChannel += QueueOrSendOnJoinChannel;
			_chatMultiplexer.OnTextMessageReceived += QueueOrSendOnTextMessageReceived;
			_chatMultiplexer.OnChatCleared += QueueOrSendOnClearChat;
			_chatMultiplexer.OnMessageCleared += QueueOrSendOnClearMessage;
			_chatMultiplexer.OnChannelResourceDataCached += QueueOrSendOnChannelResourceDataCached;*/
		}
		
		public void Initialize()
		{
			// NOP
		}

		public void Dispose()
		{
			// NOP
		}
		
		private void OnChatCoreLogReceived(CustomLogLevel level, string category, string log)
		{
			var newLevel = level switch
			{
				CustomLogLevel.Critical => IPA.Logging.Logger.Level.Critical,
				CustomLogLevel.Debug => IPA.Logging.Logger.Level.Debug,
				CustomLogLevel.Error => IPA.Logging.Logger.Level.Error,
				CustomLogLevel.Information => IPA.Logging.Logger.Level.Info,
				CustomLogLevel.Trace => IPA.Logging.Logger.Level.Trace,
				CustomLogLevel.Warning => IPA.Logging.Logger.Level.Warning,
				_ => IPA.Logging.Logger.Level.None
			};

			_logger.Logger.GetChildLogger("ChatCore").Log(newLevel, log);
		}
		
		/*private void QueueOrSendOnChannelResourceDataCached(IChatService svc, IChatChannel channel, Dictionary<string, IChatResourceData> resources) => QueueOrSendMessage(svc, channel, resources, OnChannelResourceDataCached);
		private void OnChannelResourceDataCached(IChatService svc, IChatChannel channel, Dictionary<string, IChatResourceData> resources) => _chatDisplay.OnChannelResourceDataCached(channel, resources);

		private void QueueOrSendOnTextMessageReceived(IChatService svc, IChatMessage msg) => QueueOrSendMessage(svc, msg, OnTextMesssageReceived);
		private void OnTextMesssageReceived(IChatService svc, IChatMessage msg) => _chatDisplay.OnTextMessageReceived(msg);

		private void QueueOrSendOnJoinChannel(IChatService svc, IChatChannel channel) => QueueOrSendMessage(svc, channel, OnJoinChannel);
		private void OnJoinChannel(IChatService svc, IChatChannel channel) => _chatDisplay.OnJoinChannel(svc, channel);

		private void QueueOrSendOnClearMessage(IChatService svc, string messageId) => QueueOrSendMessage(svc, messageId, OnClearMessage);
		private void OnClearMessage(IChatService svc, string messageId) => _chatDisplay.OnMessageCleared(messageId);

		private void QueueOrSendOnClearChat(IChatService svc, string userId) => QueueOrSendMessage(svc, userId, OnClearChat);
		private void OnClearChat(IChatService svc, string userId) => _chatDisplay.OnChatCleared(userId);*/
	}
}