<Query Kind="Program">
  <Reference Relative="..\..\CSharp-Playground\LzssAlgorithmImplementation\LzssAlgorithm\bin\Release\LzssAlgorithm.dll">C:\Users\Graham\source\repos\CSharp-Playground\LzssAlgorithmImplementation\LzssAlgorithm\bin\Release\LzssAlgorithm.dll</Reference>
</Query>


[Flags]
enum HeaderFlags : UInt32
{
	BL_WRITE_TO_FLASH = 1 << 0,
	BL_LZSS_COMPRESSED_MAYBE = 1 << 1,
	BL_EXECUTE_FROM_ROM = 1 << 2,
	BL_LZSS2_COMPRESSED = 1 << 3,
	BL_BYPASS_CRC_CHECK = 1 << 4,
	BL_BYPASS_IMGLEN_CHECK = 1 << 5,
}

enum PlatformProcessor : UInt32
{
	NS7520 = 2,
	NS9750 = 3,
	NS9360 = 4,
	NS9215 = 5,
	NS9210 = 6,
}

enum DigiBoardType : UInt32
{
	CONNECTCORE7U = 1,
	CONNECTCORE9C = 2,
	CONNECTCORE9P9215 = 3,
	CONNECTCORE9P9360 = 4,
	CONNECTCOREWI9C = 5,
	CONNECTCOREWI9P9215 = 7,
	CONNECTEM = 8,
	CONNECTME = 9,
	CONNECTME9210 = 10,
	CONNECTSP = 12,
	CONNECTWIEM = 13,
	CONNECTWIEM9210 = 14,
	CONNECTWIME = 17,
	CONNECTWISP = 18,
	NS7520 = 12,
	NS9210 = 13,
	NS9360 = 14,
	NS9360_ENG = 15,
	NS9750 = 17,
	CONNECTWIME9210 = 18,
}

// https://stackoverflow.com/a/58341527/978756
public class EndiannessAwareBinaryReader : BinaryReader
{
	public enum Endianness
	{
		Little,
		Big,
	}

	private readonly Endianness _endianness = Endianness.Little;

	public EndiannessAwareBinaryReader(Stream input) : base(input)
	{
	}

	public EndiannessAwareBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
	{
	}

	public EndiannessAwareBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
	{
	}

	public EndiannessAwareBinaryReader(Stream input, Endianness endianness) : base(input)
	{
		_endianness = endianness;
	}

	public EndiannessAwareBinaryReader(Stream input, Encoding encoding, Endianness endianness) : base(input, encoding)
	{
		_endianness = endianness;
	}

	public EndiannessAwareBinaryReader(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness) : base(input, encoding, leaveOpen)
	{
		_endianness = endianness;
	}

	public override short ReadInt16() => ReadInt16(_endianness);

	public override int ReadInt32() => ReadInt32(_endianness);

	public override long ReadInt64() => ReadInt64(_endianness);

	public override ushort ReadUInt16() => ReadUInt16(_endianness);

	public override uint ReadUInt32() => ReadUInt32(_endianness);

	public override ulong ReadUInt64() => ReadUInt64(_endianness);

	public short ReadInt16(Endianness endianness) => BitConverter.ToInt16(ReadForEndianness(sizeof(short), endianness), 0);

	public int ReadInt32(Endianness endianness) => BitConverter.ToInt32(ReadForEndianness(sizeof(int), endianness), 0);

	public long ReadInt64(Endianness endianness) => BitConverter.ToInt64(ReadForEndianness(sizeof(long), endianness), 0);

	public ushort ReadUInt16(Endianness endianness) => BitConverter.ToUInt16(ReadForEndianness(sizeof(ushort), endianness), 0);

	public uint ReadUInt32(Endianness endianness) => BitConverter.ToUInt32(ReadForEndianness(sizeof(uint), endianness), 0);

	public ulong ReadUInt64(Endianness endianness) => BitConverter.ToUInt64(ReadForEndianness(sizeof(ulong), endianness), 0);

	private byte[] ReadForEndianness(int bytesToRead, Endianness endianness)
	{
		var bytesRead = ReadBytes(bytesToRead);

		if ((endianness == Endianness.Little && !BitConverter.IsLittleEndian)
			|| (endianness == Endianness.Big && BitConverter.IsLittleEndian))
		{
			Array.Reverse(bytesRead);
		}

		return bytesRead;
	}
}

void DecompressLZSS2(Stream input, uint length, Stream output)
{
	const int N = 4096;
	const int F = 18;
	const int Threshold = 2;
	const int DecompressBufferSize = (N + F - 1);
	
	var decompressBuffer = new byte[DecompressBufferSize];
	
	int i = 0;
	int j = 0;
	int r = 0;
	int c = 0;
	uint flags = 0;
	
	for (int n = 0; n < decompressBuffer.Length; n++)
	{
		decompressBuffer[n] = (byte)' ';
	}
	
	r = N - F;
	for (int n = 0; n < length; n++)
	{
		flags >>= 1;
		if ((flags & 256) == 0)
		{
			c = input.ReadByte();
			if (c < 0)
				break;
			flags = (uint)c | 0xFF00u;
		}
		if ((flags & 1) != 0)
		{
			c = input.ReadByte();
			if (c < 0)
				break;
			
			output.WriteByte((byte)c);
			decompressBuffer[r++] = (byte)c;
			r &= (N - 1);
		}
		else
		{
			i = input.ReadByte();
			if (i < 0)
				break;
			j = input.ReadByte();
			if (j < 0)
				break;
			i |= ((j & 0xF0) << 4);
			j = (j & 0x0F) + Threshold;
			for (int k = 0; k <= j; k++)
			{
				int b = (i + k) & (N - 1);
				output.WriteByte(decompressBuffer[b]);
				decompressBuffer[r++] = decompressBuffer[b];
				r &= (N - 1);
			}
		}
	}
}


void PrintLog(StringBuilder sb, string message)
{
	sb.AppendLine(message);
	Console.WriteLine(message);
}


void ProcessFirmwareFile(string inputFile, string outputFile, out StringBuilder log)
{
	const int MinHeaderSize = 36;
	const int WarnHeaderSize = 0x1000;

	const int ExpectedPre74HeaderSize = 36;
	const int ExpectedPost74HeaderSize = 92;
	
	log = new StringBuilder();

	using (var fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
	using (var brBE = new EndiannessAwareBinaryReader(fs, EndiannessAwareBinaryReader.Endianness.Big))
	using (var brLE = new EndiannessAwareBinaryReader(fs, EndiannessAwareBinaryReader.Endianness.Little))
	{
		PrintLog(log, $"File size: {fs.Length} (0x{fs.Length:X8}) bytes");

		if (fs.Length < MinHeaderSize)
		{
			PrintLog(log, "Invalid NET-OS image: file is too small to contain NET-OS header.");
			return;
		}


		uint headerSize = brBE.ReadUInt32();
		PrintLog(log, $"Complete header size: 0x{headerSize:X2} ({headerSize} bytes)");
		if (headerSize > fs.Length)
		{
			PrintLog(log, "Invalid NET-OS image: header size is too large.");
			return;
		}
		if (headerSize < MinHeaderSize)
		{
			PrintLog(log, "Invalid NET-OS image: header size is too small.");
			return;
		}


		uint netOsHeaderSize = brBE.ReadUInt32();
		PrintLog(log, $"NET-OS header size: 0x{netOsHeaderSize:X2} ({netOsHeaderSize} bytes)");
		if (netOsHeaderSize > headerSize)
		{
			PrintLog(log, "Invalid NET-OS image: NET-OS header size is too large.");
			return;
		}
		if (netOsHeaderSize < MinHeaderSize)
		{
			PrintLog(log, "Invalid NET-OS image: NET-OS header size is too small.");
			return;
		}
		if (netOsHeaderSize > WarnHeaderSize)
		{
			PrintLog(log, "Warning: NET-OS header size is unexpectedly large.");
		}
		if (headerSize != netOsHeaderSize)
		{
			PrintLog(log, $"Warning: this file uses a custom header. Custom header data size: 0x{headerSize - netOsHeaderSize:X}");
		}


		byte[] signatureBytes = brBE.ReadBytes(8);
		string signature = Encoding.ASCII.GetString(signatureBytes, 0, 8).TrimEnd('\0');
		PrintLog(log, $"NET-OS signature: {signature}");
		if (signature != "bootHdr")
		{
			PrintLog(log, "Warning: signature is not default 'bootHdr', loader may be customised.");
		}


		uint version = brBE.ReadUInt32();
		if (version < 0x0704)
		{
			PrintLog(log, $"NET-OS version: <7.4 (0x{version:X4})");
		}
		else
		{
			if (version > 0xFFFF)
			{
				PrintLog(log, $"NET-OS version: Unknown (0x{version:X8})");
			}
			else
			{
				uint major = (version >> 8) & 0xFF;
				uint minor = version & 0xFF;
				PrintLog(log, $"NET-OS version: {major}.{minor}");
			}
		}


		if (version < 0x0704 && netOsHeaderSize != ExpectedPre74HeaderSize)
		{
			PrintLog(log, $"Warning: NET-OS header size {netOsHeaderSize} does not match expected size {ExpectedPre74HeaderSize} for this version. Results may be unreliable.");
		}
		if (version >= 0x0704 && netOsHeaderSize != ExpectedPost74HeaderSize)
		{
			PrintLog(log, $"Warning: NET-OS header size {netOsHeaderSize} does not match expected size {ExpectedPost74HeaderSize} for this version. Results may be unreliable.");
		}


		uint flags = brBE.ReadUInt32();
		string setFlags = "";
		for (int b = 0; b < 32; b++)
		{
			uint flagVal = 1u << b;
			if ((flags & flagVal) != 0)
			{
				if (Enum.IsDefined(typeof(HeaderFlags), flagVal))
				{
					setFlags += Enum.GetName(typeof(HeaderFlags), flagVal) + ", ";
				}
				else
				{
					setFlags += $"UNKNOWN_FLAG_{flagVal:X8}, ";
				}
			}
		}
		setFlags = setFlags.TrimEnd(',', ' ');
		PrintLog(log, "Flags: " + setFlags);
		var headerFlags = (HeaderFlags)flags;


		uint flashAddress = brBE.ReadUInt32();
		PrintLog(log, $"Flash address: 0x{flashAddress:X8}");


		uint ramAddress = brBE.ReadUInt32();
		PrintLog(log, $"RAM address: 0x{ramAddress:X8}");


		uint size = brBE.ReadUInt32();
		PrintLog(log, $"Image size: 0x{size:X8}");
		if (size == 0)
		{
			PrintLog(log, "Warning: image size is zero.");
		}
		if (size > fs.Length)
		{
			PrintLog(log, "Warning: image size exceeds file length.");
		}
		if (UInt32.MaxValue - size <= headerSize)
		{
			PrintLog(log, "Warning: image size exceeds file length.");
		}


		if (version >= 0x0704 && netOsHeaderSize >= ExpectedPost74HeaderSize)
		{
			uint backupAddress = brBE.ReadUInt32();
			PrintLog(log, $"Backup address: 0x{backupAddress:X8}");


			uint platformCount = brBE.ReadUInt32();
			PrintLog(log, $"Platform count: {platformCount}");
			if (platformCount > 8)
			{
				PrintLog(log, "Warning: more than 8 platform counts have been specified. This is unexpected. For safety, a limit of 8 has been applied.");
				platformCount = 8;
			}
			else if (platformCount == 0)
			{
				PrintLog(log, "Warning: no platforms have been specified. This firmware image will not boot unless the loader has been modified.");
			}


			for (int i = 0; i < platformCount; i++)
			{
				uint platform = brBE.ReadUInt32();
				uint minimumRAM = platform & 0xFFF;
				uint hardwareRevision = (platform >> 12) & 0xF;
				uint processor = (platform >> 16) & 0xFF;
				uint boardType = (platform >> 24) & 0xFF;

				PrintLog(log, $"Platform {i + 1}:");
				PrintLog(log, $"    (raw value): 0x{platform:X8}");
				PrintLog(log, $"    Minimum RAM: {minimumRAM}MB");
				PrintLog(log, $"    Hardware Revision: {hardwareRevision} ({(char)('A' + hardwareRevision)})");
				string processorName;
				if (Enum.IsDefined(typeof(PlatformProcessor), processor))
				{
					processorName = $"{Enum.GetName(typeof(PlatformProcessor), processor)} (0x{processor:X2})";
				}
				else
				{
					processorName = $"Unknown (0x{processor:X2})";
				}
				PrintLog(log, $"    Processor: {processorName}");

				string boardName;
				if ((boardType & 0x80) == 0)
				{
					boardName = $"Unknown non-Digi board (0x{boardType:X2})";
				}
				else
				{
					uint digiBoardType = boardType & ~0x80u;
					if (Enum.IsDefined(typeof(DigiBoardType), digiBoardType))
					{
						boardName = $"{Enum.GetName(typeof(DigiBoardType), digiBoardType)} (0x{digiBoardType:X2})";
					}
					else
					{
						boardName = $"Unknown Digi board (0x{digiBoardType:X2})";
					}
				}
				PrintLog(log, $"    Board Type: {boardName}");
			}


			// skip the remaining entries
			if (platformCount < 8)
			{
				fs.Seek((8 - platformCount) * 4, SeekOrigin.Current);
			}

			PrintLog(log, $"Reserved1: 0x{brBE.ReadUInt32():X8}");
			PrintLog(log, $"Reserved2: 0x{brBE.ReadUInt32():X8}");
			PrintLog(log, $"Reserved3: 0x{brBE.ReadUInt32():X8}");
			PrintLog(log, $"Reserved4: 0x{brBE.ReadUInt32():X8}");
		}


		PrintLog(log, $"Seeking to data at offset 0x{headerSize}");
		fs.Seek(headerSize, SeekOrigin.Begin);


		if (headerFlags.HasFlag(HeaderFlags.BL_LZSS2_COMPRESSED))
		{
			PrintLog(log, "File is LZSS2 compressed. Decompressing...");
			using (var output = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
			{
				DecompressLZSS2(fs, size, output);
				PrintLog(log, $"Expanded {fs.Length} bytes to {output.Length} bytes.");
			}
		}
		else
		{
			PrintLog(log, "File is not LZSS2 compressed, dumping raw.");
			byte[] data = new byte[size];
			fs.Read(data, 0, (int)size);
			File.WriteAllBytes(outputFile, data);
		}
	}
}

void Main()
{
	var inputDirectory = @"C:\Users\Graham\source\repos\open-network-ms\docs\3rd_party\network-ms-firmware\";
	var outputDirectory = @"C:\Users\Graham\source\repos\open-network-ms\docs\3rd_party\network-ms-firmware\processed\";
	
	var inputFiles = Directory.GetFiles(inputDirectory, "*.bin", SearchOption.TopDirectoryOnly);
	var versionRegex = new Regex(@"_([0-9a-zA-Z]{2})\.bin$");
	foreach (string inputFile in inputFiles)
	{
		var inputFileName = Path.GetFileName(inputFile);
		var match = versionRegex.Match(inputFileName);
		if (match.Success)
		{
			string version = match.Groups[1].Value.ToUpper();
			Console.WriteLine($"Processing firmware version {version} from file {inputFileName}");
			
			string outputCopyFileName = "nmc_" + version + ".bin";
			string outputCopy = Path.Combine(outputDirectory, outputCopyFileName);
			File.Copy(inputFile, outputCopy, true);
			
			string outputFileName = "nmc_" + version + "_extracted.bin";
			string outputFile = Path.Combine(outputDirectory, outputFileName);
			StringBuilder log;
			ProcessFirmwareFile(inputFile, outputFile, out log);
			string logFileName = "nmc_" + version + ".log";
			string logFile = Path.Combine(outputDirectory, logFileName);
			File.WriteAllText(logFile, log.ToString());
			Console.WriteLine();
			Console.WriteLine();
		}
	}
}

// Define other methods and classes here