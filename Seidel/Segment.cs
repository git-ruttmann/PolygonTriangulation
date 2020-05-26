namespace Ruttmann.PolygonTriangulation.Seidel
{
    using System.Collections.Generic;
    using System.Numerics;

    public interface ISegment : IEnumerable<ISegment>
    {
        ISegment Prev { get; }

        ISegment Next { get; }

        bool First { get; }

        int Id { get; }

        Vector2 v0 { get; }

        Vector2 v1 { get; }

        Vector2 Start { get; }

        Vector2 End { get; }
    }
}
