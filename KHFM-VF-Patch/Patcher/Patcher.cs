using Ionic.Zlib;
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

        private static readonly string ResourcePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "resources");

        private const string ORIGINAL_FILES_FOLDER_NAME = "original";
        private const string REMASTERED_FILES_FOLDER_NAME = "remastered";

        public static void Patch(string pkgFile, string inputFolder, string outputFolder)
        {
            var kh1Names = File.ReadAllLines(Path.Combine(ResourcePath, "kh1.txt")).ToDictionary(x => ToString(MD5.HashData(Encoding.UTF8.GetBytes(x))), x => x);

            var originalFilesFolder = Path.Combine(inputFolder, Patcher.ORIGINAL_FILES_FOLDER_NAME);

            if (!Directory.Exists(originalFilesFolder))
            {
                Console.WriteLine($"Unable to find folder {originalFilesFolder}, please make sure files to packs are there.");
            }

            var outputDir = outputFolder ?? Path.GetFileNameWithoutExtension(pkgFile);

            // Get files to inject in the PKG
            var inputFiles = Directory.Exists(originalFilesFolder) ? GetAllFiles(originalFilesFolder).ToList() : new List<string>();

            var hedFile = Path.ChangeExtension(pkgFile, "hed");
            using var hedStream = File.OpenRead(hedFile);
            using var pkgStream = File.OpenRead(pkgFile);

            var hedEntries = Hed.Read(hedStream).ToList();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            using var patchedHedStream = File.Create(Path.Combine(outputDir, Path.GetFileName(hedFile)));
            using var patchedPkgStream = File.Create(Path.Combine(outputDir, Path.GetFileName(pkgFile)));

            pkgStream.SetPosition(0);

            var eventArgs = new PatchProgressEventArgs()
            {
                EntriesPatched = 0,
                EntriesTotal = hedEntries.Count
            };

            foreach (var entry in hedEntries)
            {
                var hash = ToString(entry.MD5);

                // We don't know this filename, we ignore it
                if (!kh1Names.TryGetValue(hash, out var filename))
                {
                    continue;
                }

                Debug.WriteLine(filename);

                // Replace the found files
                if (inputFiles.Contains(filename))
                {
                    inputFiles.Remove(filename);

                    var asset = new Asset(pkgStream.SetPosition(entry.Offset), entry);
                    var fileToInject = Path.Combine(originalFilesFolder, filename);
                    var shouldCompressData = asset.OriginalAssetHeader.CompressedLength > 0;
                    var newHedEntry = ReplaceFile(inputFolder, fileToInject, patchedHedStream, patchedPkgStream, asset, entry);

                    Debug.WriteLine($"Replaced file: {filename}");

                    eventArgs.LastReplacedFile = filename;

                    //Debug.WriteLine("HED");
                    //Debug.WriteLine($"ActualLength: {entry.ActualLength} | {newHedEntry.ActualLength}");
                    //Debug.WriteLine($"DataLength: {entry.DataLength} | {newHedEntry.DataLength}");
                    //Debug.WriteLine($"Offset: {entry.Offset} | {newHedEntry.Offset}");
                    //Debug.WriteLine($"MD5: {EpicGamesAssets.ToString(entry.MD5)} | {EpicGamesAssets.ToString(newHedEntry.MD5)}");
                }
                // Write the original data
                else
                {
                    pkgStream.SetPosition(entry.Offset);

                    var data = new byte[entry.DataLength];
                    var dataLenght = pkgStream.Read(data, 0, entry.DataLength);

                    if (dataLenght != entry.DataLength)
                    {
                        throw new Exception($"Error, can't read  {entry.DataLength} bytes for file {filename}. (only read {dataLenght})");
                    }

                    // Write the HED entry with new offset
                    entry.Offset = patchedPkgStream.Position;
                    BinaryMapping.WriteObject<Hed.Entry>(patchedHedStream, entry);

                    // Write the PKG file with the original asset file data
                    patchedPkgStream.Write(data);
                }

                eventArgs.EntriesPatched++;
                PatchProgress?.Invoke(null, eventArgs);
            }

            // Add all files that are not in the original HED file and inject them in the PKG stream too
            //foreach (var filename in inputFiles)
            //{
            //    var newFilePath = Path.Combine(inputFolder, filename);
            //    AddFile(inputFolder, newFilePath, true, patchedHedStream, patchedPkgStream);
            //    Console.WriteLine($"Added a new file: {filename}");
            //}
        }

        private static Hed.Entry AddFile(string inputFolder, string input, bool shouldCompressData, FileStream hedStream, FileStream pkgStream)
        {
            using var newFileStream = File.OpenRead(input);
            var filename = GetRelativePath(input, Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME));
            var compressedData = shouldCompressData ? CompressData(newFileStream.ReadAllBytes()) : newFileStream.ReadAllBytes();
            var comrpessedDataLenght = compressedData.Length == newFileStream.Length ? -1 : compressedData.Length;
            var offset = pkgStream.Position;

            // Encrypt and write current file data in the PKG stream
            var header = CreateAssetHeader(newFileStream, comrpessedDataLenght);

            // The seed used for encryption is the data header
            var seed = new MemoryStream();
            BinaryMapping.WriteObject<Asset.Header>(seed, header);
            var encryptionKey = seed.ReadAllBytes();
            var encryptedFileData = Encryption.Encrypt(compressedData, encryptionKey);

            BinaryMapping.WriteObject<Asset.Header>(pkgStream, header);
            pkgStream.Write(encryptedFileData);

            // Write a new entry in the HED stream
            var hedEntry = CreateHedEntry(filename, newFileStream, compressedData, offset);
            BinaryMapping.WriteObject<Hed.Entry>(hedStream, hedEntry);

            return hedEntry;
        }

        private static Hed.Entry ReplaceFile(string inputFolder, string fileToInject, FileStream hedStream, FileStream pkgStream, Asset asset, Hed.Entry originalHedHeader = null)
        {
            using var newFileStream = File.OpenRead(fileToInject);
            var filename = GetRelativePath(fileToInject, Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME));
            var offset = pkgStream.Position;
            var originalHeader = asset.OriginalAssetHeader;
            var decompressedData = newFileStream.ReadAllBytes();
            var compressedData = decompressedData;
            var compressedDataLenght = originalHeader.CompressedLength;

            // CompressedLenght => -2: no compression and encryption, -1: no compression 
            if (originalHeader.CompressedLength > -1)
            {
                compressedData = CompressData(decompressedData);
                compressedDataLenght = compressedData.Length;
            }

            var header = CreateAssetHeader(
                newFileStream,
                compressedDataLenght,
                originalHeader.RemasteredAssetCount,
                originalHeader.Unknown0c
            );

            BinaryMapping.WriteObject<Asset.Header>(pkgStream, header);

            // Encrypt and write current file data in the PKG stream

            // The seed used for encryption is the data header
            var seed = new MemoryStream();
            BinaryMapping.WriteObject<Asset.Header>(seed, header);

            var encryptionKey = seed.ReadAllBytes();
            var encryptedFileData = header.CompressedLength > -2 ? Encryption.Encrypt(compressedData, encryptionKey) : compressedData;

            Console.WriteLine($"Replaced original file: {filename}");

            var remasteredHeaders = new List<Asset.RemasteredEntry>();

            // Is there remastered assets?
            if (header.RemasteredAssetCount > 0)
            {
                remasteredHeaders = ReplaceRemasteredAssets(inputFolder, fileToInject, asset, pkgStream, encryptionKey, encryptedFileData);
            }
            else
            {
                // Make sure to write the original file after remastered assets headers
                pkgStream.Write(encryptedFileData);
            }

            // Write a new entry in the HED stream
            var hedEntry = CreateHedEntry(filename, newFileStream, compressedData, offset, remasteredHeaders);

            BinaryMapping.WriteObject<Hed.Entry>(hedStream, hedEntry);

            //Debug.WriteLine("Data header");
            //Debug.WriteLine($"CompressedLength: {asset.OriginalAssetHeader.CompressedLength} | {header.CompressedLength}");
            //Debug.WriteLine($"DecompressedLength: {asset.OriginalAssetHeader.DecompressedLength} | {header.DecompressedLength}");
            //Debug.WriteLine($"RemasteredAssetCount: {asset.OriginalAssetHeader.RemasteredAssetCount} | {header.RemasteredAssetCount}");
            //Debug.WriteLine($"Unknown0c: {asset.OriginalAssetHeader.Unknown0c} | {header.Unknown0c}");

            return hedEntry;
        }

        private static List<Asset.RemasteredEntry> ReplaceRemasteredAssets(string inputFolder, string originalFile, Asset asset, FileStream pkgStream, byte[] seed, byte[] originalAssetData)
        {
            var newRemasteredHeaders = new List<Asset.RemasteredEntry>();
            var relativePath = GetRelativePath(originalFile, Path.Combine(inputFolder, ORIGINAL_FILES_FOLDER_NAME));
            var remasteredAssetsFolder = Path.Combine(inputFolder, REMASTERED_FILES_FOLDER_NAME, relativePath);

            var allRemasteredAssetsData = new MemoryStream();
            // 0x30 is the size of this header
            var totalRemasteredAssetHeadersSize = asset.RemasteredAssetHeaders.Count() * 0x30;
            var offsetPosition = asset.OriginalAssetHeader.DecompressedLength;

            foreach (var remasteredAssetHeader in asset.RemasteredAssetHeaders.Values)
            {
                var remasteredAssetFile = remasteredAssetHeader.Name;
                var assetFilePath = Path.Combine(remasteredAssetsFolder, remasteredAssetFile);
                byte[] assetData;
                var decompressedLength = 0;

                if (File.Exists(assetFilePath))
                {
                    Console.WriteLine($"Replacing remastered file: {relativePath}/{remasteredAssetFile}");

                    assetData = File.ReadAllBytes(assetFilePath);
                    decompressedLength = assetData.Length;
                    assetData = remasteredAssetHeader.CompressedLength > -1 ? CompressData(assetData) : assetData;
                    assetData = remasteredAssetHeader.CompressedLength > -2 ? Encryption.Encrypt(assetData, seed) : assetData;
                }
                else
                {
                    Console.WriteLine($"Keeping remastered file: {relativePath}/{remasteredAssetFile}");

                    decompressedLength = asset.RemasteredAssetsData[remasteredAssetFile].Length;

                    var rawData = asset.RemasteredAssetsRawData[remasteredAssetFile];

                    assetData = asset.RemasteredAssetsData[remasteredAssetFile];
                    assetData = remasteredAssetHeader.CompressedLength > -1 ? CompressData(assetData) : assetData;
                    assetData = remasteredAssetHeader.CompressedLength > -2 ? Encryption.Encrypt(assetData, seed) : assetData;

                    // assetData and rawData should be equals, but it's not the case, why?
                }

                var compressedLength = remasteredAssetHeader.CompressedLength > -1 ? assetData.Length : remasteredAssetHeader.CompressedLength;

                // This offset is relative to the original asset data
                var currentOffset = offsetPosition + totalRemasteredAssetHeadersSize + 0x10;

                var newRemasteredAssetHeader = new Asset.RemasteredEntry()
                {
                    CompressedLength = compressedLength,
                    DecompressedLength = decompressedLength,
                    Name = remasteredAssetFile,
                    Offset = currentOffset,
                    Unknown24 = remasteredAssetHeader.Unknown24
                };

                newRemasteredHeaders.Add(newRemasteredAssetHeader);

                // Write asset header in the PKG stream
                BinaryMapping.WriteObject<Asset.RemasteredEntry>(pkgStream, newRemasteredAssetHeader);

                // Don't write into the PKG stream yet as we need to write
                // all HD assets header juste after original file's data
                allRemasteredAssetsData.Write(assetData);

                offsetPosition += decompressedLength;
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

        public static Hed.Entry CreateHedEntry(string filename, FileStream fileStream, byte[] data, long offset, List<Asset.RemasteredEntry> remasteredHeaders = null)
        {
            var fileHash = CreateMD5(filename);
            // 0x10 => size of the original asset header
            // 0x30 => size of the remastered asset header
            var dataLength = data.Length + 0x10;

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
                ActualLength = (int)fileStream.Length,
                DataLength = dataLength,
                Offset = offset
            };
        }

        public static Asset.Header CreateAssetHeader(FileStream fileStream, int compressedDataLenght, int remasteredAssetCount = 0, int unknown0c = 0x0)
        {
            return new Asset.Header()
            {
                CompressedLength = compressedDataLenght,
                DecompressedLength = (int)fileStream.Length,
                RemasteredAssetCount = remasteredAssetCount,
                Unknown0c = unknown0c
            };
        }

        private static string GetHDAssetFolder(string assetFile)
        {
            var parentFolder = Directory.GetParent(assetFile).FullName;
            var assetFolderName = Path.Combine(parentFolder, $"{Path.GetFileName(assetFile)}");

            return assetFolderName;
        }

        private static string GetRelativePath(string filePath, string origin)
        {
            return filePath.Replace($"{origin}\\", "").Replace(@"\", "/");
        }

        #endregion
    }
}
