using SiliconStudio.Xenko.Engine;

namespace TopDownRPG
{
    class TopDownRPGApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
