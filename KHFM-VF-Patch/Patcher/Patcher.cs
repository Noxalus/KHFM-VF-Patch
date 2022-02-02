﻿using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            var kh1Names = File.ReadAllLines(Path.Combine(ResourcePath, "kh1.txt"));
            var kh2Names = File.ReadAllLines(Path.Combine(ResourcePath, "kh2.txt"));
            var names = kh1Names.Concat(kh2Names).ToDictionary(x => ToString(MD5.HashData(Encoding.UTF8.GetBytes(x))), x => x);

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
                if (!names.TryGetValue(hash, out var filename))
                {
                    //throw new Exception("Unknown filename!");
                    Console.WriteLine($"Unknown filename: {hash}");
                    continue;
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
            var decompressedData = asset.OriginalData;
            var encryptedData = asset.OriginalRawData;
            var encryptionSeed = asset.Seed;

            // We want to replace the original file
            if (File.Exists(completeFilePath))
            {
                Debug.WriteLine($"Replacing original: {filename}!");

                using var newFileStream = File.OpenRead(completeFilePath);
                decompressedData = newFileStream.ReadAllBytes();

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
                BinaryMapping.WriteObject<Asset.Header>(seed, header);

                encryptionSeed = seed.ReadAllBytes();
                encryptedData = header.CompressedLength > -2 ? Encryption.Encrypt(compressedData, encryptionSeed) : compressedData;
            }

            // Write original file header
            BinaryMapping.WriteObject<Asset.Header>(pkgStream, header);

            var remasteredHeaders = new List<Asset.RemasteredEntry>();

            // Is there remastered assets?
            if (header.RemasteredAssetCount > 0)
            {
                remasteredHeaders = ReplaceRemasteredAssets(inputFolder, filename, asset, pkgStream, encryptionSeed, encryptedData);
            }
            else
            {
                // Make sure to write the original file after remastered assets headers
                pkgStream.Write(encryptedData);
            }

            // Write a new entry in the HED stream
            var hedHeader = new Hed.Entry()
            {
                MD5 = ToBytes(CreateMD5(filename)),
                ActualLength = decompressedData.Length,
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

            BinaryMapping.WriteObject<Hed.Entry>(hedStream, hedHeader);

            return hedHeader;
        }

        private static List<Asset.RemasteredEntry> ReplaceRemasteredAssets(string inputFolder, string originalFile, Asset asset, FileStream pkgStream, byte[] seed, byte[] originalAssetData)
        {
            var newRemasteredHeaders = new List<Asset.RemasteredEntry>();
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

                newRemasteredHeaders.Add(newRemasteredAssetHeader);

                // Write asset header in the PKG stream
                BinaryMapping.WriteObject<Asset.RemasteredEntry>(pkgStream, newRemasteredAssetHeader);

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

            return newRemasteredHeaders;
        }

        #region Utils

        private static IEnumerable<string> GetAllFiles(string folder)
        {
            return Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                            .Select(x => x.Replace($"{folder}\\", "")
                            .Replace(@"\", "/"));
        }

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

        public static Hed.Entry CreateHedEntry(string filename, byte[] decompressedData, byte[] compressedData, long offset, List<Asset.RemasteredEntry> remasteredHeaders = null)
        {
            var fileHash = CreateMD5(filename);
            // 0x10 => size of the original asset header
            // 0x30 => size of the remastered asset header
            var dataLength = compressedData.Length + 0x10;

            if (remasteredHeaders != null)
            {
                foreach (var header in remasteredHeaders)
                {
                    dataLength += header.CompressedLength + 0x30;
                }
            }

            return new Hed.Entry()
            {
                MD5 = ToBytes(fileHash),
                ActualLength = decompressedData.Length,
                DataLength = dataLength,
                Offset = offset
            };
        }

        private static string GetRelativePath(string filePath, string origin)
        {
            return filePath.Replace($"{origin}\\", "").Replace(@"\", "/");
        }

        #endregion
    }
}
