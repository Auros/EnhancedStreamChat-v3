using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace EnhancedStreamChat.Chat
{
	[HotReload(RelativePathToLayout = @"Chat.bsml")]
	[ViewDefinition("EnhancedStreamChat.Chat.Chat.bsml")]
	public class ChatViewController : BSMLAutomaticViewController
	{
		[UIAction("#post-parse")]
		private void PostParse()
		{
			
		}
	}
}