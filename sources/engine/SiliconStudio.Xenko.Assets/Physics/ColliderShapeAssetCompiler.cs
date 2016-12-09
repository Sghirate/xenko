﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics.Data;
using SiliconStudio.Xenko.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Shaders.Ast;
using VHACDSharp;
using Buffer = SiliconStudio.Xenko.Graphics.Buffer;

namespace SiliconStudio.Xenko.Assets.Physics
{
    internal class ColliderShapeAssetCompiler : AssetCompilerBase
    {
        static ColliderShapeAssetCompiler()
        {
            NativeLibrary.PreloadLibrary("VHACD.dll");
        }

        protected override void Compile(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (ColliderShapeAsset)assetItem.Asset;
            result.BuildSteps = new AssetBuildStep(assetItem)
            {
                new ColliderShapeCombineCommand(targetUrlInStorage, asset, assetItem.Package),
            };

            result.ShouldWaitForPreviousBuilds = asset.ColliderShapes.Any(shape => shape != null && shape.GetType() == typeof(ConvexHullColliderShapeDesc));
        }

        public class ColliderShapeCombineCommand : AssetCommand<ColliderShapeAsset>
        {
            public ColliderShapeCombineCommand(string url, ColliderShapeAsset parameters, Package package)
                : base(url, parameters)
            {
                this.package = package;
            }

            private readonly Package package;

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                ComputeCompileTimeDependenciesHash(package, writer, Parameters);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager();

                // Cloned list of collider shapes
                var descriptions = Parameters.ColliderShapes.ToList();

                Parameters.ColliderShapes = Parameters.ColliderShapes.Where(x => x != null
                    && (x.GetType() != typeof(ConvexHullColliderShapeDesc) || ((ConvexHullColliderShapeDesc)x).Model != null)).ToList();

                //pre process special types
                foreach (var convexHullDesc in
                    (from shape in Parameters.ColliderShapes let type = shape.GetType() where type == typeof(ConvexHullColliderShapeDesc) select shape)
                    .Cast<ConvexHullColliderShapeDesc>())
                {
                    // Clone the convex hull shape description so the fields that should not be serialized can be cleared (Model in this case)
                    ConvexHullColliderShapeDesc convexHullDescClone = new ConvexHullColliderShapeDesc
                    {
                        Scaling = convexHullDesc.Scaling,
                        LocalOffset = convexHullDesc.LocalOffset,
                        LocalRotation = convexHullDesc.LocalRotation,
                        Depth = convexHullDesc.Depth,
                        PosSampling = convexHullDesc.PosSampling,
                        AngleSampling = convexHullDesc.AngleSampling,
                        PosRefine = convexHullDesc.PosRefine,
                        AngleRefine = convexHullDesc.AngleRefine,
                        Alpha = convexHullDesc.Alpha,
                        Threshold = convexHullDesc.Threshold,
                    };

                    // Replace shape in final result with cloned description
                    int replaceIndex = descriptions.IndexOf(convexHullDesc);
                    descriptions[replaceIndex] = convexHullDescClone;

                    var loadSettings = new ContentManagerLoaderSettings
                    {
                        ContentFilter = ContentManagerLoaderSettings.NewContentFilterByType(typeof(Mesh), typeof(Skeleton))
                    };
                    
                    var modelAsset = assetManager.Load<Model>(AttachedReferenceManager.GetUrl(convexHullDesc.Model), loadSettings);
                    if (modelAsset == null) continue;

                    convexHullDescClone.ConvexHulls = new List<List<List<Vector3>>>();
                    convexHullDescClone.ConvexHullsIndices = new List<List<List<uint>>>();

                    commandContext.Logger.Info("Processing convex hull generation, this might take a while!");

                    var nodeTransforms = new List<Matrix>();

                    //pre-compute all node transforms, assuming nodes are ordered... see ModelViewHierarchyUpdater
                    
                    if (modelAsset.Skeleton == null)
                    {
                        Matrix baseMatrix;
                        Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                        nodeTransforms.Add(baseMatrix);
                    }
                    else
                    {
                        var nodesLength = modelAsset.Skeleton.Nodes.Length;
                        for (var i = 0; i < nodesLength; i++)
                        {
                            Matrix localMatrix;
                            Matrix.Transformation(
                                ref modelAsset.Skeleton.Nodes[i].Transform.Scale,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Rotation,
                                ref modelAsset.Skeleton.Nodes[i].Transform.Position, out localMatrix);

                            Matrix worldMatrix;
                            if (modelAsset.Skeleton.Nodes[i].ParentIndex != -1)
                            {
                                var nodeTransform = nodeTransforms[modelAsset.Skeleton.Nodes[i].ParentIndex];
                                Matrix.Multiply(ref localMatrix, ref nodeTransform, out worldMatrix);
                            }
                            else
                            {
                                worldMatrix = localMatrix;
                            }

                            if (i == 0)
                            {
                                Matrix baseMatrix;
                                Matrix.Transformation(ref convexHullDescClone.Scaling, ref convexHullDescClone.LocalRotation, ref convexHullDescClone.LocalOffset, out baseMatrix);
                                nodeTransforms.Add(baseMatrix*worldMatrix);
                            }
                            else
                            {
                                nodeTransforms.Add(worldMatrix); 
                            }                           
                        }
                    }

                    for (var i = 0; i < nodeTransforms.Count; i++)
                    {
                        var i1 = i;
                        if (modelAsset.Meshes.All(x => x.NodeIndex != i1)) continue; // no geometry in the node

                        var combinedVerts = new List<float>();
                        var combinedIndices = new List<uint>();

                        var hullsList = new List<List<Vector3>>();
                        convexHullDescClone.ConvexHulls.Add(hullsList);

                        var indicesList = new List<List<uint>>();
                        convexHullDescClone.ConvexHullsIndices.Add(indicesList);

                        foreach (var meshData in modelAsset.Meshes.Where(x => x.NodeIndex == i1))
                        {
                            var indexOffset = (uint)combinedVerts.Count / 3;

                            var stride = meshData.Draw.VertexBuffers[0].Declaration.VertexStride;

                            var vertexBufferRef = AttachedReferenceManager.GetAttachedReference(meshData.Draw.VertexBuffers[0].Buffer);
                            byte[] vertexData;
                            if (vertexBufferRef.Data != null)
                            {
                                vertexData = ((BufferData)vertexBufferRef.Data).Content;
                            }
                            else if (!vertexBufferRef.Url.IsNullOrEmpty())
                            {
                                var dataAsset = assetManager.Load<Buffer>(vertexBufferRef.Url);
                                vertexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                continue;
                            }

                            var vertexIndex = meshData.Draw.VertexBuffers[0].Offset;
                            for (var v = 0; v < meshData.Draw.VertexBuffers[0].Count; v++)
                            {
                                var posMatrix = Matrix.Translation(new Vector3(BitConverter.ToSingle(vertexData, vertexIndex + 0), BitConverter.ToSingle(vertexData, vertexIndex + 4), BitConverter.ToSingle(vertexData, vertexIndex + 8)));

                                Matrix rotatedMatrix;
                                var nodeTransform = nodeTransforms[i];
                                Matrix.Multiply(ref posMatrix, ref nodeTransform, out rotatedMatrix);

                                combinedVerts.Add(rotatedMatrix.TranslationVector.X);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Y);
                                combinedVerts.Add(rotatedMatrix.TranslationVector.Z);

                                vertexIndex += stride;
                            }

                            var indexBufferRef = AttachedReferenceManager.GetAttachedReference(meshData.Draw.IndexBuffer.Buffer);
                            byte[] indexData;
                            if (indexBufferRef.Data != null)
                            {
                                indexData = ((BufferData)indexBufferRef.Data).Content;
                            }
                            else if (!indexBufferRef.Url.IsNullOrEmpty())
                            {
                                var dataAsset = assetManager.Load<Buffer>(indexBufferRef.Url);
                                indexData = dataAsset.GetSerializationData().Content;
                            }
                            else
                            {
                                throw new Exception("Failed to find index buffer while building a convex hull.");
                            }

                            var indexIndex = meshData.Draw.IndexBuffer.Offset;
                            for (var v = 0; v < meshData.Draw.IndexBuffer.Count; v++)
                            {
                                if (meshData.Draw.IndexBuffer.Is32Bit)
                                {
                                    combinedIndices.Add(BitConverter.ToUInt32(indexData, indexIndex) + indexOffset);
                                    indexIndex += 4;
                                }
                                else
                                {
                                    combinedIndices.Add(BitConverter.ToUInt16(indexData, indexIndex) + indexOffset);
                                    indexIndex += 2;
                                }
                            }
                        }

                        var decompositionDesc = new ConvexHullMesh.DecompositionDesc
                        {
                            VertexCount = (uint)combinedVerts.Count / 3,
                            IndicesCount = (uint)combinedIndices.Count,
                            Vertexes = combinedVerts.ToArray(),
                            Indices = combinedIndices.ToArray(),
                            Depth = convexHullDesc.Depth,
                            PosSampling = convexHullDesc.PosSampling,
                            PosRefine = convexHullDesc.PosRefine,
                            AngleSampling = convexHullDesc.AngleSampling,
                            AngleRefine = convexHullDesc.AngleRefine,
                            Alpha = convexHullDesc.Alpha,
                            Threshold = convexHullDesc.Threshold,
                            SimpleHull = convexHullDesc.SimpleWrap
                        };

                        var convexHullMesh = new ConvexHullMesh();

                        convexHullMesh.Generate(decompositionDesc);

                        var count = convexHullMesh.Count;

                        commandContext.Logger.Info("Node generated " + count + " convex hulls");

                        var vertexCountHull = 0;

                        for (uint h = 0; h < count; h++)
                        {
                            float[] points;
                            convexHullMesh.CopyPoints(h, out points);

                            var pointList = new List<Vector3>();

                            for (var v = 0; v < points.Length; v += 3)
                            {
                                var vert = new Vector3(points[v + 0], points[v + 1], points[v + 2]);
                                pointList.Add(vert);

                                vertexCountHull++;
                            }

                            hullsList.Add(pointList);

                            uint[] indices;
                            convexHullMesh.CopyIndices(h, out indices);

                            for (var t = 0; t < indices.Length; t += 3)
                            {
                                Utilities.Swap(ref indices[t], ref indices[t + 2]);
                            }

                            var indexList = new List<uint>(indices);

                            indicesList.Add(indexList);
                        }

                        convexHullMesh.Dispose();

                        commandContext.Logger.Info("For a total of " + vertexCountHull + " vertexes");
                    }
                }

                var runtimeShape = new PhysicsColliderShape { Descriptions = descriptions };
                assetManager.Save(Url, runtimeShape);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
