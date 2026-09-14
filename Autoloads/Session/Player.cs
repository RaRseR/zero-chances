public class Player
{
	#region Identity

	public int Slot = -1;
	public int PeerId;
	public ulong SteamId;

	#endregion


	#region State

	public string Name = string.Empty;
	public bool IsHost;
	public bool IsBot;
	public int FactionIndex = -1;
	public bool CanEditWorld;

	#endregion


	#region Access

	public Faction Faction => Factions.Get(FactionIndex);

	public bool IsNetworked => !IsBot && PeerId > 0;

	#endregion
}
