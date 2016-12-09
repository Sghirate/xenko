using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Physics.Tests
{
    public class SkinnedTest : GameTest
    {
        public SkinnedTest() : base("SkinnedTest")
        {
        }

        public static bool ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation)
        {
            var invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

            Vector3 sPos;
            sPos.X = screenPos.X * 2f - 1f;
            sPos.Y = 1f - screenPos.Y * 2f;

            sPos.Z = 0f;
            var vectorNear = Vector3.Transform(sPos, invViewProj);
            vectorNear /= vectorNear.W;

            sPos.Z = 1f;
            var vectorFar = Vector3.Transform(sPos, invViewProj);
            vectorFar /= vectorFar.W;

            var result = simulation.RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ());
            foreach (var hitResult in result)
            {
                if (hitResult.Succeeded)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void SkinnedTest1()
        {
            var game = new SkinnedTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();

                var character = game.SceneSystem.SceneInstance.Scene.Entities.First(ent => ent.Name == "Model");
                var dynamicBody = character.GetAll<RigidbodyComponent>().First(x => !x.IsKinematic);
                var kinematicBody = character.GetAll<RigidbodyComponent>().First(x => x.IsKinematic);
                var model = character.Get<ModelComponent>();
                var anim = character.Get<AnimationComponent>();

                var pastTransform = model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix;

                //let the controller land
                var twoSeconds = 120;
                while (twoSeconds-- > 0)
                {
                    await game.Script.NextFrame();
                }

                Assert.AreEqual(dynamicBody.BoneWorldMatrix, model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix);
                Assert.AreNotEqual(pastTransform, model.Skeleton.NodeTransformations[dynamicBody.BoneIndex].WorldMatrix);

                anim.Play("Run");

                pastTransform = model.Skeleton.NodeTransformations[kinematicBody.BoneIndex].WorldMatrix;

                Assert.AreEqual(kinematicBody.BoneWorldMatrix, pastTransform);

                await game.Script.NextFrame();

                Assert.AreNotEqual(kinematicBody.BoneWorldMatrix, pastTransform);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
