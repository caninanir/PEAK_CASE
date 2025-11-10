using UnityEngine;

public static class CubeFactory
{
    public static CubeItem CreateCube(ItemType cubeType, GameObject prefab, Transform parent)
    {
        GameObject cubeObj = Object.Instantiate(prefab, parent);
        CubeItem cube = cubeObj.GetComponent<CubeItem>();
        
        if (cube == null)
        {
            cube = cubeObj.AddComponent<CubeItem>();
        }
        
        cube.Initialize(cubeType);
        return cube;
    }

    public static ItemType GetRandomCubeType()
    {
        ItemType[] cubeTypes = { ItemType.RedCube, ItemType.GreenCube, ItemType.BlueCube, ItemType.YellowCube, ItemType.PurpleCube };
        return cubeTypes[Random.Range(0, cubeTypes.Length)];
    }

    public static bool IsCubeType(ItemType type)
    {
        return type == ItemType.RedCube || 
               type == ItemType.GreenCube || 
               type == ItemType.BlueCube || 
               type == ItemType.YellowCube ||
               type == ItemType.PurpleCube;
    }
}




