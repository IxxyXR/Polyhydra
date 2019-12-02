namespace Forces
{
    public class ExpansionForce : Force
    {
        private const float ExpansionFactor = 0.1f;

        public override void ApplyForce(float alpha)
        {
            Nodes.ForEach(node =>
            {
                var direction = node.Position.normalized;

                node.Velocity = direction * ExpansionFactor * alpha;
            });
        }
    }
}