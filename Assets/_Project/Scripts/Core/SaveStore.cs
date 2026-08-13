using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Meowdoku.Core
{
    public enum LegacySaveMigrationResult
    {
        NotNeeded,
        Migrated,
        Failed
    }

    /// <summary>
    /// Unity storage adapter for game_state/save_store.gd.
    /// It preserves verified writes, dual-slot fallback, flag selection and
    /// legacy fallback. Its encrypted file format is Unity-specific and is not
    /// binary-compatible with Godot ConfigFile.save_encrypted_pass.
    /// </summary>
    public sealed class SaveStore
    {
        private const int LoadAttempts = 3;
        private const int RetryDelayMilliseconds = 60;
        private const int SaltLength = 16;
        private const int IvLength = 16;
        private const int TagLength = 32;
        private const int DerivedKeyLength = 64;
        private const int Pbkdf2Iterations = 100000;

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MDKS1");

        private readonly string _password;
        private readonly string _directory;
        private readonly bool _dualSlot;
        private readonly string _pathA;
        private readonly string _pathB;
        private readonly string _flagPath;
        private readonly string _legacyPath;
        private readonly Action<string> _beforeVerify;

        public SaveStore(
            string password,
            string directory,
            bool dualSlot,
            string pathA,
            string pathB = "",
            string flagPath = "",
            string legacyPath = "")
            : this(
                password,
                directory,
                dualSlot,
                pathA,
                pathB,
                flagPath,
                legacyPath,
                null)
        {
        }

        internal SaveStore(
            string password,
            string directory,
            bool dualSlot,
            string pathA,
            string pathB,
            string flagPath,
            string legacyPath,
            Action<string> beforeVerify)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Save password cannot be empty.", nameof(password));
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Save directory cannot be empty.", nameof(directory));
            if (string.IsNullOrEmpty(pathA))
                throw new ArgumentException("Primary save path cannot be empty.", nameof(pathA));
            if (dualSlot && (string.IsNullOrEmpty(pathB) || string.IsNullOrEmpty(flagPath)))
                throw new ArgumentException("Dual-slot stores require path B and a flag path.");

            _password = password;
            _directory = directory;
            _dualSlot = dualSlot;
            _pathA = pathA;
            _pathB = pathB;
            _flagPath = flagPath;
            _legacyPath = legacyPath;
            _beforeVerify = beforeVerify;
        }

        public bool SaveConfig(IDictionary<string, object> config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            return SaveSerializedConfig(MiniJson.Serialize(config));
        }

        internal bool SaveSerializedConfig(string serializedConfig)
        {
            if (serializedConfig == null)
                throw new ArgumentNullException(nameof(serializedConfig));

            if (!_dualSlot)
            {
                return AtomicWrite(serializedConfig, _pathA);
            }

            string lastGood = ReadFlag();
            string target = lastGood != "A" ? "A" : "B";
            string finalPath = target == "A" ? _pathA : _pathB;
            if (!AtomicWrite(serializedConfig, finalPath))
            {
                return false;
            }

            WriteFlag(target);
            return true;
        }

        public Dictionary<string, object> LoadConfig()
        {
            for (int attempt = 0; attempt < LoadAttempts; attempt++)
            {
                Dictionary<string, object> config = LoadOnce();
                if (config != null)
                {
                    return config;
                }

                if (attempt < LoadAttempts - 1)
                {
                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }

            return null;
        }

        public LegacySaveMigrationResult MigrateLegacyIfNeeded()
        {
            if (!_dualSlot ||
                (!string.IsNullOrEmpty(_flagPath) && File.Exists(_flagPath)) ||
                string.IsNullOrEmpty(_legacyPath) ||
                !File.Exists(_legacyPath))
            {
                return LegacySaveMigrationResult.NotNeeded;
            }

            Dictionary<string, object> legacy = TryRead(_legacyPath);
            if (legacy == null || !SaveConfig(legacy))
            {
                return LegacySaveMigrationResult.Failed;
            }

            return LegacySaveMigrationResult.Migrated;
        }

        public void Remove()
        {
            TryDelete(_pathA);
            TryDelete(_pathA + ".tmp");
            TryDelete(_pathA + ".bak");
        }

        private Dictionary<string, object> LoadOnce()
        {
            if (!_dualSlot)
            {
                return TryRead(_pathA);
            }

            string lastGood = ReadFlag();
            string primary = lastGood == "B" ? _pathB : _pathA;
            string backup = lastGood == "B" ? _pathA : _pathB;

            Dictionary<string, object> config = TryRead(primary);
            if (config != null)
            {
                return config;
            }

            config = TryRead(backup);
            if (config != null)
            {
                return config;
            }

            if (!string.IsNullOrEmpty(_legacyPath))
            {
                return TryRead(_legacyPath);
            }

            return null;
        }

        private bool AtomicWrite(string serializedConfig, string finalPath)
        {
            string temporaryPath = finalPath + ".tmp";
            string backupPath = finalPath + ".bak";
            try
            {
                Directory.CreateDirectory(_directory);
                byte[] plainText = Encoding.UTF8.GetBytes(serializedConfig);
                byte[] encrypted = Encrypt(plainText);
                WriteAllBytesFlushed(temporaryPath, encrypted);
                _beforeVerify?.Invoke(temporaryPath);

                if (TryRead(temporaryPath) == null)
                {
                    TryDelete(temporaryPath);
                    return false;
                }

                ReplaceFile(temporaryPath, finalPath, backupPath);
                return true;
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
                TryDelete(temporaryPath);
                return false;
            }
        }

        private Dictionary<string, object> TryRead(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                byte[] plainText = Decrypt(File.ReadAllBytes(path));
                object parsed = MiniJson.Deserialize(Encoding.UTF8.GetString(plainText));
                return parsed as Dictionary<string, object>;
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
                return null;
            }
        }

        private string ReadFlag()
        {
            if (string.IsNullOrEmpty(_flagPath) || !File.Exists(_flagPath))
            {
                return string.Empty;
            }

            try
            {
                string flag = File.ReadAllText(_flagPath, Encoding.UTF8).Trim();
                return flag == "A" || flag == "B" ? flag : string.Empty;
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
                return string.Empty;
            }
        }

        private void WriteFlag(string slot)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                WriteAllBytesFlushed(_flagPath, Encoding.UTF8.GetBytes(slot));
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
            }
        }

        private byte[] Encrypt(byte[] plainText)
        {
            byte[] salt = new byte[SaltLength];
            byte[] iv = new byte[IvLength];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
                random.GetBytes(iv);
            }

            byte[] derivedKey = DeriveKey(salt);
            byte[] cipherText;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Slice(derivedKey, 0, 32);
                aes.IV = iv;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    cipherText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                }
            }

            byte[] authenticated = Join(Magic, salt, iv, cipherText);
            byte[] tag;
            using (var hmac = new HMACSHA256(Slice(derivedKey, 32, 32)))
            {
                tag = hmac.ComputeHash(authenticated);
            }

            Array.Clear(derivedKey, 0, derivedKey.Length);
            return Join(authenticated, tag);
        }

        private byte[] Decrypt(byte[] payload)
        {
            int headerLength = Magic.Length + SaltLength + IvLength;
            if (payload == null || payload.Length < headerLength + TagLength + 1)
            {
                throw new CryptographicException("Save payload is truncated.");
            }

            for (int i = 0; i < Magic.Length; i++)
            {
                if (payload[i] != Magic[i])
                    throw new CryptographicException("Save payload header is invalid.");
            }

            int cipherLength = payload.Length - headerLength - TagLength;
            byte[] salt = Slice(payload, Magic.Length, SaltLength);
            byte[] iv = Slice(payload, Magic.Length + SaltLength, IvLength);
            byte[] cipherText = Slice(payload, headerLength, cipherLength);
            byte[] expectedTag = Slice(payload, headerLength + cipherLength, TagLength);
            byte[] authenticated = Slice(payload, 0, headerLength + cipherLength);
            byte[] derivedKey = DeriveKey(salt);

            byte[] actualTag;
            using (var hmac = new HMACSHA256(Slice(derivedKey, 32, 32)))
            {
                actualTag = hmac.ComputeHash(authenticated);
            }

            if (!FixedTimeEquals(actualTag, expectedTag))
            {
                Array.Clear(derivedKey, 0, derivedKey.Length);
                throw new CryptographicException("Save payload authentication failed.");
            }

            byte[] plainText;
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Slice(derivedKey, 0, 32);
                aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                }
            }

            Array.Clear(derivedKey, 0, derivedKey.Length);
            return plainText;
        }

        private byte[] DeriveKey(byte[] salt)
        {
            using (var derivation = new Rfc2898DeriveBytes(
                _password,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256))
            {
                return derivation.GetBytes(DerivedKeyLength);
            }
        }

        private static void ReplaceFile(string temporaryPath, string finalPath, string backupPath)
        {
            if (!File.Exists(finalPath))
            {
                File.Move(temporaryPath, finalPath);
                return;
            }

            TryDelete(backupPath);
            try
            {
                File.Replace(temporaryPath, finalPath, backupPath, true);
                TryDelete(backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                ReplaceFileFallback(temporaryPath, finalPath, backupPath);
            }
        }

        private static void ReplaceFileFallback(
            string temporaryPath,
            string finalPath,
            string backupPath)
        {
            File.Move(finalPath, backupPath);
            try
            {
                File.Move(temporaryPath, finalPath);
                TryDelete(backupPath);
            }
            catch
            {
                if (!File.Exists(finalPath) && File.Exists(backupPath))
                {
                    File.Move(backupPath, finalPath);
                }
                throw;
            }
        }

        private static void WriteAllBytesFlushed(string path, byte[] bytes)
        {
            using (var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static bool FixedTimeEquals(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < first.Length; i++)
            {
                difference |= first[i] ^ second[i];
            }
            return difference == 0;
        }

        private static byte[] Slice(byte[] source, int offset, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(source, offset, result, 0, count);
            return result;
        }

        private static byte[] Join(params byte[][] arrays)
        {
            int length = 0;
            for (int i = 0; i < arrays.Length; i++) length += arrays[i].Length;
            var result = new byte[length];
            int offset = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                Buffer.BlockCopy(arrays[i], 0, result, offset, arrays[i].Length);
                offset += arrays[i].Length;
            }
            return result;
        }

        private static void TryDelete(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception exception) when (IsStorageException(exception))
            {
            }
        }

        private static bool IsStorageException(Exception exception)
        {
            return exception is IOException ||
                   exception is UnauthorizedAccessException ||
                   exception is CryptographicException ||
                   exception is FormatException ||
                   exception is InvalidOperationException;
        }
    }
}
