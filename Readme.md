# Polygon Triangulation

A C# library to convert a complex polygon into triangles.
Support includes:
* multiple polygons
* polygon holes
* fusion vertex (same vertex is used by two sub polygons)

The project was developed as part of a Unity project to visualize the intersection between a 3D mesh and a plane.

The included Windows Forms project `PolygonDisplay` visualizes the [Polygon](Documentation/Polygon.md), the detected splits and the [Monotones](Documentation/Monotones.md).

## Usage

Create a `PlanePolygonBuilder(plane)` and call repeatedly `AddEdge(start, end)`.
After adding the last edge, call `Build()`.
The result contains a vertex array and a list of Triangles. A Triangle is defined by three consecutive indices in the vertex array.

The 3D vertices of the edge are converted to 2D by rotating them along the plane and removing the depth component.

## Testing

To test the intermediate steps, create a `IEdgesToPolygonBuilder` by calling `PlanePolygonBuilder.CreatePolygonBuilder()`.
Call `AddEdge(start, end)` to add the polygon edges and finally `BuildPolygon()` to receive a `IPlanePolygon` which contains a 
[Polygon](Documentation/Polygon.md) and the original 3D vertices. 
The polygon contains the 2D vertices in the same order as the 3D vertices of the result.

Alternatively construct the [Polygon](Documentation/Polygon.md) with `Polygon.Build()`.

Create a new `PolygonTriangulator(polygon)` and call `BuildTriangles()`.
That uses [Trapezoidation](Documentation/Polygon.md) to split the [Polygon](Documentation/Polygon.md) into [Monotones](Documentation/Monotones.md) and then 
triangulates those monotones.

To test only the edges to polygon conversion, create a `IPolygonLineDetector` by calling `PlanePolygonBuilder.CreatePolygonLineDetector()`.
Call `JoinEdgesToPolygones(pairsOfEdges)` to create polygon lines. It results in lists of `ClosedPolygons` and `UnclosedPolygons`.

Unclosed polygons can be joined by `TryClusteringUnclosedEnds(vertices)`.
That merges unclosed polygons by considering vertex coordinates with small distances as the same vertex.
