using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Tests.EditMode
{
    public class SaveSystemTests
    {
        [Serializable]
        private class DummyPayload
        {
            public int version = 1;
            public int gold = 0;
        }

        private string CreateTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MedievalRPG_SaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void CleanupDir(string dir)
        {
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        private static DummyPayload DeserializeDummy(string json)
        {
            if (!SaveFileUtility.LooksLikeJsonObject(json)) return null;
            string trimmed = json.Trim();
            int versionIdx = trimmed.IndexOf("\"version\":", StringComparison.Ordinal);
            int goldIdx = trimmed.IndexOf("\"gold\":", StringComparison.Ordinal);
            if (versionIdx < 0 || goldIdx < 0) return null;

            string goldTail = trimmed.Substring(goldIdx + "\"gold\":".Length).TrimStart();
            int end = 0;
            while (end < goldTail.Length && char.IsDigit(goldTail[end])) end++;
            if (end == 0 || !int.TryParse(goldTail.Substring(0, end), out int goldVal)) return null;

            return new DummyPayload { version = 1, gold = goldVal };
        }

        [Test]
        public void SaveFileUtility_WriteAtomicCreatesBackupAndRecoversFromCorruptPrimary()
        {
            string dir = CreateTempDir();
            try
            {
                string savePath = Path.Combine(dir, "test_save.json");
                SaveFileUtility.WriteAtomicWithBackup(savePath, "{\"version\":1,\"gold\":100}");
                Assert.IsTrue(File.Exists(savePath));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetBackupPath(savePath)));

                SaveFileUtility.WriteAtomicWithBackup(savePath, "{\"version\":1,\"gold\":250}");
                Assert.IsTrue(File.Exists(savePath));
                Assert.IsTrue(File.Exists(SaveFileUtility.GetBackupPath(savePath)));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetTempPath(savePath)));

                // Làm hỏng file chính
                File.WriteAllText(savePath, "{corrupt_truncated_json");

                DummyPayload loaded = SaveFileUtility.LoadWithBackup(
                    savePath,
                    DeserializeDummy,
                    out bool recovered,
                    out bool corruptDetected);

                Assert.IsNotNull(loaded);
                Assert.IsTrue(recovered);
                Assert.IsTrue(corruptDetected);
                Assert.AreEqual(100, loaded.gold);
                Assert.IsTrue(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));
                Assert.IsTrue(File.Exists(savePath), "File chính phải được khôi phục từ backup");
            }
            finally
            {
                CleanupDir(dir);
            }
        }

        [Test]
        public void SaveFileUtility_BothPrimaryAndBackupCorrupt_QuarantinesSafely()
        {
            string dir = CreateTempDir();
            try
            {
                string savePath = Path.Combine(dir, "broken_save.json");
                File.WriteAllText(savePath, "not json at all");
                File.WriteAllText(SaveFileUtility.GetBackupPath(savePath), "{}");

                DummyPayload loaded = SaveFileUtility.LoadWithBackup(
                    savePath,
                    DeserializeDummy,
                    out bool recovered,
                    out bool corruptDetected);

                Assert.IsNull(loaded);
                Assert.IsFalse(recovered);
                Assert.IsTrue(corruptDetected);
                Assert.IsFalse(File.Exists(savePath));
                Assert.IsTrue(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));
                Assert.IsTrue(File.Exists(SaveFileUtility.GetCorruptPath(SaveFileUtility.GetBackupPath(savePath))));
            }
            finally
            {
                CleanupDir(dir);
            }
        }

        [Test]
        public void PlayerCampProgressManager_DiskPersistence_RecoversFromCorruptPrimaryAndDeletesBackupOnReset()
        {
            string dir = CreateTempDir();
            try
            {
                string savePath = Path.Combine(dir, "camp.json");

                PlayerCampProgressManager writer = new PlayerCampProgressManager();
                writer.InitializeForDiskTests(savePath, 180);
                writer.LoadIfNeeded();
                writer.AddGold(120); // 300 vàng -> Save lần 1 (file chính = 300)
                Assert.IsTrue(writer.TryBuyConsumable(ConsumableItemType.HealthPotion)); // 260 vàng -> Save lần 2 (.bak = 300, chính = 260)

                // Làm hỏng file chính
                File.WriteAllText(savePath, "{\"version\":2,\"gold\":");

                PlayerCampProgressManager reader = new PlayerCampProgressManager();
                reader.InitializeForDiskTests(savePath);
                reader.LoadIfNeeded();

                Assert.AreEqual(300, reader.Gold, "Phải tự khôi phục đúng lượng vàng từ bản backup trước khi corrupt");
                Assert.IsTrue(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));

                reader.ResetAllProgress(alsoClearEquipment: false);
                Assert.IsFalse(File.Exists(savePath));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetBackupPath(savePath)));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));
            }
            finally
            {
                CleanupDir(dir);
            }
        }

        [Test]
        public void PlayerEquipmentManager_DiskPersistence_RecoversFromCorruptPrimaryAndClearsBackupOnReset()
        {
            string dir = CreateTempDir();
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeFallbackCatalog();
            try
            {
                string savePath = Path.Combine(dir, "equipment.json");
                EquipmentItemData item = catalog.allItems[0];

                PlayerEquipmentManager writer = new PlayerEquipmentManager();
                writer.InitializeForDiskTests(savePath, catalog);
                writer.LoadIfNeeded();
                writer.AddItem(item, 1); // Save lần 1: 1 món
                writer.AddItem(item, 2); // Save lần 2: .bak = 1 món, chính = 2 món

                // Làm hỏng file chính bằng object rỗng hoặc JSON lỗi
                File.WriteAllText(savePath, "{}");

                PlayerEquipmentManager reader = new PlayerEquipmentManager();
                reader.InitializeForDiskTests(savePath, catalog);
                reader.LoadIfNeeded();

                Assert.AreEqual(1, reader.OwnedCount, "Phải tự khôi phục 1 món từ bản backup");
                Assert.IsTrue(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));

                reader.ClearAllForReset();
                Assert.IsFalse(File.Exists(savePath));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetBackupPath(savePath)));
                Assert.IsFalse(File.Exists(SaveFileUtility.GetCorruptPath(savePath)));
            }
            finally
            {
                foreach (EquipmentItemData item in catalog.allItems)
                {
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                }
                UnityEngine.Object.DestroyImmediate(catalog);
                CleanupDir(dir);
            }
        }
    }
}
