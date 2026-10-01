// SPDX-FileCopyrightText: 2026 Ikpil Choi(ikpil@naver.com)
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Box2D.NET
{
    /// A bounded cache of reusable managed objects. Each borrowed object belongs exclusively
    /// to its caller until returned. The limit controls retention, not concurrent borrowers.
    /// Pool operations are thread-safe; borrowed objects are not made thread-safe by the pool.
    public sealed class B2ObjectPool<T> : IDisposable where T : class
    {
        private readonly object _lock = new object();
        private readonly Stack<T> _items = new Stack<T>();
        private readonly Func<T> _create;
        private readonly Func<T, bool> _reset;
        private readonly int _maximumRetained;
        private bool _disposed;

        /// The reset policy clears caller state and returns false to reject retention.
        /// Policies run outside the storage lock and must support simultaneous calls on
        /// different objects. The pool does not call IDisposable.Dispose on stored objects.
        public B2ObjectPool(Func<T> create, Func<T, bool> reset, int maximumRetained = 32)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (reset == null)
            {
                throw new ArgumentNullException(nameof(reset));
            }

            if (maximumRetained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumRetained));
            }

            _create = create;
            _reset = reset;
            _maximumRetained = maximumRetained;
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        /// Get an idle object, or create one without waiting for another borrower.
        public T Get()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(B2ObjectPool<T>));
                }

                if (_items.Count > 0)
                {
                    return _items.Pop();
                }
            }

            return _create() ?? throw new InvalidOperationException("The object pool factory returned null.");
        }

        /// Return an object exactly once to the pool it came from. Do not use it afterward.
        /// Even after the pool closes, the policy clears references before the object is dropped.
        public void Return(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_reset(item) == false)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposed == false && _items.Count < _maximumRetained)
                {
                    _items.Push(item);
                }
            }
        }

        /// Close the pool and release its references to idle objects. Outstanding borrowers
        /// can still return to this instance, but their objects will not be retained.
        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _items.Clear();
            }
        }
    }
}
