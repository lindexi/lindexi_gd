using System;

namespace HearwhejiyehallyiheFubaduwheefu
{
    public static class Registry
    {
        /// <summary>Current User Key. This key should be used as the root for all user specific settings.</summary>
        public static readonly RegistryKey CurrentUser = new RegistryKey("HKEY_CURRENT_USER");

        /// <summary>Local Machine key. This key should be used as the root for all machine specific settings.</summary>
        public static readonly RegistryKey LocalMachine = new RegistryKey("HKEY_LOCAL_MACHINE");

        /// <summary>Classes Root Key. This is the root key of class information.</summary>
        public static readonly RegistryKey ClassesRoot = new RegistryKey("HKEY_CLASSES_ROOT");

        /// <summary>Users Root Key. This is the root of users.</summary>
        public static readonly RegistryKey Users = new RegistryKey("HKEY_USERS");

        /// <summary>Performance Root Key. This is where dynamic performance data is stored on NT.</summary>
        public static readonly RegistryKey PerformanceData = new RegistryKey("HKEY_PERFORMANCE_DATA");

        /// <summary>Current Config Root Key. This is where current configuration information is stored.</summary>
        public static readonly RegistryKey CurrentConfig = new RegistryKey("HKEY_CURRENT_CONFIG");


        /// <summary>
        /// Parse a keyName and returns the basekey for it.
        /// It will also store the subkey name in the out parameter.
        /// If the keyName is not valid, we will throw ArgumentException.
        /// The return value shouldn't be null.
        /// </summary>
        public static RegistryKey OldGetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            int i = keyName.IndexOf('\\');
            int length = i != -1 ? i : keyName.Length;

            // Determine the potential base key from the length.
            RegistryKey? baseKey = null;
            switch (length)
            {
                case 10: baseKey = Users; break; // HKEY_USERS
                case 17: baseKey = char.ToUpperInvariant(keyName[6]) == 'L' ? ClassesRoot : CurrentUser; break; // HKEY_C[L]ASSES_ROOT, otherwise HKEY_CURRENT_USER
                case 18: baseKey = LocalMachine; break; // HKEY_LOCAL_MACHINE
                case 19: baseKey = CurrentConfig; break; // HKEY_CURRENT_CONFIG
                case 21: baseKey = PerformanceData; break; // HKEY_PERFORMANCE_DATA
            }

            // If a potential base key was found, see if keyName actually starts with the potential base key's name.
            if (baseKey != null && keyName.StartsWith(baseKey.Name, StringComparison.OrdinalIgnoreCase))
            {
                subKeyName = (i == -1 || i == keyName.Length) ?
                    string.Empty :
                    keyName.Substring(i + 1, keyName.Length - i - 1);

                return baseKey;
            }

            //throw new ArgumentException(SR.Format(SR.Arg_RegInvalidKeyName, nameof(keyName)), nameof(keyName));
            subKeyName = null;
            return null;
        }

        /// <summary>
        /// Parse a keyName and returns the basekey for it.
        /// It will also store the subkey name in the out parameter.
        /// If the keyName is not valid, we will throw ArgumentException.
        /// The return value shouldn't be null.
        /// </summary>
        public static RegistryKey NewGetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            const int minBaseKeyLength = 10; // HKEY_USERS
            if (keyName.Length >= minBaseKeyLength)
            {
                int i = keyName.IndexOf('\\', startIndex: minBaseKeyLength);
                int length = i != -1 ? i : keyName.Length;

                // Determine the potential base key from the length.
                RegistryKey? baseKey = null;
                switch (length)
                {
                    case 10: baseKey = Users; break; // HKEY_USERS
                    case 17: baseKey = char.ToUpperInvariant(keyName[6]) == 'L' ? ClassesRoot : CurrentUser; break; // HKEY_C[L]ASSES_ROOT, otherwise HKEY_CURRENT_USER
                    case 18: baseKey = LocalMachine; break; // HKEY_LOCAL_MACHINE
                    case 19: baseKey = CurrentConfig; break; // HKEY_CURRENT_CONFIG
                    case 21: baseKey = PerformanceData; break; // HKEY_PERFORMANCE_DATA
                }

                // If a potential base key was found, see if keyName actually starts with the potential base key's name.
                if (baseKey != null && keyName.StartsWith(baseKey.Name, StringComparison.OrdinalIgnoreCase))
                {
                    subKeyName = (i == -1 || i == keyName.Length) ?
                        string.Empty :
                        keyName.Substring(i + 1, keyName.Length - i - 1);

                    return baseKey;
                }
            }

            //throw new ArgumentException(SR.Format(SR.Arg_RegInvalidKeyName, nameof(keyName)), nameof(keyName));
            subKeyName = null;
            return null;
        }
    }
}