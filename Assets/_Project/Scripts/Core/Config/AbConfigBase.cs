using System;

namespace Meowdoku.Core.Config
{
    public interface IAbConfig
    {
        string Key { get; }
        string Timing { get; }
        bool IsValueLoaded { get; }
        void ReloadValue(IAbValueProvider provider);
    }

    public interface IAbValueProvider
    {
        int GetInt(string key, int defaultValue);
        string GetString(string key, string defaultValue);
    }

    public interface IAbDyeSink
    {
        void Dye(string key);
    }

    public sealed class DefaultAbValueProvider : IAbValueProvider
    {
        public static readonly DefaultAbValueProvider Instance = new DefaultAbValueProvider();

        private DefaultAbValueProvider() { }

        public int GetInt(string key, int defaultValue) => defaultValue;
        public string GetString(string key, string defaultValue) => defaultValue;
    }

    public abstract class AbConfigBase<T> : IAbConfig
    {
        private T _value;
        private bool _valueLoaded;
        private bool _hasDebugOverride;
        private T _debugOverride;

        protected AbConfigBase(string key, T defaultValue, string timing)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            DefaultValue = defaultValue;
            Timing = timing ?? throw new ArgumentNullException(nameof(timing));
            _value = defaultValue;
            _debugOverride = defaultValue;
        }

        public string Key { get; }
        public T DefaultValue { get; }
        public string Timing { get; }
        public bool IsDebugDisabled { get; private set; }
        public bool IsValueLoaded => _valueLoaded || _hasDebugOverride;

        public T Value => _hasDebugOverride
            ? _debugOverride
            : (_valueLoaded ? _value : DefaultValue);

        public void InitDefault()
        {
            _value = DefaultValue;
        }

        public void ReloadValue(IAbValueProvider provider)
        {
            IAbValueProvider resolved = provider ?? DefaultAbValueProvider.Instance;
            _value = Read(resolved);
            _valueLoaded = true;
            if (resolved is IAbDyeSink dyeSink)
                dyeSink.Dye(Key);
        }

        public T PeekValue(IAbValueProvider provider = null)
        {
            return _hasDebugOverride
                ? _debugOverride
                : Read(provider ?? DefaultAbValueProvider.Instance);
        }

        public void SetDebugOverride(T value)
        {
            _debugOverride = value;
            _hasDebugOverride = true;
        }

        public void ClearDebugOverride()
        {
            _debugOverride = DefaultValue;
            _hasDebugOverride = false;
        }

        public void SetDebugDisabled(bool disabled)
        {
            IsDebugDisabled = disabled;
        }

        public string CheatLabel() => Key;
        public string CheatValueString() => Convert.ToString(Value);

        private T Read(IAbValueProvider provider)
        {
            if (typeof(T) == typeof(int))
            {
                int result = provider.GetInt(Key, Convert.ToInt32(DefaultValue));
                return (T)(object)result;
            }

            if (typeof(T) == typeof(string))
            {
                string result = provider.GetString(Key, Convert.ToString(DefaultValue));
                return (T)(object)result;
            }

            throw new NotSupportedException(
                $"AB config type {typeof(T).Name} is not present in the Godot source.");
        }
    }
}
