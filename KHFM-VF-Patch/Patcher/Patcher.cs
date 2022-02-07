using Ionic.Zlib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xe.BinaryMapper;

namespace KHFM_VF_Patch
{
    public class PatchProgressEventArgs : EventArgs
    {
        public int EntriesTotal { get; set; }
        public int EntriesPatched { get; set; }
        public string LastReplacedFile { get; set; }
    }

    public static class Patcher
    {
        public static event EventHandler<PatchProgressEventArgs> PatchProgress;

        private static readonly string ResourcePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "Resources");

        private const string ORIGINAL_FILES_FOLDER_NAME = "original";
        private const string REMASTERED_FILES_FOLDER_NAME = "remastered";

        public static void Patch(string pkgFile, string inputFolder, string outputFolder)
        {
            var kh1Names = File.ReadAllLines(Path.Combine(ResourcePath, "kh1.txt")).ToDictionary(x => ToString(MD5.HashData(Encoding.UTF8.GetBytes(x))), x => x);

            var remasteredFilesFolder = Path.Combine(inputFolder, REMASTERED_FILES_FOLDER_NAME);

            var outputDir = outputFolder ?? Path.GetFileNameWithoutExtension(pkgFile);

            var hedFile = Path.ChangeExtension(pkgFile, "hed");
            using var hedStream = File.OpenRead(hedFile);
            using var pkgStream = File.OpenRead(pkgFile);

            var hedHeaders = Hed.Read(hedStream).ToList();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            using var patchedHedStream = File.Create(Path.Combine(outputDir, Path.GetFileName(hedFile)));
            using var patchedPkgStream = File.Create(Path.Combine(outputDir, Path.GetFileName(pkgFile)));

            var eventArgs = new PatchProgressEventArgs()
            {
                EntriesPatched = 0,
                EntriesTotal = hedHeaders.Count
            };

            foreach (var hedHeader in hedHeaders)
            {
                var hash = ToString(hedHeader.MD5);

                // We don't know this filename, we ignore it
                if (!kh1Names.TryGetValue(hash, out var filename))
                {
                    throw new Exception("Unknown filename!");
                }

                //Debug.WriteLine(filename);

                var asset = new Asset(pkgStream.SetPosition(hedHeader.Offset), hedHeader);

                if (hedHeader.DataLength > 0)
                {
                    ReplaceFile(inputFolder, filename, patchedHedStream, patchedPkgStream, asset, hedHeader);
                }
                else
                {
                    Debug.WriteLine($"Skipped: {filename}");
                }

                eventArgs.EntriesPatched++;
                PatchProgress?.Invoke(null, eventArgs);
            }
        }

        private static Hed.Entry ReplaceFile(
            string inputFolder,
            string filename,
            FileStream hedStream,
            FileStream pkgStream,
            Asset asset,
            Hed.Entry originalHedHeader = null)
        {
            var completeFilePath = Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME, filename);

            var offset = pkgStream.Position;
            var originalHeader = asset.OriginalAssetHeader;

            // Clone the original asset header
            var header = new Asset.Header()
            {
                CompressedLength = originalHeader.CompressedLength,
                DecompressedLength = originalHeader.DecompressedLength,
                RemasteredAssetCount = originalHeader.RemasteredAssetCount,
                CreationDate = originalHeader.CreationDate
            };

            // Use the base original asset data by default
            var decompressedDataSize = asset.OriginalAssetHeader.DecompressedLength;

            byte[] encryptedData = null;
            var encryptionSeed = asset.Seed;
            var isOriginalFileReplaced = false;

            // We want to replace the original file
            if (File.Exists(completeFilePath))
            {
                // Only read data if needed
                asset.ReadData();

                Debug.WriteLine($"Replacing original: {filename}!");

                using var newFileStream = File.OpenRead(completeFilePath);
                var decompressedData = newFileStream.ReadAllBytes();

                var compressedData = decompressedData.ToArray();
                var compressedDataLenght = originalHeader.CompressedLength;

                // CompressedLenght => -2: no compression and encryption, -1: no compression 
                if (originalHeader.CompressedLength > -1)
                {
                    compressedData = CompressData(decompressedData);
                    compressedDataLenght = compressedData.Length;
                }

                header.CompressedLength = compressedDataLenght;
                header.DecompressedLength = decompressedData.Length;

                // Encrypt and write current file data in the PKG stream

                // The seed used for encryption is the original data header
                var seed = new MemoryStream();
                BinaryMapping.WriteObject(seed, header);

                encryptionSeed = seed.ReadAllBytes();
                encryptedData = header.CompressedLength > -2 ? Encryption.Encrypt(compressedData, encryptionSeed) : compressedData;

                decompressedDataSize = decompressedData.Length;

                isOriginalFileReplaced = true;
            }

            // Write original file header
            BinaryMapping.WriteObject(pkgStream, header);

            // Is there remastered assets?
            if (header.RemasteredAssetCount > 0)
            {
                // Should we replace a remastered asset?
                if (isOriginalFileReplaced || ShouldReplaceRemasteredAssets(inputFolder, filename, asset))
                {
                    ReplaceRemasteredAssets(inputFolder, filename, asset, pkgStream, encryptionSeed, encryptedData);
                }
                else
                {
                    // Keep the orignal and the remastered assets data untouched in new PKG file
                    // In order :
                    // 1. Write all remastered assets original headers
                    // 2. Write the original file data
                    // 3. Write all remastered assets data

                    // Write remastered assets original headers
                    var totalRemasteredAssetHeadersSize = asset.RemasteredAssetHeaders.Count() * 0x30;
                    var remasteredAssetDataOffset = totalRemasteredAssetHeadersSize + 0x10 + asset.OriginalAssetHeader.DecompressedLength;

                    foreach (var remasteredAssetHeader in asset.RemasteredAssetHeaders)
                    {
                        // Just change the offset
                        var newRemasteredAssetHeader = new Asset.RemasteredEntry()
                        {
                            CompressedLength = remasteredAssetHeader.Value.CompressedLength,
                            DecompressedLength = remasteredAssetHeader.Value.DecompressedLength,
                            Name = remasteredAssetHeader.Value.Name,
                            Offset = remasteredAssetDataOffset,
                            Unknown24 = remasteredAssetHeader.Value.Unknown24
                        };

                        BinaryMapping.WriteObject(pkgStream, newRemasteredAssetHeader);

                        remasteredAssetDataOffset += newRemasteredAssetHeader.DecompressedLength;
                    }

                    // Write original data
                    var originalDataSize = header.CompressedLength > -1 ? header.CompressedLength : header.DecompressedLength;
                    asset.Stream.SetPosition(asset.DataOffset);
                    var originalAssetData = asset.Stream.ReadBytes(originalDataSize);
                    pkgStream.Write(originalAssetData);

                    // Write remastered assets data
                    foreach (var remasteredAssetHeader in asset.RemasteredAssetHeaders)
                    {
                        var remasteredAssetSize = remasteredAssetHeader.Value.CompressedLength > -1 ? remasteredAssetHeader.Value.CompressedLength : remasteredAssetHeader.Value.DecompressedLength;
                        var remasteredAssetData = asset.Stream.ReadBytes(remasteredAssetSize);
                        pkgStream.Write(remasteredAssetData);

                        if (remasteredAssetData.Length % 0x10 != 0)
                        {
                            var alignmentSize = 16 - (remasteredAssetData.Length % 0x10);
                            var alignmentBytes = asset.Stream.ReadBytes(alignmentSize);

                            pkgStream.Write(alignmentBytes);
                        }
                    }
                }
            }
            else
            {
                // Write original data
                var originalDataSize = header.CompressedLength > -1 ? header.CompressedLength : header.DecompressedLength;
                asset.Stream.CopyTo(pkgStream, asset.DataOffset, originalDataSize);
            }

            // Write a new entry in the HED stream
            var hedHeader = new Hed.Entry()
            {
                MD5 = ToBytes(CreateMD5(filename)),
                ActualLength = decompressedDataSize,
                DataLength = (int)(pkgStream.Position - offset),
                Offset = offset
            };

            // For unknown reason, some files have a data length of 0
            if (originalHedHeader.DataLength == 0)
            {
                Debug.WriteLine($"{filename} => {originalHedHeader.ActualLength} ({originalHedHeader.DataLength})");

                hedHeader.ActualLength = originalHedHeader.ActualLength;
                hedHeader.DataLength = originalHedHeader.DataLength;
            }

            BinaryMapping.WriteObject(hedStream, hedHeader);

            return hedHeader;
        }

        private static bool ShouldReplaceRemasteredAssets(string inputFolder, string originalFile, Asset asset)
        {
            var relativePath = GetRelativePath(originalFile, Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME));
            var remasteredAssetsFolder = Path.Combine(inputFolder, REMASTERED_FILES_FOLDER_NAME, relativePath);

            foreach (var remasteredAssetHeader in asset.RemasteredAssetHeaders.Values)
            {
                var filename = remasteredAssetHeader.Name;
                var assetFilePath = Path.Combine(remasteredAssetsFolder, filename);

                if (File.Exists(assetFilePath))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ReplaceRemasteredAssets(string inputFolder, string originalFile, Asset asset, FileStream pkgStream, byte[] seed, byte[] originalAssetData = null)
        {
            asset.ReadData();
            originalAssetData = originalAssetData ?? asset.OriginalRawData;

            var relativePath = GetRelativePath(originalFile, Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME));
            var remasteredAssetsFolder = Path.Combine(inputFolder, REMASTERED_FILES_FOLDER_NAME, relativePath);

            var allRemasteredAssetsData = new MemoryStream();
            // 0x30 is the size of this header
            var totalRemasteredAssetHeadersSize = asset.RemasteredAssetHeaders.Count() * 0x30;
            // This offset is relative to the original asset data
            var offset = totalRemasteredAssetHeadersSize + 0x10 + asset.OriginalAssetHeader.DecompressedLength;

            if (offset != asset.RemasteredAssetHeaders.Values.First().Offset)
            {
                throw new Exception("Something is wrong here!");
            }

            foreach (var remasteredAssetHeader in asset.RemasteredAssetHeaders.Values)
            {
                var filename = remasteredAssetHeader.Name;
                var assetFilePath = Path.Combine(remasteredAssetsFolder, filename);

                // Use base remastered asset data
                var assetData = asset.RemasteredAssetsCompressedData[filename];
                var decompressedLength = remasteredAssetHeader.DecompressedLength;

                if (File.Exists(assetFilePath))
                {
                    Debug.WriteLine($"Replacing remastered file: {relativePath}/{filename}");
                    assetData = File.ReadAllBytes(assetFilePath);
                    decompressedLength = assetData.Length;
                    assetData = remasteredAssetHeader.CompressedLength > -1 ? CompressData(assetData) : assetData;
                    assetData = remasteredAssetHeader.CompressedLength > -2 ? Encryption.Encrypt(assetData, seed) : assetData;
                }
                else
                {
                    Debug.WriteLine($"Keeping remastered file: {relativePath}/{filename}");

                    // The original file have been replaced, we need to encrypt all remastered asset with the new key
                    if (!seed.SequenceEqual(asset.Seed))
                    {
                        assetData = asset.RemasteredAssetsDecompressedData[filename];
                        assetData = remasteredAssetHeader.CompressedLength > -1 ? CompressData(assetData) : assetData;
                        assetData = remasteredAssetHeader.CompressedLength > -2 ? Encryption.Encrypt(assetData, seed) : assetData;
                    }
                }

                var compressedLength = remasteredAssetHeader.CompressedLength > -1 ? assetData.Length : remasteredAssetHeader.CompressedLength;

                var newRemasteredAssetHeader = new Asset.RemasteredEntry()
                {
                    CompressedLength = compressedLength,
                    DecompressedLength = decompressedLength,
                    Name = filename,
                    Offset = offset,
                    Unknown24 = remasteredAssetHeader.Unknown24
                };

                //if (/*newRemasteredAssetHeader.CompressedLength != remasteredAssetHeader.CompressedLength ||*/
                //    newRemasteredAssetHeader.DecompressedLength != remasteredAssetHeader.DecompressedLength ||
                //    newRemasteredAssetHeader.Name != remasteredAssetHeader.Name ||
                //    /*newRemasteredAssetHeader.Offset != remasteredAssetHeader.Offset ||*/
                //    newRemasteredAssetHeader.Unknown24 != remasteredAssetHeader.Unknown24)
                //{
                //    throw new Exception("Something is wrong with the remastered asset header");
                //}

                // Write asset header in the PKG stream
                BinaryMapping.WriteObject(pkgStream, newRemasteredAssetHeader);

                // Don't write into the PKG stream yet as we need to write
                // all HD assets header juste after original file's data
                allRemasteredAssetsData.Write(assetData);

                // Make sure to align remastered asset data on 16 bytes
                if (assetData.Length % 0x10 != 0)
                {
                    allRemasteredAssetsData.Write(Enumerable.Repeat((byte)0xCD, 16 - (assetData.Length % 0x10)).ToArray());
                }

                offset += decompressedLength;
            }

            pkgStream.Write(originalAssetData);
            pkgStream.Write(allRemasteredAssetsData.ReadAllBytes());
        }

        #region Utils

        private static string ToString(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            for (var i = 0; i < data.Length; i++)
                sb.Append(data[i].ToString("X02"));

            return sb.ToString();
        }

        public static byte[] ToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static byte[] CompressData(byte[] data)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                var deflateStream = new ZlibStream(compressedStream, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.Default, true);

                deflateStream.Write(data, 0, data.Length);
                deflateStream.Close();

                var compressedData = compressedStream.ReadAllBytes();

                // Make sure compressed data is aligned with 0x10
                int padding = compressedData.Length % 0x10 == 0 ? 0 : (0x10 - compressedData.Length % 0x10);
                Array.Resize(ref compressedData, compressedData.Length + padding);

                return compressedData;
            }
        }

        private static string GetRelativePath(string filePath, string origin)
        {
            return filePath.Replace($"{origin}\\", "").Replace(@"\", "/");
        }

        #endregion
    }
}
