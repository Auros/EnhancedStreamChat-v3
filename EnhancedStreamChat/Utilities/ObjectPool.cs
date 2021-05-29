using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedStreamChat.Utilities
{
	/// <summary>
	/// A dynamic pool of unity components of type T, that recycles old objects when possible, and allocates new objects when required.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectPool<T> : IDisposable where T : Component
	{
		private readonly Queue<T> _freeObjects;
		private readonly Action<T>? _firstAlloc;
		private readonly Action<T>? _onAlloc;
		private readonly Action<T>? _onFree;
		private readonly Func<T>? _constructor;
		private readonly object _lock = new object();

		/// <summary>
		/// ObjectPool constructor function, used to setup the initial pool size and callbacks.
		/// </summary>
		/// <param name="initialCount">The number of components of type T to allocate right away.</param>
		/// <param name="constructor">The constructor function used to create new objects in the pool.</param>
		/// <param name="firstAlloc">The callback function you want to occur only the first time when a new component of type T is allocated.</param>
		/// <param name="onAlloc">The callback function to be called everytime ObjectPool.Alloc() is called.</param>
		/// <param name="onFree">The callback function to be called everytime ObjectPool.Free() is called</param>
		public ObjectPool(int initialCount = 0, Func<T>? constructor = null, Action<T>? firstAlloc = null, Action<T>? onAlloc = null, Action<T>? onFree = null)
		{
			_constructor = constructor;
			_firstAlloc = firstAlloc;
			_onAlloc = onAlloc;
			_onFree = onFree;
			_freeObjects = new Queue<T>(initialCount);

			while (initialCount-- > 0)
			{
				_freeObjects.Enqueue(InternalAlloc());
			}
		}

		~ObjectPool()
		{
			Dispose();
		}

		public void Dispose()
		{
			Dispose(false);
		}

		public void Dispose(bool immediate)
		{
			lock (_lock)
			{
				foreach (T obj in _freeObjects)
				{
					if (immediate)
					{
						UnityEngine.Object.DestroyImmediate(obj.gameObject);
					}
					else
					{
						UnityEngine.Object.Destroy(obj.gameObject);
					}
				}

				_freeObjects.Clear();
			}
		}

		private T InternalAlloc()
		{
			T newObj = _constructor is null ? new GameObject().AddComponent<T>() : _constructor.Invoke();

			_firstAlloc?.Invoke(newObj);
			return newObj;
		}

		/// <summary>
		/// Allocates a component of type T from a pre-allocated pool, or instantiates a new one if required.
		/// </summary>
		/// <returns></returns>
		public T Alloc()
		{
			lock (_lock)
			{
				T? obj = null;
				if (_freeObjects.Count > 0)
				{
					obj = _freeObjects.Dequeue();
				}

				if (!obj || obj == null)
				{
					obj = InternalAlloc();
				}

				_onAlloc?.Invoke(obj);
				return obj;
			}
		}

		/// <summary>
		/// Inserts a component of type T into the stack of free objects. Note: the component does *not* need to be allocated using ObjectPool.Alloc() to be freed with this function!
		/// </summary>
		/// <param name="obj"></param>
		public void Free(T obj)
		{
			lock (_lock)
			{
				if (obj == null) return;
				_freeObjects.Enqueue(obj);
				_onFree?.Invoke(obj);
			}
		}
	}
}