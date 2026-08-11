using System;
using System.Text;

namespace Meowdoku.Core.Profile
{
    public interface IProfileRandom
    {
        int NextInclusive(int minimum, int maximum);
    }

    public sealed class SystemProfileRandom : IProfileRandom
    {
        private readonly Random _random;

        public SystemProfileRandom() : this(new Random()) { }

        public SystemProfileRandom(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum < minimum)
                throw new ArgumentOutOfRangeException(nameof(maximum));
            return _random.Next(minimum, maximum + 1);
        }
    }

    public static class ProfileNickname
    {
        private const int Length = 6;
        private const string Alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string RandomDefault(IProfileRandom random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var builder = new StringBuilder(Length);
            for (int index = 0; index < Length; index++)
                builder.Append(Alphabet[random.NextInclusive(
                    0,
                    Alphabet.Length - 1)]);
            return builder.ToString();
        }

        public static string TruncateCodePoints(string value, int maximum)
        {
            if (string.IsNullOrEmpty(value) || maximum <= 0)
                return string.Empty;
            int codePoints = 0;
            int index = 0;
            while (index < value.Length && codePoints < maximum)
            {
                index += char.IsHighSurrogate(value[index]) &&
                         index + 1 < value.Length &&
                         char.IsLowSurrogate(value[index + 1])
                    ? 2
                    : 1;
                codePoints++;
            }
            return index >= value.Length ? value : value.Substring(0, index);
        }
    }
}
