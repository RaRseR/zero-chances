using System;
using System.Text;

public class MessageWriter
{
	#region Constants

	private const int INITIAL_CAPACITY = 256;
	private const int MIN_CAPACITY = 16;

	#endregion


	#region State

	private byte[] Data;
	private int Length;

	#endregion


	#region Construction

	public MessageWriter() : this(INITIAL_CAPACITY)
	{
	}


	public MessageWriter(int capacity)
	{
		Data = new byte[Math.Max(capacity, MIN_CAPACITY)];
		Length = 0;
	}

	#endregion


	#region Primitives

	public void Byte(byte value)
	{
		Reserve(1);

		Data[Length++] = value;
	}


	public void Flag(bool value)
	{
		Byte(value ? (byte)1 : (byte)0);
	}


	public void Short(short value)
	{
		UShort((ushort)value);
	}


	public void UShort(ushort value)
	{
		Reserve(2);

		Data[Length++] = (byte)value;
		Data[Length++] = (byte)(value >> 8);
	}


	public void Int(int value)
	{
		UInt((uint)value);
	}


	public void UInt(uint value)
	{
		Reserve(4);

		Data[Length++] = (byte)value;
		Data[Length++] = (byte)(value >> 8);
		Data[Length++] = (byte)(value >> 16);
		Data[Length++] = (byte)(value >> 24);
	}


	public void Long(long value)
	{
		ULong((ulong)value);
	}


	public void ULong(ulong value)
	{
		Reserve(8);

		for (int shift = 0; shift < 64; shift += 8)
		{
			Data[Length++] = (byte)(value >> shift);
		}
	}


	public void Float(float value)
	{
		UInt(BitConverter.SingleToUInt32Bits(value));
	}

	#endregion


	#region Variable length

	public void Var(int value)
	{
		VarUnsigned((uint)((value << 1) ^ (value >> 31)));
	}


	public void VarUnsigned(uint value)
	{
		while (value >= 0x80)
		{
			Byte((byte)(value | 0x80));

			value >>= 7;
		}

		Byte((byte)value);
	}

	#endregion


	#region Composites

	public void Text(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			VarUnsigned(0);
			return;
		}

		byte[] utf8 = Encoding.UTF8.GetBytes(value);

		VarUnsigned((uint)utf8.Length);
		Raw(utf8);
	}


	public void Blob(byte[] value)
	{
		if (value == null || value.Length == 0)
		{
			VarUnsigned(0);
			return;
		}

		VarUnsigned((uint)value.Length);
		Raw(value);
	}


	public void Raw(byte[] value)
	{
		if (value == null || value.Length == 0)
		{
			return;
		}

		Reserve(value.Length);

		Buffer.BlockCopy(value, 0, Data, Length, value.Length);

		Length += value.Length;
	}

	#endregion


	#region Result

	public int Size => Length;


	public byte[] ToArray()
	{
		byte[] result = new byte[Length];

		Buffer.BlockCopy(Data, 0, result, 0, Length);

		return result;
	}


	public void Reset()
	{
		Length = 0;
	}

	#endregion


	#region Growth

	private void Reserve(int count)
	{
		if (Length + count <= Data.Length)
		{
			return;
		}

		int capacity = Data.Length;

		while (capacity < Length + count)
		{
			capacity *= 2;
		}

		byte[] grown = new byte[capacity];

		Buffer.BlockCopy(Data, 0, grown, 0, Length);

		Data = grown;
	}

	#endregion
}
