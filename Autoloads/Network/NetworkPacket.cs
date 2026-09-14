using Godot;

public struct NetworkPacket
{
	#region State

	public byte[] Data;
	public int From;
	public int Channel;
	public MultiplayerPeer.TransferModeEnum Mode;

	#endregion
}
