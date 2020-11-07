using System;
using BeatSaberMarkupLanguage.FloatingScreen;
using HMUI;
using IPA.Utilities;
using SiraUtil.Tools;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace EnhancedStreamChat.Chat
{
	public class ChatScreen : IInitializable, IDisposable
	{
		private readonly SiraLog _logger;
		private readonly ChatConfig _chatConfig;
		private readonly PhysicsRaycasterWithCache _physicsRaycasterWithCache;
		private readonly ChatViewController _chatViewController;
		private readonly bool _inGame;

		private FloatingScreen? _floatingScreen;

		[Inject]
		internal ChatScreen(SiraLog logger, ChatConfig chatConfig, PhysicsRaycasterWithCache physicsRaycasterWithCache, ChatViewController chatViewController, [Inject(Id = "InGame")] bool inGame)
		{
			_logger = logger;

			_logger.Logger.Trace($"Constructing {nameof(ChatScreen)} with args: {nameof(inGame)}: {inGame}");

			_chatConfig = chatConfig;
			_physicsRaycasterWithCache = physicsRaycasterWithCache;
			_chatViewController = chatViewController;
			_inGame = inGame;
		}

		public void Initialize()
		{
			_floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(_chatConfig.ChatWidth, _chatConfig.ChatHeight), true,
				_inGame ? _chatConfig.GameChatPosition : _chatConfig.MenuChatPosition,
				Quaternion.identity, hasBackground: true);
			_floatingScreen.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", _physicsRaycasterWithCache);

			_floatingScreen.SetRootViewController(_chatViewController, ViewController.AnimationType.None);

			_floatingScreen.ScreenRotation = Quaternion.Euler(_inGame ? _chatConfig.GameChatPosition : _chatConfig.MenuChatRotation);

			_floatingScreen.HandleReleased += OnHandleRelease;
		}

		public void Dispose()
		{
			_logger.Logger.Trace($"Disposing {nameof(ChatScreen)} with args: {nameof(_inGame)}: {_inGame}");

			if (_floatingScreen != null)
			{
				_floatingScreen.HandleReleased -= OnHandleRelease;
			}
		}

		private void OnHandleRelease(object sender, FloatingScreenHandleEventArgs floatingScreenHandleEventArgs)
		{
			using (_chatConfig.ChangeTransaction)
			{
				if (_inGame)
				{
					_chatConfig.GameChatPosition = floatingScreenHandleEventArgs.Position;
					_chatConfig.GameChatRotation = floatingScreenHandleEventArgs.Rotation.eulerAngles;
				}
				else
				{
					_chatConfig.MenuChatPosition = floatingScreenHandleEventArgs.Position;
					_chatConfig.MenuChatRotation = floatingScreenHandleEventArgs.Rotation.eulerAngles;
				}
			}
		}
	}
}