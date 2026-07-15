using System;
using System.IO;
using UnityEngine;

namespace Tier9.Core
{
    public static class SaveSystem
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static void Save(AccountState account)
        {
            try
            {
                account.lastSeenUtcTicks = DateTime.UtcNow.Ticks;
                string json = JsonUtility.ToJson(account, prettyPrint: true);
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", overwrite: true);
                File.Copy(tmp, SavePath, overwrite: true);
                File.Delete(tmp);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tier9] Save failed: {e}");
            }
        }

        public static AccountState Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var acc = JsonUtility.FromJson<AccountState>(File.ReadAllText(SavePath));
                return acc != null && acc.characters != null ? acc : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tier9] Load failed, starting fresh: {e}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(SavePath + ".bak")) File.Delete(SavePath + ".bak");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Tier9] Delete save failed: {e}");
            }
        }
    }
}
