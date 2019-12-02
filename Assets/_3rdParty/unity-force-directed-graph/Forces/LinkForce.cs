using UnityEngine;

namespace Forces
{
    public class LinkForce : Force
    {
        private const float Distance = 1f;

        public override void ApplyForce(float alpha)
        {
            Edges.ForEach(edge =>
            {
                var source = edge.Source;
                var target = edge.Target;

                var delta = target.Position + target.Velocity - source.Position - source.Velocity;
                var length = delta.magnitude != 0f ? delta.magnitude : Jiggle();
                var weight = (length - Distance) / length * alpha;
                var weightedDelta = delta * weight;

                target.Velocity -= weightedDelta;
                source.Velocity += weightedDelta;

            });
        }

        private static float Jiggle()
        {
            return (Random.value - 0.4f) * 1e-6f;
        }
    }
}