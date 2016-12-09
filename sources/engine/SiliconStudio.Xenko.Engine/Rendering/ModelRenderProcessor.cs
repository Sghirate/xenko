﻿using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Threading;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Materials;
using SiliconStudio.Xenko.Rendering.Materials.ComputeColors;

namespace SiliconStudio.Xenko.Rendering
{
    public class ModelRenderProcessor : EntityProcessor<ModelComponent, RenderModel>, IEntityComponentRenderProcessor
    {
        private Material fallbackMaterial;

        public Dictionary<ModelComponent, RenderModel> RenderModels => ComponentDatas;

        public VisibilityGroup VisibilityGroup { get; set; }

        public ModelRenderProcessor() : base(typeof(TransformComponent))
        {
        }

        protected internal override void OnSystemAdd()
        {
            base.OnSystemAdd();

            var graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            fallbackMaterial = Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });
        }

        protected override RenderModel GenerateComponentData(Entity entity, ModelComponent component)
        {
            var modelComponent = entity.Get<ModelComponent>();
            var renderModel = new RenderModel(modelComponent);

            return renderModel;
        }

        protected override void OnEntityComponentRemoved(Entity entity, ModelComponent component, RenderModel renderModel)
        {
            // Remove old meshes
            if (renderModel.Meshes != null)
            {
                foreach (var renderMesh in renderModel.Meshes)
                {
                    // Unregister from render system
                    VisibilityGroup.RenderObjects.Remove(renderMesh);
                }
            }
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            // Note: we are rebuilding RenderMeshes every frame
            // TODO: check if it wouldn't be better to add/remove directly in CheckMeshes()?
            //foreach (var entity in ComponentDatas)
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var renderModel = entity.Value;

                CheckMeshes(renderModel);
                UpdateRenderModel(renderModel);
            });
        }

        private void UpdateRenderModel(RenderModel renderModel)
        {
            if (renderModel.ModelComponent.Model == null)
                return;

            var modelComponent = renderModel.ModelComponent;
            var modelViewHierarchy = modelComponent.Skeleton;
            var nodeTransformations = modelViewHierarchy.NodeTransformations;

            var modelComponentMaterials = modelComponent.Materials;
            var modelMaterials = renderModel.ModelComponent.Model.Materials;

            for (int meshIndex = 0; meshIndex < renderModel.Meshes.Length; meshIndex++)
            {
                var renderMesh = renderModel.Meshes[meshIndex];
                var mesh = renderMesh.Mesh;
                var meshInfo = modelComponent.MeshInfos[meshIndex];

                renderMesh.Enabled = modelComponent.Enabled;

                if (renderMesh.Enabled)
                {
                    // Update material
                    var materialIndex = mesh.MaterialIndex;
                    var materialOverride = modelComponentMaterials.SafeGet(materialIndex);
                    var modelMaterialInstance = modelMaterials.GetItemOrNull(materialIndex);
                    UpdateMaterial(renderMesh, materialOverride, modelMaterialInstance, modelComponent);

                    // Copy world matrix
                    var nodeIndex = mesh.NodeIndex;
                    renderMesh.World = nodeTransformations[nodeIndex].WorldMatrix;
                    renderMesh.IsScalingNegative = nodeTransformations[nodeIndex].IsScalingNegative;
                    renderMesh.BoundingBox = new BoundingBoxExt(meshInfo.BoundingBox);
                    renderMesh.RenderGroup = modelComponent.Entity.Group;
                    renderMesh.BlendMatrices = meshInfo.BlendMatrices;
                }
            }
        }

        private void UpdateMaterial(RenderMesh renderMesh, Material materialOverride, MaterialInstance modelMaterialInstance, ModelComponent modelComponent)
        {
            renderMesh.Material = materialOverride ?? modelMaterialInstance?.Material ?? fallbackMaterial;

            renderMesh.IsShadowCaster = modelComponent.IsShadowCaster;
            renderMesh.IsShadowReceiver = modelComponent.IsShadowReceiver;
            if (modelMaterialInstance != null)
            {
                renderMesh.IsShadowCaster = renderMesh.IsShadowCaster && modelMaterialInstance.IsShadowCaster;
                renderMesh.IsShadowReceiver = renderMesh.IsShadowReceiver && modelMaterialInstance.IsShadowReceiver;
            }
        }

        private void CheckMeshes(RenderModel renderModel)
        {
            // Check if model changed
            var model = renderModel.ModelComponent.Model;
            if (renderModel.Model == model)
                return;

            // Remove old meshes
            if (renderModel.Meshes != null)
            {
                lock (VisibilityGroup.RenderObjects)
                {
                    foreach (var renderMesh in renderModel.Meshes)
                    {
                        // Unregister from render system
                        VisibilityGroup.RenderObjects.Remove(renderMesh);
                    }
                }
            }

            if (model == null)
                return;

            // Create render meshes
            var renderMeshes = new RenderMesh[model.Meshes.Count];
            var modelComponent = renderModel.ModelComponent;
            for (int index = 0; index < model.Meshes.Count; index++)
            {
                var mesh = model.Meshes[index];

                // TODO: Somehow, if material changed we might need to remove/add object in render system again (to evaluate new render stage subscription)
                var materialIndex = mesh.MaterialIndex;
                renderMeshes[index] = new RenderMesh
                {
                    RenderModel = renderModel,
                    Mesh = mesh,
                };

                // Update material
                UpdateMaterial(renderMeshes[index], modelComponent.Materials.SafeGet(materialIndex), model.Materials.GetItemOrNull(materialIndex), modelComponent);
            }

            renderModel.Model = model;
            renderModel.Meshes = renderMeshes;

            // Update and register with render system
            lock (VisibilityGroup.RenderObjects)
            {
                foreach (var renderMesh in renderMeshes)
                {
                    VisibilityGroup.RenderObjects.Add(renderMesh);
                }
            }
        }
    }
}
