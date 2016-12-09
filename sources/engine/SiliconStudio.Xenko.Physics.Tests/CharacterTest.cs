using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Physics.Tests
{
    public class CharacterTest : GameTest
    {
        public CharacterTest() : base("CharacterTest")
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
        public void CharacterTest1()
        {
            var game = new CharacterTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                await game.Script.NextFrame();
                await game.Script.NextFrame();

                var character = game.SceneSystem.SceneInstance.Scene.Entities.First(ent => ent.Name == "Character");
                var controller = character.Get<CharacterComponent>();
                var simulation = controller.Simulation;

                //let the controller land
                var twoSeconds = 120;
                while (twoSeconds-- > 0)
                {
                    await game.Script.NextFrame();
                }

                Assert.IsTrue(controller.IsGrounded);

                controller.Jump();

                await game.Script.NextFrame();

                Assert.IsFalse(controller.IsGrounded);

                //let the controller land
                twoSeconds = 120;
                while (twoSeconds-- > 0)
                {
                    await game.Script.NextFrame();
                }

                Assert.IsTrue(controller.IsGrounded);

                var currentPos = character.Transform.Position;

                controller.SetVelocity(Vector3.UnitX * 3);

                await game.Script.NextFrame();

                Assert.AreNotEqual(currentPos, character.Transform.Position);
                var target = currentPos + Vector3.UnitX*3*simulation.FixedTimeStep;
                Assert.AreEqual(character.Transform.Position.X, target.X, float.Epsilon);
                Assert.AreEqual(character.Transform.Position.Y, target.Y, float.Epsilon);
                Assert.AreEqual(character.Transform.Position.Z, target.Z, float.Epsilon);

                currentPos = character.Transform.Position;

                await game.Script.NextFrame();

                Assert.AreNotEqual(currentPos, character.Transform.Position);
                target = currentPos + Vector3.UnitX * 3 * simulation.FixedTimeStep;
                Assert.AreEqual(character.Transform.Position.X, target.X, float.Epsilon);
                Assert.AreEqual(character.Transform.Position.Y, target.Y, float.Epsilon);
                Assert.AreEqual(character.Transform.Position.Z, target.Z, float.Epsilon);

                controller.SetVelocity(Vector3.Zero);

                await game.Script.NextFrame();

                currentPos = character.Transform.Position;

                await game.Script.NextFrame();

                Assert.AreEqual(currentPos, character.Transform.Position);

                var collider = game.SceneSystem.SceneInstance.Scene.Entities.First(ent => ent.Name == "Collider").Get<StaticColliderComponent>();

                game.Script.AddTask(async () =>
                {
                    var fourSeconds = 240;
                    while (fourSeconds-- > 0)
                    {
                        await game.Script.NextFrame();
                    }
                    Assert.Fail("Character controller never collided with test collider.");
                });

                controller.SetVelocity(Vector3.UnitX * 2.5f);

                await collider.NewCollision();

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
