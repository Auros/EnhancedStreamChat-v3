using IPA.Utilities.Async;
using System;
using System.Threading;

namespace EnhancedStreamChat.Utilities
{
	public class MainThreadInvoker
	{
		private static CancellationTokenSource _cancellationToken = new CancellationTokenSource();

		public static void ClearQueue()
		{
			_cancellationToken.Cancel();
			_cancellationToken = new CancellationTokenSource();
		}

		public static void Invoke(Action? action)
		{
			if (action != null)
			{
				UnityMainThreadTaskScheduler.Factory.StartNew(action, _cancellationToken.Token);
			}
		}

		public static void Invoke<TA>(Action<TA?>? action, TA? a)
			where TA : class
		{
			if (action != null)
			{
				UnityMainThreadTaskScheduler.Factory.StartNew(() => action(a), _cancellationToken.Token);
			}
		}

		public static void Invoke<TA, TB>(Action<TA?, TB?>? action, TA? a, TB? b)
			where TA : class
			where TB : class
		{
			if (action != null)
			{
				UnityMainThreadTaskScheduler.Factory.StartNew(() => action(a, b), _cancellationToken.Token);
			}
		}
	}
}