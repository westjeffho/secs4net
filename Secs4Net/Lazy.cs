using System;
using System.Threading;

namespace Secs4Net
{
    sealed class Lazy<T> where T : class
    {
        readonly Func<T> creator;
        T _value;
        public Lazy(Func<T> func)
        {
            creator = func;
        }
        public Lazy(T value)
        {
            _value = value;
        }
        public T Value
        {
            get
            {
                if (Volatile.Read<T>(ref _value) != null)
                    return _value;
                Interlocked.CompareExchange(ref _value, creator(), null);
                return _value;
            }
        }
    }

    static class Lazy
    {
        public static Lazy<T> Create<T>(Func<T> creator) where T : class => new Lazy<T>(creator);

        public static Lazy<T> Create<T>(T value) where T : class => new Lazy<T>(value);
    }
}
