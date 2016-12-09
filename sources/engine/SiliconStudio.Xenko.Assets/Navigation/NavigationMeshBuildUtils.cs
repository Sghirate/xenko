﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Physics;

namespace SiliconStudio.Xenko.Assets.Navigation
{
    public class NavigationMeshBuildUtils
    {
        /// <summary>
        /// Check which tiles overlap a given bounding box
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        public static List<Point> GetOverlappingTiles(NavigationMeshBuildSettings settings, BoundingBox boundingBox)
        {
            List<Point> ret = new List<Point>();
            float tcs = settings.TileSize * settings.CellSize;
            Vector2 start = boundingBox.Minimum.XZ() / tcs;
            Vector2 end = boundingBox.Maximum.XZ() / tcs;
            Point startTile = new Point(
                (int)Math.Floor(start.X),
                (int)Math.Floor(start.Y));
            Point endTile = new Point(
                (int)Math.Ceiling(end.X),
                (int)Math.Ceiling(end.Y));
            for (int y = startTile.Y; y < endTile.Y; y++)
            {
                for (int x = startTile.X; x < endTile.X; x++)
                {
                    ret.Add(new Point(x, y));
                }
            }
            return ret;
        }

        /// <summary>
        /// Clamps X-Z coordinates to a navigation mesh tile
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="boundingBox"></param>
        /// <param name="tileCoord"></param>
        /// <returns></returns>
        public static BoundingBox ClampBoundingBoxToTile(NavigationMeshBuildSettings settings, BoundingBox boundingBox, Point tileCoord)
        {
            float tcs = settings.TileSize * settings.CellSize;
            Vector2 tileMin = new Vector2(tileCoord.X * tcs, tileCoord.Y * tcs);
            Vector2 tileMax = tileMin + new Vector2(tcs);

            boundingBox.Minimum.X = tileMin.X;
            boundingBox.Minimum.Z = tileMin.Y;
            boundingBox.Maximum.X = tileMax.X;
            boundingBox.Maximum.Z = tileMax.Y;

            // Snap Y to tile height to avoid height differences between tiles
            boundingBox.Minimum.Y = (float)Math.Floor(boundingBox.Minimum.Y / settings.CellHeight) * settings.CellHeight;
            boundingBox.Maximum.Y = (float)Math.Ceiling(boundingBox.Maximum.Y / settings.CellHeight) * settings.CellHeight;

            return boundingBox;
        }

        /// <summary>
        /// Generates a random tangent and binormal for a given normal, 
        /// usefull for creating plane vertices or orienting objects (lookat) where the rotation along the normal doesn't matter
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="tangent"></param>
        /// <param name="binormal"></param>
        public static void GenerateTangentBinormal(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            tangent = Math.Abs(normal.Y) < 0.01f
                ? new Vector3(normal.Z, normal.Y, -normal.X)
                : new Vector3(-normal.Y, normal.X, normal.Z);
            tangent.Normalize();
            binormal = Vector3.Cross(normal, tangent);
            tangent = Vector3.Cross(binormal, normal);
        }

        /// <summary>
        /// Generates vertices and indices for an infinite size, limited by the <see cref="size"/> parameter
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="size">the amount from the origin the plane points are placed</param>
        /// <param name="points"></param>
        /// <param name="inds"></param>
        public static void BuildPlanePoints(ref Plane plane, float size, out Vector3[] points, out int[] inds)
        {
            Vector3 up = plane.Normal;
            Vector3 right;
            Vector3 forward;
            GenerateTangentBinormal(up, out right, out forward);

            points = new Vector3[4];
            points[0] = -forward * size - right * size + up * plane.D;
            points[1] = -forward * size + right * size + up * plane.D;
            points[2] = forward * size - right * size + up * plane.D;
            points[3] = forward * size + right * size + up * plane.D;

            inds = new int[6];
            // CCW
            inds[0] = 0;
            inds[1] = 2;
            inds[2] = 1;
            inds[3] = 1;
            inds[4] = 2;
            inds[5] = 3;
        }

        /// <summary>
        /// Applies an offset vector to a bounding box to make it bigger or smaller
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <param name="offsets"></param>
        public static void ExtendBoundingBox(ref BoundingBox boundingBox, Vector3 offsets)
        {
            boundingBox.Minimum -= offsets;
            boundingBox.Maximum += offsets;
        }

        /// <summary>
        /// Hashes and entity's transform and it's collider shape settings
        /// </summary>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static int HashEntityCollider(StaticColliderComponent collider)
        {
            int hash = 0;
            hash = (hash * 397) ^ collider.Entity.Transform.WorldMatrix.GetHashCode();
            hash = (hash * 397) ^ collider.CollisionGroup.GetHashCode();
            foreach (var shape in collider.ColliderShapes)
            {
                hash = (hash * 397) ^ shape.GetType().GetHashCode();
                hash = (hash * 397) ^ shape.GetHashCode();
            }
            return hash;
        }
    }
}