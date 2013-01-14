using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zlib;

namespace MAS2Extract
{
    /// <summary>
    /// This class reads .MAS files for rFactor 2.
    /// Made in-house of course :-)
    /// </summary>
    public class MAS2Reader
    {
        #region Deciphering information
        protected byte[] Salt;
        protected uint Saltkey;
        protected uint Saltkey2;

        protected readonly byte[] FileTypeKeys = new byte[]
                             {
                                 0x42, 0xF8, 0x95, 0x20, 0xDE, 0x5F, 0xC1, 0x10, 0xD9, 0xC8, 0xAE, 0xD0, 0x0F, 0x0D,
                                 0x70, 0xAB
                             };
        protected readonly byte[] FileHeaderKeys = new byte[]
                                      {
                                          0xB8, 0xA8, 0x8B, 0x07, 0x8A, 0x0E, 0xF2, 0x11, 0x68, 0xFB, 0xBC, 0xDB, 0x12,
                                          0xD0, 0xB6, 0xB3, 0x9F, 0x69, 0x55, 0x5F, 0xC7, 0xCA, 0x61, 0xAD, 0x3C, 0x56,
                                          0xC1, 0xDF, 0x46, 0x13, 0x28, 0x1C, 0x4B, 0x20, 0x3B, 0x75, 0xAC, 0xE7, 0x3E,
                                          0x9A, 0xB1, 0x8D, 0xE1, 0x6E, 0x6F, 0x14, 0xC0, 0x88, 0x97, 0x95, 0x6B, 0xB0,
                                          0xD7, 0x64, 0x17, 0x30, 0xA4, 0xCE, 0x66, 0x9E, 0x70, 0x03, 0x1E, 0xEC, 0xE9,
                                          0xDE, 0x5A, 0xB2, 0x90, 0xA2, 0xBA, 0x4A, 0x2A, 0xF8, 0x6D, 0xE6, 0x23, 0x59,
                                          0x7A, 0xEE, 0xF6, 0xDA, 0x3A, 0x60, 0xB5, 0xFE, 0x06, 0x2B, 0xEA, 0xD4, 0xE3,
                                          0x72, 0x62, 0x27, 0xA1, 0x4C, 0x0B, 0x3D, 0x8F, 0x34, 0xEB, 0x15, 0x18, 0x39,
                                          0xC5, 0x22, 0x58, 0x94, 0xF5, 0x42, 0xA3, 0xC2, 0x25, 0x87, 0x81, 0x48, 0x93,
                                          0x02, 0xD6, 0xC9, 0x71, 0x7D, 0x35, 0xBD, 0xD3, 0x09, 0x9D, 0x7F, 0x53, 0xCD,
                                          0xAE, 0x21, 0x4E, 0x77, 0xE8, 0x43, 0xD1, 0xB9, 0xFF, 0x33, 0xBB, 0xF7, 0x8C,
                                          0xB4, 0xBF, 0xD9, 0xAB, 0x4F, 0x4D, 0xE0, 0x6A, 0xD5, 0xF3, 0xD8, 0xC6, 0x08,
                                          0x83, 0x1D, 0xFA, 0x05, 0x41, 0xCB, 0xD2, 0xF9, 0xC3, 0x84, 0x65, 0x40, 0x49,
                                          0x54, 0xCC, 0xEF, 0x0F, 0xA6, 0xA9, 0xAF, 0x5C, 0x91, 0x1A, 0x36, 0xBE, 0x01,
                                          0x2F, 0x16, 0x1F, 0xA5, 0xFD, 0x45, 0x96, 0x52, 0x79, 0xE5, 0x10, 0x1B, 0x67,
                                          0x63, 0x24, 0x32, 0x80, 0x9C, 0x2E, 0x47, 0x57, 0x2D, 0x8E, 0x7E, 0xE4, 0x78,
                                          0xA0, 0x99, 0xDC, 0xB7, 0xFC, 0x76, 0xF4, 0x19, 0x3F, 0xAA, 0x5D, 0x04, 0x26,
                                          0x7C, 0xF0, 0xF1, 0x37, 0xA7, 0xED, 0x86, 0xDD, 0x98, 0xC4, 0x82, 0x89, 0x31,
                                          0x5B, 0x74, 0x0C, 0x92, 0x0D, 0x6C, 0x38, 0xCF, 0x51, 0x2C, 0x7B, 0x44, 0x50,
                                          0x0A, 0x9B, 0x5E, 0, 0x73, 0x29, 0x85, 0xC8, 0xE2  };
        #endregion

        #region Binary readers/helpers
        protected readonly BinaryReader Reader;
        protected byte[] FileHeader;
        #endregion

        #region MAS2 file info
        protected readonly List<MAS2File> _files = new List<MAS2File>();
        public List<MAS2File> Files { get { return _files; } }
        public int Count { get { return _files.Count; } }

        protected string _File;
        public string File { get { return _File; } }
        #endregion

        /// <summary>
        /// Read the file header containing the file format + 
        /// all internal files
        /// </summary>
        protected void ReadHeader()
        {
            var fileTypeBytes = Reader.ReadBytes(16);
            var fileTypeString = DecodeFileFormatHeader(fileTypeBytes).Replace('\0',' ').Trim();
            if (fileTypeString == "GMOTOR_MAS_2.90")
            {
                Salt = Reader.ReadBytes(8);
                Saltkey = BitConverter.ToUInt32(Salt, 0);
                Saltkey2 = BitConverter.ToUInt32(Salt, 4);
                if (Saltkey < 64) Saltkey += 64;
                if (Saltkey2 < 64) Saltkey2 += 64;

                var garbage = Reader.ReadBytes(120 - 16 - 8);

                // Reader size of file header:
                var bfSize = Reader.ReadInt32();
                var bf = Reader.ReadBytes(bfSize);

                FileHeader = DecodeFilesHeader(bf);
            }
        }

        /// <summary>
        /// Decode the file header data
        /// </summary>
        /// <param name="masHeader"></param>
        /// <returns></returns>
        private string DecodeFileFormatHeader(byte[] masHeader)
        {
            // The header is encoded with a XOR-like compression with 16-byte key.
            var pkgType = masHeader;
            if (pkgType == null || pkgType.Length != 16)
                throw new Exception("Invalid file header");

            for (var i = 0; i < 16; i++)
                pkgType[i] = (byte)(pkgType[i] ^ (FileTypeKeys[i] >> 1));

            return Encoding.ASCII.GetString(pkgType);
        }

        /// <summary>
        /// Decode the header containing all the files of the MAS2 file.
        /// </summary>
        /// <param name="bf"></param>
        /// <returns></returns>
        private byte[] DecodeFilesHeader(byte[] bf)
        {
            // The files header is encoded with a XOR-like compression with 256-byte key.
            // The specific decoding algorithm is in here.
            // The MAS header itself is not parsed

            var output = new byte[bf.Length];

            if (bf.Length > 0)
            {
                uint gigabyteIndex = 0;
                for (var byteIndex = 0; byteIndex < bf.Length; byteIndex++)
                {
                    var ind = (byte)((byteIndex + byteIndex / 256) % 256);
                    var c = (byte)(byteIndex & 0x3F);

                    var value = ((ulong)FileHeaderKeys[ind]) << c;

                    var valueH = value & 0xFFFFFFFF00000000;
                    valueH = valueH >> 32;
                    ulong value_l;
                    value_l = ((ulong)byteIndex) | Saltkey & value;
                    valueH = gigabyteIndex | Saltkey2 & valueH;

                    value = value_l | valueH << 32;

                    output[byteIndex] = (byte)(bf[byteIndex] ^ DecodeFilesHeaderShiftBytes(value, c));

                    gigabyteIndex = (uint)DecodeFilesHeaderShiftBytes((ulong)byteIndex, 32);
                }
            }

            return output;

        }

        /// <summary>
        /// Helper function for decoding file header.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        private static ulong DecodeFilesHeaderShiftBytes(ulong d, byte s)
        {
            if (s > 0x40)
                return 64;
            return d >> s;
        }

        /// <summary>
        /// Constructor; parses MAS2 file for its contents.
        /// Use the ContainsFile & GetFile methods to search for files.
        /// Use the ExctractFile, ExctractString and ExtractBytes methods
        /// for extracting a file.
        /// </summary>
        /// <param name="file"></param>
        public MAS2Reader(string file)
        {
            _File = file;

            Reader = new BinaryReader(System.IO.File.OpenRead(file));
            ReadHeader();

            var files = FileHeader.Length / 256;
            var filePosition = (uint)Reader.BaseStream.Position;

            Reader.Close();

            for (var i = 0; i < files; i++)
            {
                var filename = Encoding.ASCII.GetString(FileHeader, i * 256 + 16, 128);
                filename = filename.Substring(0, filename.IndexOf('\0'));

                var path = Encoding.ASCII.GetString(FileHeader, i * 256 + 16 + filename.Length + 1, 128);
                path = path.Substring(0, path.IndexOf('\0'));

                var fileIndex = BitConverter.ToUInt32(FileHeader, i * 256);
                var sizeCompressed = BitConverter.ToUInt32(FileHeader, 65*4 + i * 256);
                var sizeUncompressed = BitConverter.ToUInt32(FileHeader, 63 * 4 + i * 256);

                var masfile = new MAS2File(fileIndex, filename, path, sizeCompressed, sizeUncompressed,
                                           filePosition);
                _files.Add(masfile);

                filePosition += sizeCompressed;
            }
        }

        #region Simple search methods.
        public bool ContainsFile(string file)
        {
            return _files.Any(x => x.Filename == file);
        }

        public IEnumerable<MAS2File> GetFile(string file)
        {
            return _files.Where(x => x.Filename == file);
        }
        #endregion
        #region Extract files in MAS2File
        public void ExtractFile(MAS2File f, string target)
            {
            var reader = new BinaryReader(System.IO.File.OpenRead(_File));
            reader.BaseStream.Seek(f.FileOffset, SeekOrigin.Begin);
            
            var rawData = reader.ReadBytes((int)f.CompressedSize);

            if (f.IsCompressed)
            {
                var outputData = new byte[f.UncompressedSize];

                // MAS2 compression consists of a simple inflate/deflate action.
                var codec = new ZlibCodec(CompressionMode.Decompress);
                codec.InitializeInflate();
                codec.InputBuffer = rawData;
                codec.NextIn = 0;
                codec.AvailableBytesIn = rawData.Length;

                codec.OutputBuffer = outputData;
                codec.NextOut = 0;
                codec.AvailableBytesOut = outputData.Length;

                codec.Inflate(FlushType.None);
                codec.EndInflate();

                System.IO.File.WriteAllBytes(target, outputData);
            }
            else
            {
                System.IO.File.WriteAllBytes(target, rawData);
            }

        }
        public byte[] ExtractBytes(MAS2File f)
        {
            var reader = new BinaryReader(System.IO.File.OpenRead(this._File));
            reader.BaseStream.Seek(f.FileOffset, SeekOrigin.Begin);
            var rawData = reader.ReadBytes((int)f.CompressedSize);
            reader.Close();

            if (f.IsCompressed)
            {
                var outputData = new byte[f.UncompressedSize];

                // MAS2 compression consists of a simple inflate/deflate action.
                var codec = new ZlibCodec(CompressionMode.Decompress);
                codec.InitializeInflate();
                codec.InputBuffer = rawData;
                codec.NextIn = 0;
                codec.AvailableBytesIn = rawData.Length;

                codec.OutputBuffer = outputData;
                codec.NextOut = 0;
                codec.AvailableBytesOut = outputData.Length;

                codec.Inflate(FlushType.None);
                codec.EndInflate();

                return outputData;
            }
            else
            {
                return rawData;
            }
        }

        public string ExtractString(MAS2File f)
        {
            return Encoding.ASCII.GetString(ExtractBytes(f));
        }
#endregion
        #region Extract functions on index
        public void ExtractFile(int index, string target)
        {
            ExtractFile(_files[index], target);
        }

        public string ExtractString(int index)
        {
            return ExtractString(_files[index]);
        }

        public byte[] ExtractBytes(int index)
        {
            return ExtractBytes(_files[index]);
        }
        #endregion
    }
}