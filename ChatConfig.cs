using System;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using SiraUtil.Converters;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace EnhancedStreamChat
{
	internal class ChatConfig
	{
		internal static ChatConfig? Instance { get; set; }

		internal event EventHandler? ConfigChanged;

		// Main
		// When set to true, animated emotes will be precached in memory when the game starts.
		public virtual bool PreCacheAnimatedEmotes { get; set; } = true;

		// UI
		// The name of the system font to be used in chat
		public virtual string SystemFontName { get; set; } = "Segoe UI";

		// The background color of the chat
		[UseConverter(typeof(HexColorConverter))]
		public virtual Color BackgroundColor { get; set; } = Color.black.ColorWithAlpha(0.5f);

		// The base color of the chat text.
		[UseConverter(typeof(HexColorConverter))]
		public virtual Color TextColor { get; set; } = Color.white;

		// The accent color to be used on system messages
		[UseConverter(typeof(HexColorConverter))]
		public virtual Color AccentColor { get; set; } = new Color(0.57f, 0.28f, 1f, 1f);

		// The highlight color to be used on system messages
		[UseConverter(typeof(HexColorConverter))]
		public virtual Color HighlightColor { get; set; } = new Color(0.57f, 0.28f, 1f, 0.06f);

		// The color pings will be highlighted as in chat
		[UseConverter(typeof(HexColorConverter))]
		public virtual Color PingColor { get; set; } = new Color(1f, 0f, 0f, 0.13f);

		// General Layout
		// The width of the chat
		public virtual int ChatWidth { get; set; } = 120;

		// The height of the chat
		public virtual int ChatHeight { get; set; } = 140;

		// The size of the font
		public virtual float FontSize { get; set; } = 3.4f;

		// Allow movement of the chat
		public virtual bool AllowMovement { get; set; } = false;

		// Sync positions and rotations for the chat in menu and in-game
		public virtual bool SyncOrientation { get; set; } = false;

		// Reverse the order of the chat
		public virtual bool ReverseChatOrder { get; set; } = false;

		// In-Menu Layout
		// The world position of the chat while at the main menu
		[UseConverter(typeof(Vector3Converter))]
		public virtual Vector3 MenuChatPosition { get; set; } = new Vector3(0, 3.75f, 2.5f);

		// The world rotation of the chat while at the main menu
		[UseConverter(typeof(Vector3Converter))]
		public virtual Vector3 MenuChatRotation { get; set; } = new Vector3(325, 0, 0);

		// In-Song Layout
		// The world position of the chat while in-song
		[UseConverter(typeof(Vector3Converter))]
		public virtual Vector3 GameChatPosition { get; set; } = new Vector3(0, 3.75f, 2.5f);

		// The world rotation of the chat while in-song
		[UseConverter(typeof(Vector3Converter))]
		public virtual Vector3 GameChatRotation { get; set; } = new Vector3(325, 0, 0);


		public virtual void Changed()
		{
			// this is called whenever one of the virtual properties is changed
			// can be called to signal that the content has been changed

			ConfigChanged?.Invoke(this, EventArgs.Empty);
		}

		public virtual IDisposable ChangeTransaction => null!;
	}
}