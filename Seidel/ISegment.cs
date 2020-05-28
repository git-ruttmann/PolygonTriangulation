namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System.Collections.Generic;
    using Vertex = System.Numerics.Vector2;

    public interface ISegment : IEnumerable<ISegment>
    {
        ISegment Prev { get; }

        int PrevId { get; }

        ISegment Next { get; }

        int NextId { get; }

        int Id { get; }

        Vertex v0 { get; }

        Vertex v1 { get; }

        Vertex Start { get; }

        Vertex End { get; }
    }
}
