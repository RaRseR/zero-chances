using System;
using System.Text;

public class MessageReader
{
	#region Constants

	private const int MAX_TEXT_BYTES = 64 * 1024;
	private const int MAX_VAR_BYTES = 5;

	#endregion


	#region State

	private readonly byte[] Data;
	private int Position;

	public bool Failed { get; private set; }

	#endregion


	#region Construction

	public MessageReader(byte[] data)
	{
		Data = data ?? Array.Empty<byte>();
		Position = 0;
		Failed = false;
	}

	#endregion


	#region Access

	public int Remaining => Data.Length - Position;

	public bool AtEnd => Position >= Data.Length;

	#endregion


	#region Primitives

	public byte Byte()
	{
		if (!Take(1))
		{
			return 0;
		}

		return Data[Position++];
	}


	public bool Flag()
	{
		return Byte() != 0;
	}


	public short Short()
	{
		return (short)UShort();
	}


	public ushort UShort()
	{
		if (!Take(2))
		{
			return 0;
		}

		ushort value = (ushort)(Data[Position] | (Data[Position + 1] << 8));

		Position += 2;

		return value;
	}


	public int Int()
	{
		return (int)UInt();
	}


	public uint UInt()
	{
		if (!Take(4))
		{
			return 0;
		}

		uint value = Data[Position]
			| ((uint)Data[Position + 1] << 8)
			| ((uint)Data[Position + 2] << 16)
			| ((uint)Data[Position + 3] << 24);

		Position += 4;

		return value;
	}


	public long Long()
	{
		return (long)ULong();
	}


	public ulong ULong()
	{
		if (!Take(8))
		{
			return 0;
		}

		ulong value = 0;

		for (int index = 0; index < 8; index++)
		{
			value |= (ulong)Data[Position + index] << (index * 8);
		}

		Position += 8;

		return value;
	}


	public float Float()
	{
		return BitConverter.UInt32BitsToSingle(UInt());
	}

	#endregion


	#region Variable length

	public int Var()
	{
		uint raw = VarUnsigned();

		return (int)(raw >> 1) ^ -(int)(raw & 1);
	}


	public uint VarUnsigned()
	{
		uint value = 0;
		int shift = 0;

		for (int index = 0; index < MAX_VAR_BYTES; index++)
		{
			if (!Take(1))
			{
				return 0;
			}

			byte piece = Data[Position++];

			value |= (uint)(piece & 0x7F) << shift;

			if ((piece & 0x80) == 0)
			{
				return value;
			}

			shift += 7;
		}

		Fail();

		return 0;
	}

	#endregion


	#region Composites

	public string Text()
	{
		uint count = VarUnsigned();

		if (Failed || count == 0)
		{
			return string.Empty;
		}

		if (count > MAX_TEXT_BYTES || !Take((int)count))
		{
			Fail();
			return string.Empty;
		}

		string value = Encoding.UTF8.GetString(Data, Position, (int)count);

		Position += (int)count;

		return value;
	}


	public byte[] Blob()
	{
		uint count = VarUnsigned();

		if (Failed || count == 0)
		{
			return Array.Empty<byte>();
		}

		if (!Take((int)count))
		{
			Fail();
			return Array.Empty<byte>();
		}

		byte[] value = new byte[count];

		Buffer.BlockCopy(Data, Position, value, 0, (int)count);

		Position += (int)count;

		return value;
	}

	#endregion


	#region Guards

	private bool Take(int count)
	{
		if (Failed || Position + count > Data.Length)
		{
			Fail();
			return false;
		}

		return true;
	}


	private void Fail()
	{
		Failed = true;
	}

	#endregion
}
