using System;
using System.Text;
using UnityEngine;

namespace Meowdoku.Core
{
    public static class LevelBankIO
    {
        private const string Key = "meowdoku-2026-bank-secret";
        private const string ResourceDirectory = "Levels/";
        internal static Func<string, object> LoadOverride { get; set; }

        public static object LoadJson(string filename)
        {
            if (LoadOverride != null) return LoadOverride(filename);
            string resourceName = ResourceDirectory + RemoveJsonExtension(filename);
            TextAsset asset = Resources.Load<TextAsset>(resourceName);
            // Source parity: a bank absent for a given size is a normal empty
            // pool (for example LK Style only exists for sizes 7-12).
            if (asset == null) return null;

            byte[] bytes = (byte[])asset.bytes.Clone();
            XorInPlace(bytes);
            string json = Encoding.UTF8.GetString(bytes);
            return MiniJson.Deserialize(json);
        }

        private static void XorInPlace(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || Key.Length == 0) return;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= (byte)Key[i % Key.Length];
            }
        }

        private static string RemoveJsonExtension(string filename)
        {
            return filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? filename.Substring(0, filename.Length - 5)
                : filename;
        }
    }
}
