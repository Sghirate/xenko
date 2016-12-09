using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Assets.Quantum
{
    /// <summary>
    /// A base visitor class that allows to visit a single part of an <see cref="AssetComposite"/> at a time.
    /// </summary>
    /// <typeparam name="TAssetPartDesign">The type of the design-time object containing the part.</typeparam>
    /// <typeparam name="TAssetPart">The type of the part.</typeparam>
    public class AssetCompositePartVisitor : GraphVisitorBase
    {
        private readonly AssetCompositePropertyGraph propertyGraph;

        public AssetCompositePartVisitor(AssetCompositePropertyGraph propertyGraph)
        {
            this.propertyGraph = propertyGraph;
        }

        protected override bool ShouldVisitNode(MemberContent memberContent, IGraphNode targetNode)
        {
            // Make sure it's actually a target (not a member) node.
            return !propertyGraph.IsReferencedPart(memberContent, targetNode) && base.ShouldVisitNode(memberContent, targetNode);
        }
    }
}
