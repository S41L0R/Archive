using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Archive.Core
{
    class Archive
    {
        private FileStream archFStream;

        public Archive(String fileName)
        {
            archFStream = new FileStream(fileName, FileMode.OpenOrCreate);

            // Make sure that the entry list exists
            if (archFStream.Length == 0)
            {
                BinaryWriter bwStream = new BinaryWriter(archFStream);
                bwStream.BaseStream.Position = 32;
                long entryListEndIndex = 40;
                bwStream.Write(entryListEndIndex);
            }
            else
            {
                VerifyHash();
            }

            UpdateHash();
        }

        public void AddFolder(String folderName, String key, bool compress, CompressionLevel compressionLevel)
        {
            BinaryReader brStream = new BinaryReader(archFStream);
            BinaryWriter bwStream = new BinaryWriter(archFStream);
            SHA256 sha256 = SHA256.Create();

            byte[] folderBytes = GetFolderBytes(folderName);
            byte[] compressedFolderBytes = CompressBytes(XORByteArrays(folderBytes, sha256.ComputeHash(Encoding.ASCII.GetBytes(key))), compress, compressionLevel);

            // Write to the entry list
            brStream.BaseStream.Position = 32;
            long position = brStream.ReadInt64();
            long entryListEndIndex = position + (32 /*< Length of SHA-256 Hash*/ + sizeof(long) + sizeof(long) + sizeof(bool));
            bwStream.BaseStream.Position = 32;
            bwStream.Write(entryListEndIndex);

            // We're inserting data, so we need to store a copy of the data that comes after this first...
            brStream.BaseStream.Position = position;
            byte[] remainderBytes = brStream.ReadBytes((int)(brStream.BaseStream.Length - position));


            
            bwStream.BaseStream.Position = position;

            long indexOffset = archFStream.Length + 32 + sizeof(long) + sizeof(long) + sizeof(bool) - entryListEndIndex; // We add the sizes in order to account for the stuff we're about to insert.
            bwStream.Write(sha256.ComputeHash(sha256.ComputeHash(Encoding.ASCII.GetBytes(key)))); // We compute the hash of the hash in order to not store the decryption key in the file.
            bwStream.Write(indexOffset); // We add the sizes in order to account for the stuff we're about to insert.
            bwStream.Write((long)compressedFolderBytes.Length);
            bwStream.Write(compress); // Whether or not the dir is compressed


            // Re-insert the remainder bytes
            bwStream.Write(remainderBytes);

            


            // Write the actual (encrypted) data
            bwStream.BaseStream.Position = bwStream.BaseStream.Length;

            
            bwStream.Write(compressedFolderBytes, 0, compressedFolderBytes.Length);
            //bwStream.Write(XORByteArrays(folderBytes, sha256.ComputeHash(Encoding.ASCII.GetBytes(key))), 0, folderBytes.Length);

            

            // Update the hash
            UpdateHash();

            bwStream.Flush();
        }

        public void ExtractFolder(String key, String destinationPath)
        {
            VerifyHash();

            // First thing we're gonna want to do is create the destination path.
            //Directory.CreateDirectory(destinationPath);


            BinaryReader brStream = new BinaryReader(archFStream);
            SHA256 sha256 = SHA256.Create();

            byte[] hash1 = sha256.ComputeHash(Encoding.ASCII.GetBytes(key));
            byte[] hash2 = sha256.ComputeHash(hash1);

            brStream.BaseStream.Position = 32;
            long entryListEndIndex = brStream.ReadInt64();
            long index = 0;
            long size = 0;
            bool compressed = false;
            for (int i = 40; i < entryListEndIndex; i = i + (32 + sizeof(long) + sizeof(long) + sizeof(bool)))
            {
                brStream.BaseStream.Position = i;
                byte[] entryHash2 = brStream.ReadBytes(32);

                if (Enumerable.SequenceEqual(entryHash2, hash2))
                {
                    index = brStream.ReadInt64() + entryListEndIndex;
                    size = brStream.ReadInt64();
                    compressed = brStream.ReadBoolean();
                    break;
                }
            }
            if (size != 0)
            {

                brStream.BaseStream.Position = index;
                byte[] rawBytes = brStream.ReadBytes((int)size);

                byte[] decompressedBytes = DecompressBytes(rawBytes, compressed);

                byte[] decryptedBytes = XORByteArrays(decompressedBytes, hash1);

                extractFolderBytes(decryptedBytes, 0, 0, destinationPath);

                long extractFolderBytes(byte[] folderBytes, long startIndex, long encasingStartIndex, string folderPath)
                {

                    BinaryReader brStream = new BinaryReader(new MemoryStream(folderBytes));
                    brStream.BaseStream.Position = startIndex;
                    int idListEntryCount = brStream.ReadInt32();
                    for (int i = 0; i < idListEntryCount; i++)
                    {
                        int nameLength = brStream.ReadInt32();
                        brStream.BaseStream.Position += nameLength;
                        brStream.BaseStream.Position += 4; // To account for skipping over the id.
                    }
                    // Now we're to the directory tree. We'll just read it out and use recursion.
                    int folderId = brStream.ReadInt32();
                    int folderNum = brStream.ReadInt32();
                    int fileNum = brStream.ReadInt32();

                    // Don't want to forget to make the folder
                    Directory.CreateDirectory(Path.Combine(folderPath, getEntryName(folderBytes, folderId, encasingStartIndex)));


                    for (int x = 0; x < folderNum; x++)
                    {
                        long folderStart = brStream.BaseStream.Position;
                        long length = extractFolderBytes(folderBytes, folderStart, startIndex, Path.Combine(folderPath, getEntryName(folderBytes, folderId, encasingStartIndex)));
                        brStream.BaseStream.Position += length;
                    }
                    for (int x = 0; x < fileNum; x++)
                    {
                        int itemId = brStream.ReadInt32();
                        int fileLength = brStream.ReadInt32();
                        byte[] file = brStream.ReadBytes(fileLength);

                        // Write the file
                        FileStream fileFS = new FileStream(Path.Combine(folderPath, getEntryName(folderBytes, folderId, encasingStartIndex), getEntryName(folderBytes, itemId, startIndex)), FileMode.OpenOrCreate, FileAccess.Write);
                        BinaryWriter fileBS = new BinaryWriter(fileFS);
                        fileBS.Write(file);

                        fileBS.Close();
                    }

                    return (brStream.BaseStream.Position - startIndex); // We can use this to determine how long the folder is.

                }

                string getEntryName(byte[] folderBytes, int id, long startIndex)
                {
                    if (id == 0)
                    {
                        return (key);
                    }
                    BinaryReader brStream = new BinaryReader(new MemoryStream(folderBytes.Skip((int)startIndex).ToArray()));
                    brStream.BaseStream.Position = 0;
                    int idListEntryCount = brStream.ReadInt32();
                    for (int i = 0; i < idListEntryCount; i++)
                    {
                        int nameLength = brStream.ReadInt32();
                        string name = Encoding.UTF8.GetString(brStream.ReadBytes(nameLength));
                        int entryId = brStream.ReadInt32();
                        if (entryId == id)
                        {
                            return (name);
                        }
                    }
                    return (null);
                }
            }
            else
            {
                Console.WriteLine("Directory not present in archive or entry missing.");
            }
        }

        public void Flush()
        {
            archFStream.Flush();
        }

        public void VerifyHash()
        {
            BinaryReader brStream = new BinaryReader(archFStream);
            SHA256 sha256 = SHA256.Create();

            brStream.BaseStream.Position = 0;
            byte[] storedHash = brStream.ReadBytes(32);

            brStream.BaseStream.Position = 32;
            byte[] data = brStream.ReadBytes((int)brStream.BaseStream.Length - 32);

            byte[] realHash = sha256.ComputeHash(data);
            if (!Enumerable.SequenceEqual(realHash, storedHash))
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Someone messed with this file and didn't do it right!");
                Console.WriteLine("Continue?");
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                
                // In future, migrate boolean response stuff to CommandLineManager
                string response = Console.ReadLine();
                if (response.ToLower() == "yes" || response.ToLower() == "y")
                {
                    Console.WriteLine("Ok...");
                }
                else if (response.ToLower() == "no" || response.ToLower() == "n")
                {
                    Console.WriteLine("Aborting...");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Uhh I'll take that as a no...");
                    Environment.Exit(0);
                }
            }
        }

        private byte[] GetFolderBytes(String folderName)
        {
            /*
             * Okay, so this is how folder binary is going to be formatted:
             * Int32: Entries in ID List (Aka, number of folders & files)
             * 
             * ID List (Repeats per each folder/file):
             * Int32: Number of bytes in folder/file name
             * Next Few Bytes: Folder/file name in UTF-8
             * Int32: ID for folder/file
             * 
             * 
             * Directory Tree (For any folder this is what it looks like):
             * Int32: ID
             * Int32: Number of folders
             * Int32: Number of files
             * Below this is all the folder entries inside this, then all the file definitions inside this.
             * 
             * File Definition (For any file):
             * Int32: ID
             * Int32: Length of file binary
             * Rest of Bytes: The file binary
             */
            int idCounter = 0;
            byte[] folderBytes = getFolderBytes(folderName, ref idCounter).ToArray();


            List<byte> getFolderBytes(string dirPath, ref int idCounter)
            {
                List<byte> dirTree = new List<byte>();

                List<byte> folderEntries = new List<byte>();
                List<byte> fileEntries = new List<byte>();

                List<byte> idEntries = new List<byte>();


                string[] directories = Directory.GetDirectories(dirPath, "", SearchOption.TopDirectoryOnly);
                string[] files = Directory.GetFiles(dirPath, "", SearchOption.TopDirectoryOnly);

                dirTree.AddRange(BitConverter.GetBytes(idCounter));
                dirTree.AddRange(BitConverter.GetBytes(directories.Length));
                dirTree.AddRange(BitConverter.GetBytes(files.Length));


                idCounter += 1;


                foreach (string path in directories)
                {

                    idEntries.AddRange(getIdEntry(path, idCounter));

                    folderEntries.AddRange(getFolderBytes(path, ref idCounter));

                }
                foreach (string path in files)
                {

                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    BinaryReader brStream = new BinaryReader(fileStream);
                    brStream.BaseStream.Position = 0;

                    byte[] fileBytes = brStream.ReadBytes((int)brStream.BaseStream.Length);

                    fileEntries.AddRange(BitConverter.GetBytes(idCounter));
                    fileEntries.AddRange(BitConverter.GetBytes(fileBytes.Length));
                    fileEntries.AddRange(fileBytes);
                    idEntries.AddRange(getIdEntry(path, idCounter));

                    idCounter += 1;

                }

                dirTree.AddRange(folderEntries);
                dirTree.AddRange(fileEntries);
                dirTree.InsertRange(0, idEntries);
                dirTree.InsertRange(0, BitConverter.GetBytes(directories.Length + files.Length));


                return (dirTree);
            }

            List<byte> getIdEntry(string path, int id)
            {
                List<byte> idEntry = new List<byte>();
                // I don't feel like naming variables so it's all inline. All it does is find the actual file/foldername from the path
                string entryName = path.Split("/")[path.Split("/").Length - 1].Split("\\")[path.Split("/")[path.Split("/").Length - 1].Split("\\").Length - 1];

                byte[] entryNameByteArray = Encoding.UTF8.GetBytes(entryName);
                idEntry.AddRange(BitConverter.GetBytes(entryNameByteArray.Length));
                idEntry.AddRange(entryNameByteArray);
                idEntry.AddRange(BitConverter.GetBytes(id));

                return (idEntry);
            }

            return (folderBytes);
        }

        private byte[] XORByteArrays(byte[] arr1, byte[] arr2)
        {
            byte[] returnBytes = new byte[arr1.Length];

            int arr1Index = 0;
            int arr2Index = 0;
            foreach (byte b in arr1)
            {
                returnBytes[arr1Index] = (byte)(b ^ arr2[arr2Index]);
                arr1Index += 1;
                if (arr2Index + 1 == arr2.Length)
                {
                    arr2Index = 0;
                }
                else
                {
                    arr2Index += 1;
                }
            }

            return (returnBytes);
        }

        private void UpdateHash()
        {
            BinaryWriter bwStream = new BinaryWriter(archFStream);

            SHA256 sha256 = SHA256.Create();

            BinaryReader brStream = new BinaryReader(archFStream);
            brStream.BaseStream.Position = 32;
            byte[] data = brStream.ReadBytes((int)brStream.BaseStream.Length - 32);


            byte[] hash = sha256.ComputeHash(data);
            bwStream.BaseStream.Position = 0;
            bwStream.Write(hash);
        }

        private byte[] CompressBytes(byte[] uncompressedBytes, bool compress, CompressionLevel compressionLevel)
        {
            if (compress)
            {
                MemoryStream memoryStream = new MemoryStream();
                GZipStream gZipStream = new GZipStream(memoryStream, compressionLevel);
                gZipStream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                gZipStream.Flush();
                memoryStream.Flush();
                byte[] compressedBytes = memoryStream.ToArray();
                gZipStream.Close();
                memoryStream.Close();
                return (compressedBytes);
            }
            else // This is just in case we don't actually want to compress.
            {
                return (uncompressedBytes);
            }
        }
        private byte[] DecompressBytes(byte[] compressedBytes, bool decompress)
        {
            if (decompress)
            {
                MemoryStream memoryStream = new MemoryStream(compressedBytes);
                GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);

                List<byte> ByteListUncompressedData = new List<byte>();

                int bytesRead = gZipStream.ReadByte();
                while (bytesRead != -1)
                {
                    ByteListUncompressedData.Add((byte)bytesRead);
                    bytesRead = gZipStream.ReadByte();
                }
                gZipStream.Flush();
                memoryStream.Flush();
                gZipStream.Close();
                memoryStream.Close();
                return (ByteListUncompressedData.ToArray());
            }
            else // Just in case the data isn't compressed after all
            {
                return (compressedBytes);
            }
        }
    }
}
