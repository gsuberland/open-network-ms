<Query Kind="Program">
  <Reference Relative="..\..\CSharp-Playground\LzssAlgorithmImplementation\LzssAlgorithm\bin\Release\LzssAlgorithm.dll">C:\Users\Graham\source\repos\CSharp-Playground\LzssAlgorithmImplementation\LzssAlgorithm\bin\Release\LzssAlgorithm.dll</Reference>
</Query>

[Flags]
enum HeaderFlags : UInt32
{
	BL_WRITE_TO_FLASH = 1,
	BL_LZSS_COMPRESSED = 2,
	BL_LZSS2_COMPRESSED = 4,
	BL_EXECUTE_FROM_ROM = 8,
	BL_BYPASS_CRC_CHECK = 16,
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

void Main()
{
	const int MinHeaderSize = 36;
	
	string inputFile = @"C:\Users\Graham\source\repos\open-network-ms\docs\network-ms-firmware\NetworkMS_KB.bin";
	string outputFile = @"C:\Users\Graham\source\repos\open-network-ms\docs\network-ms-firmware\NetworkMS_KB_PAYLOAD.bin";
	
	using (var fs = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
	using (var brBE = new EndiannessAwareBinaryReader(fs, EndiannessAwareBinaryReader.Endianness.Big))
	using (var brLE = new EndiannessAwareBinaryReader(fs, EndiannessAwareBinaryReader.Endianness.Little))
	{
		if (fs.Length < MinHeaderSize)
		{
			Console.WriteLine("Invalid NET-OS image: file is too small to contain NET-OS header.");
			return;
		}
		
		
		uint headerSize = brBE.ReadUInt32();
		if (headerSize > fs.Length)
		{
			Console.WriteLine("Invalid NET-OS image: header size is too large.");
			return;
		}
		if (headerSize < MinHeaderSize)
		{
			Console.WriteLine("Invalid NET-OS image: header size is too small.");
			return;
		}
		Console.WriteLine($"Complete header size: 0x{headerSize:X2} ({headerSize} bytes)");


		uint netOsHeaderSize = brBE.ReadUInt32();
		if (netOsHeaderSize > headerSize)
		{
			Console.WriteLine("Invalid NET-OS image: NET-OS header size is too large.");
			return;
		}
		if (netOsHeaderSize < MinHeaderSize)
		{
			Console.WriteLine("Invalid NET-OS image: NET-OS header size is too small.");
			return;
		}
		Console.WriteLine($"NET-OS header size: 0x{netOsHeaderSize:X2} ({netOsHeaderSize} bytes)");


		byte[] signatureBytes = brBE.ReadBytes(8);
		string signature = Encoding.ASCII.GetString(signatureBytes, 0, 7);
		if (signatureBytes[7] != 0)
		{
			Console.WriteLine("Invalid NET-OS image: bootHdr signature missing.");
			return;
		}
		if (signature != "bootHdr")
		{
			Console.WriteLine("Warning: bootHdr signature does not match expected; this might be something custom.");
		}
		Console.WriteLine($"NET-OS signature: {signature}");
		
		
		uint version = brBE.ReadUInt32();
		if (version == 0)
		{
			Console.WriteLine("NET-OS version: <7.4");
		}
		else if (version == 1)
		{
			Console.WriteLine("NET-OS version: >=7.4");
		}
		else
		{
			Console.WriteLine($"NET-OS version: Unknown ({version})");
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
		Console.WriteLine("Flags: " + setFlags);


		uint flashAddress = brBE.ReadUInt32();
		Console.WriteLine($"Flash address: 0x{flashAddress:X8}");


		uint ramAddress = brBE.ReadUInt32();
		Console.WriteLine($"RAM address: 0x{ramAddress:X8}");


		uint size = brBE.ReadUInt32();
		Console.WriteLine($"Image size: 0x{size:X8}");
		if (size == 0)
		{
			Console.WriteLine("Warning: image size is zero.");
		}
		if (size > fs.Length)
		{
			Console.WriteLine("Warning: image size exceeds file length.");
		}
		if (UInt32.MaxValue - size <= headerSize)
		{
			Console.WriteLine("Warning: image size exceeds file length.");
		}

		fs.Seek(headerSize, SeekOrigin.Begin);

		/*uint backupAddress = brBE.ReadUInt32();
		Console.WriteLine($"Backup address: 0x{backupAddress:X8}");*/
		
		byte[] data = new byte[size];
		fs.Read(data, 0, (int)size);
		File.WriteAllBytes(outputFile, data);
	}
}

// Define other methods and classes here
