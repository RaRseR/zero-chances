//using Godot;
//using System;
//using System.Collections.Generic;
//
//public enum RegionType
//{
	//Ocean,
	//Lake,
	//Island,
	//Continent
//}
//
//public readonly struct Region
//{
	//public readonly string Title;
	//
	//public readonly RegionType Type;
	//public readonly bool IsBorder;
	//public readonly uint Size;
	//
	//public readonly Vector2 Centroid;
	//public readonly Vector2 MinBoundary;
	//public readonly Vector2 MaxBoundary;
	//
	//public Region(
		//string title, 
		//RegionType type,
		//bool isBorder, 
		//uint size, 
		//Vector2 centroid, 
		//Vector2 minBoundary, 
		//Vector2 maxBoundary
	//)
	//{
		//Title = title;
		//Type = type;
		//IsBorder = isBorder; 
		//Size = size;
		//Centroid = centroid;
		//MinBoundary = minBoundary;
		//MaxBoundary = maxBoundary;
	//}
//}
//
//
//public static class TerrainUtil
//{
	//public static Region[] Generate(
		//float graphWidth,
		//float graphHeight,
		//float[] heights, 
		//Vector2[] positions, 
		//uint[][] neighbours,
		//Vector2[][] polygons
	//)
	//{
		//bool[] border = FindBorders(
			//graphWidth,
			//graphHeight,
			//polygons
		//);
		//
		//Region[] regions = GenerateRegions(
			//graphWidth,
			//graphHeight,
			//heights,
			//positions,
			//neighbours,
			//border
		//);
		//
		//
		//regions = GenerateLakes(
			//regions,
			//heights,
			//positions,
			//neighbours,
			//border
		//);
		//
		//return regions;
	//}
	//
	//private static bool[] FindBorders(
		//float graphWidth,
		//float graphHeight,
		//Vector2[][] polygons
	//)
	//{
		//int siteCount = polygons.Length;
		//
		//bool[] border = new bool[siteCount];
		//
		//for (int siteIndex = 0; siteIndex < siteCount; siteIndex++)
		//{
			//foreach (Vector2 polygon in polygons[siteIndex])
			//{
				//if (!border[siteIndex] && (polygon.X == 0f || polygon.Y == 0f || polygon.X == graphWidth || polygon.Y == graphHeight))
				//{
					//border[siteIndex] = true;
				//}
			//}
		//}
		//
		//return border;
	//}
	//
	//private static Region[] GenerateRegions(
		//float graphWidth,
		//float graphHeight,
		//float[] heights, 
		//Vector2[] positions, 
		//uint[][] neighbours,
		//bool[] border
	//)
	//{
		//int siteCount = positions.Length;
		//
		//bool[] visited = new bool[siteCount];
		//uint[] siteRegionIds = new uint[siteCount];
		//
		//Queue<int> queue = new();
		//
		//List<Region> regions = new();
		//
		//uint nextRegionId = 0;
		//
		//for (int startSiteIndex = 0; startSiteIndex < siteCount; startSiteIndex++)
		//{
			//if (visited[startSiteIndex])
			//{
				//continue;
			//}
			//
			//bool isWater = heights[startSiteIndex] <= WorldHeight.SeaLevel;
			//bool isBorder = border[startSiteIndex];
			//
			//uint regionSize = 0;
			//
			//Vector2 regionMinBoundary = new(float.MaxValue, float.MaxValue);
			//Vector2 regionMaxBoundary = new(float.MinValue, float.MinValue);
			//
			//Vector2 regionCentroid = Vector2.Zero;
			//
			//queue.Clear();
			//queue.Enqueue(startSiteIndex);
			//visited[startSiteIndex] = true;
			//
			//while (queue.Count > 0)
			//{
				//int currentSiteIndex = queue.Dequeue();
				//
				//siteRegionIds[currentSiteIndex] = nextRegionId;
				//regionSize++;
				//
				//Vector2 currentSitePosition = positions[currentSiteIndex];
				//
				//regionMinBoundary = regionMinBoundary.Min(currentSitePosition);
				//regionMaxBoundary = regionMaxBoundary.Max(currentSitePosition);
				//
				//if (!isBorder)
				//{
					//isBorder = border[currentSiteIndex];
				//}
				//
				//regionCentroid += currentSitePosition;
				//
				//foreach (uint neighbourIndex in neighbours[currentSiteIndex])
				//{
					//if (visited[neighbourIndex])
					//{
						//continue;
					//}
					//
					//bool neighbourIsWater = heights[neighbourIndex] <= WorldHeight.SeaLevel;
					//
					//if (neighbourIsWater != isWater)
					//{
						//continue;
					//}
					//
					//visited[neighbourIndex] = true;
					//queue.Enqueue((int)neighbourIndex);
				//}
				//
			//}
			//
			//RegionType type = RegionType.Continent;
			//if (isWater)
			//{
				//if (isBorder) {
					//type = RegionType.Ocean;
				//}
				//else 
				//{
					//type = RegionType.Lake;
				//}
			//}
			//else 
			//{
				//
			//}
			//
			//regions.Add(new Region(
				//"",
				//type,
				//isBorder,
				//regionSize,
				//regionCentroid,
				//regionMinBoundary,
				//regionMaxBoundary
			//));
		//}
		//
		//return regions.ToArray();
	//}
	//
	//private static Region[] GenerateLakes(
		//Region[] regions,
		//float[] heights, 
		//Vector2[] positions, 
		//uint[][] neighbours,
		//bool[] border
	//)
	//{
		//int siteCount = positions.Length;
		//
		//Queue<int> queue = new();
		//
		//for (int siteIndex = 0; siteIndex < siteCount; siteIndex++)
		//{
			//float siteHeight = heights[siteIndex];
			//
			//if (border[siteIndex] || siteHeight < WorldHeight.SeaLevel)
			//{
				//continue;
			//}
			//
			//float minHeight = float.MaxValue;
			//foreach (int neighbourIndex in neighbours[siteIndex])
			//{
				//if (heights[neighbourIndex] < minHeight)
				//{
					//minHeight = heights[neighbourIndex];
				//}
			//}
			//
			//if (siteHeight > minHeight)
			//{
				//continue;
			//}
			//
			//bool isDeep = true;
			//float threshold = siteHeight + WorldHeight.SeaLevel;
			//
			//bool[] visited = new bool[siteCount];
			//visited[siteIndex] = true;
			//
			//queue.Clear();
			//queue.Enqueue(siteIndex);
			//
			//while (isDeep && queue.Count > 0)
			//{
				//int currentSiteIndex = queue.Dequeue();
				//
				//foreach (int neighbourIndex in neighbours[currentSiteIndex])
				//{
					//if (visited[neighbourIndex] || heights[neighbourIndex] >= threshold)
					//{
						//continue;
					//}
					//
					//if (heights[neighbourIndex] < WorldHeight.SeaLevel) 
					//{
						//isDeep = false;
						//break;
					//}
					//
					//visited[neighbourIndex] = true;
					//
					//queue.Enqueue(neighbourIndex);
				//}
			//}
			//
			//if (isDeep)
			//{
				//
			//}
		//}
		//
		//return new Region[0];
	//}
//}
