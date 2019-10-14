using Godot;
using Immersion.Voxel;
using System;

public class SetChunkMesh : MeshInstance
{
	public override void _Ready()
	{
		var rnd = new Random();
		var chunk = new ChunkVoxelStorage();
		for (var x = 0; x < 16; x++)
		for (var y = 0; y < 16; y++)
		for (var z = 0; z < 16; z++)
			chunk[x, y, z] = (rnd.Next(4) == 0) ? (byte)1 : (byte)0;
		
		var mesh  = new ChunkMeshGenerator().Generate(chunk);
		var shape = mesh.CreateTrimeshShape();
		
		var body = new StaticBody();
		body.AddChild(new CollisionShape { Shape = shape });
		AddChild(body);
		
		Mesh = mesh;
	}
}
