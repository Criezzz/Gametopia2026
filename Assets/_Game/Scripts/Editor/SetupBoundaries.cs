#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SetupBoundaries
{
    [MenuItem("Tools/Setup Camera Boundaries")]
    public static void Setup()
    {
        // Target 16:9 at ortho 5.625 => Height = 11.25, Width = 20
        GameObject boundaries = new GameObject("Level_Boundaries");
        boundaries.transform.position = Vector3.zero;

        // Left Wall
        var left = new GameObject("LeftWall");
        left.transform.SetParent(boundaries.transform);
        left.transform.position = new Vector3(-10f, 0, 0);
        var leftCol = left.AddComponent<BoxCollider2D>();
        leftCol.size = new Vector2(1f, 30f);

        // Right Wall
        var right = new GameObject("RightWall");
        right.transform.SetParent(boundaries.transform);
        right.transform.position = new Vector3(10f, 0, 0);
        var rightCol = right.AddComponent<BoxCollider2D>();
        rightCol.size = new Vector2(1f, 30f);

        // Top Wall
        var top = new GameObject("TopWall");
        top.transform.SetParent(boundaries.transform);
        top.transform.position = new Vector3(0, 7f, 0);
        var topCol = top.AddComponent<BoxCollider2D>();
        topCol.size = new Vector2(30f, 1f);

        // Ground layer assignment to act as walls/ceilings the player bumps into
        int groundLayer = LayerMask.NameToLayer("Ground");
        if(groundLayer != -1)
        {
            left.layer = groundLayer;
            right.layer = groundLayer;
            top.layer = groundLayer;
        }

        Debug.Log("[SetupBoundaries] Created Left, Right, and Top boundary walls!");
    }
}
#endif