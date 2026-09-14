using Godot;

public enum CollisionMode
{
	None,
	Trimesh,
	Convex
}

public partial class WorldBody : StaticBody3D
{
	public MeshInstance3D MeshInstance { get; private set; }
	public CollisionShape3D CollisionShape { get; private set; }
	
	private ArrayMesh[] Lods;
	private int CurrentLod = -1;


	public WorldBody(
		ArrayMesh[] lods,
		Material material,
		CollisionMode collisionMode
	)
	{
		Lods = lods;
		
		MeshInstance = new();
		
		SetMaterial(material);
		
		AddChild(MeshInstance);
		
		SetLod(0);
		
		BuildCollision(collisionMode);
	}
	
	
	public void SetLod(int lod)
	{
		if (Lods == null || Lods.Length == 0)
		{
			return;
		}
		
		int newLod = Mathf.Clamp(lod, 0, Lods.Length - 1);
		
		if (newLod == CurrentLod)
		{
			return;
		}
		
		CurrentLod = newLod;
		
		MeshInstance.Mesh = Lods[newLod];
	}
	
	
	public void SetMaterial(Material material)
	{
		MeshInstance.MaterialOverride = material;
	}
	
	
	private void BuildCollision(CollisionMode mode)
	{
		if (mode == CollisionMode.None)
		{
			return;
		}
		if (Lods == null || Lods.Length == 0) 
		{
			return;
		}
		
		Shape3D shape = mode == CollisionMode.Convex
			? Lods[0].CreateConvexShape()
			: Lods[0].CreateTrimeshShape();
		
		if (shape == null)
		{
			return;
		}
		
		CollisionShape = new();
		CollisionShape.Shape = shape;
		AddChild(CollisionShape);
	}
}
