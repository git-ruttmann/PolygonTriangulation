namespace PolygonTriangulation
{
    using System;
    using System.Diagnostics;

    using IEdge = IActiveEdge<Trapezoid>;

    /// <summary>
    /// The receiver of split commands
    /// </summary>
    public interface IPolygonSplitter
    {
        /// <summary>
        /// Split the polygon between left and right vertex
        /// </summary>
        /// <param name="leftVertex">the left vertex</param>
        /// <param name="rightVertex">the right vertex</param>
        void SplitPolygon(int leftVertex, int rightVertex);
    }

    /// <summary>
    /// Create trapezoids by splitting, joining and traversing along a polygon.
    /// Each trapezoid has a left and a right base and an upper and lower edge.
    /// The left/right base lines are vertical, parallel and the X coordinate is defined by the associated vertex.
    /// Trapezoids are built from left to right.
    /// Each edge, the upper and the lower, has one current trapezoid.
    /// </summary>
    [DebuggerDisplay("{Debug}")]
    public class Trapezoid
    {
        /// <summary>
        /// The neighbor state of the left/right base line
        /// </summary>
        [Flags]
        private enum Base
        {
            /// <summary>
            /// No neighbor, i.e. a triangle.
            /// </summary>
            NoNeighbor = 1,

            /// <summary>
            /// One neighbor and the common vertex is on the upper corner.
            /// </summary>
            UpperCorner = 2,

            /// <summary>
            /// One neighbor and the common vertex is on the lower corner.
            /// </summary>
            LowerCorner = 4,

            /// <summary>
            /// The baseline has two neighbors, the associated vertex is somewhere in the middle of the base.
            /// </summary>
            TwoNeighbors = 8,
        }

        /// <summary>
        /// Gets the upper edge
        /// </summary>
        private readonly IEdge upperEdge;

        /// <summary>
        /// Gets the lower edge
        /// </summary>
        private readonly IEdge lowerEdge;

        /// <summary>
        /// The neighbor state of the left base and <see cref="leftVertex"/>.
        /// </summary>
        private readonly Base leftBase;

        /// <summary>
        /// Gets the index of the left vertex, defining the left base.
        /// </summary>
        private readonly int leftVertex;

        /// <summary>
        /// Initialize a new trapzoid
        /// </summary>
        /// <param name="leftVertex">the id of the vertex for the left "virtual trapezoid" edge</param>
        /// <param name="leftNeighborCount">number of left trapezoids</param>
        /// <param name="leftCornerValidity">the validity of the leftVertex. Can be Upper, Lower or None for Cusps</param>
        /// <param name="lowerEdge">the lower edge</param>
        /// <param name="upperEdge">the upper edge</param>
        private Trapezoid(int leftVertex, Base leftBase, IEdge lowerEdge, IEdge upperEdge)
        {
            this.leftBase = leftBase;
            this.leftVertex = leftVertex;

            this.lowerEdge = lowerEdge;
            this.upperEdge = upperEdge;
            this.lowerEdge.Data = this;
            this.upperEdge.Data = this;
        }

        /// <summary>
        /// Gets a debug string
        /// </summary>
        public string Debug => $"Left:{this.leftVertex} {this.leftBase} Low: {this.lowerEdge} High: {this.upperEdge}";

        /// <summary>
        /// A left pointing cusp that enters the polygon space.
        /// </summary>
        /// <param name="vertexId">the vertex id of the cusp</param>
        /// <param name="lowerEdge">the lower edge of the new split</param>
        /// <param name="upperEdge">the upper edge of the new split</param>
        /// <param name="splitter">the polygon splitter</param>
        public static void EnterInsideBySplit(int vertexId, IEdge lowerEdge, IEdge upperEdge, IPolygonSplitter splitter)
        {
            new Trapezoid(vertexId, Base.NoNeighbor, lowerEdge, upperEdge);
        }

        /// <summary>
        /// A right pointing cusp that enters the polygon space. Join the upper left and the lower left trapezoids in one.
        /// </summary>
        /// <param name="vertexId">the vertex id that joins the two edges.</param>
        /// <param name="lower">the left lower trapezoid</param>
        /// <param name="upper">the left upper trapezoid</param>
        /// <param name="splitter">the polygon splitter</param>
        public static void EnterInsideByJoin(Trapezoid lower, Trapezoid upper, int vertexId, IPolygonSplitter splitter)
        {
            upper.EvaluateRight(vertexId, Base.LowerCorner, splitter);
            lower.EvaluateRight(vertexId, Base.UpperCorner, splitter);

            new Trapezoid(vertexId, Base.TwoNeighbors, lower.lowerEdge, upper.upperEdge);
        }

        /// <summary>
        /// A cusp that transitions from inside to outside. Splits the Trapezoid by one point.
        /// </summary>
        /// <param name="vertexId">the vertex id of the start point</param>
        /// <param name="lowerEdge">the lower edge of the new split</param>
        /// <param name="upperEdge">the upper edge of the new split</param>
        /// <param name="splitter">the polygon splitter</param>
        public void LeaveInsideBySplit(int vertexId, IEdge lowerEdge, IEdge upperEdge, IPolygonSplitter splitter)
        {
            this.EvaluateRight(vertexId, Base.TwoNeighbors, splitter);

            new Trapezoid(vertexId, Base.LowerCorner, upperEdge, this.upperEdge);
            new Trapezoid(vertexId, Base.UpperCorner, this.lowerEdge, lowerEdge);
        }

        /// <summary>
        /// Join two edges. Right of the vertex is outside.
        /// </summary>
        /// <param name="vertexId">the closing vertex id</param>
        /// <param name="splitter">the polygon splitter</param>
        public void LeaveInsideByJoin(int vertexId, IPolygonSplitter splitter)
        {
            this.EvaluateRight(vertexId, Base.NoNeighbor, splitter);
        }

        /// <summary>
        /// The upper edge transitions at vertex to a new edge
        /// </summary>
        /// <param name="vertexId">the transition vertex</param>
        /// <param name="nextEdge">the new edge</param>
        /// <param name="splitter">the polygon splitter</param>
        public void TransitionOnUpperEdge(int vertexId, IEdge nextEdge, IPolygonSplitter splitter)
        {
            this.EvaluateRight(vertexId, Base.UpperCorner, splitter);

            new Trapezoid(vertexId, Base.UpperCorner, this.lowerEdge, nextEdge);
        }

        /// <summary>
        /// The lower edge transitions at vertex to a new edge
        /// </summary>
        /// <param name="vertexId">the transition vertex</param>
        /// <param name="nextEdge">the new edge</param>
        /// <param name="splitter">the polygon splitter</param>
        public void TransitionOnLowerEdge(int vertexId, IEdge nextEdge, IPolygonSplitter splitter)
        {
            this.EvaluateRight(vertexId, Base.LowerCorner, splitter);

            new Trapezoid(vertexId, Base.LowerCorner, nextEdge, this.upperEdge);
        }

        /// <summary>
        /// Detects whether the left and right vertex represent a diagonale of the trapezoid.
        /// </summary>
        /// <param name="combinedBase">the combined base line state</param>
        /// <returns>true if a diagonale is detected</returns>
        private static bool DetectDiagonale(Base combinedBase)
        {
            return combinedBase == (Base.LowerCorner | Base.UpperCorner);
        }

        /// <summary>
        /// Detects whether one side has two neighbors (i.e. a touching cusp).
        /// </summary>
        /// <param name="combinedBase">the combined base line state</param>
        /// <returns>true if any base line has two neighbors</returns>
        private static bool DetectDoubleNeighbor(Base combinedBase)
        {
            return (combinedBase & Base.TwoNeighbors) != 0;
        }

        /// <summary>
        /// Combine the right side info with the left side and evaluate if it matches a split situation.
        /// </summary>
        /// <param name="rightVertex">the vertex that defines the right side</param>
        /// <param name="neighborCount">the number of right neighbors</param>
        /// <param name="cornerValidity">the vertex position of the right vertex</param>
        private void EvaluateRight(int rightVertex, Base rightBase, IPolygonSplitter splitter)
        {
            var combinedBase = this.leftBase | rightBase;
            if (DetectDoubleNeighbor(combinedBase) || DetectDiagonale(combinedBase))
            {
                splitter.SplitPolygon(this.leftVertex, rightVertex);
            }
        }
    }
}
