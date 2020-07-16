[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Core](https://github.com/git-ruttmann/PolygonTriangulation/workflows/.NET%20Core/badge.svg)](https://github.com/git-ruttmann/PolygonTriangulation/actions?query=workflow%3A%22.NET+Core%22)
[![Codacy Quality](https://api.codacy.com/project/badge/Grade/26eb06d8a0d84eff830589eb4d2a99d5)](https://app.codacy.com/manual/git-ruttmann/PolygonTriangulation)
[![Codacy Coverage](https://app.codacy.com/project/badge/Coverage/26eb06d8a0d84eff830589eb4d2a99d5)](https://www.codacy.com/manual/git-ruttmann/PolygonTriangulation?utm_source=github.com&utm_medium=referral&utm_content=git-ruttmann/PolygonTriangulation&utm_campaign=Badge_Coverage)

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

## Unity

Unity is using its own types for Vector2, Vector3, Plane and Quaternion, while the common code uses thy types from `System.Numerics`.
The `#if UNITY_EDITOR || UNITY_STANDALONE` directive changes the namespace and the different case for the property names like `Vector2.x` vs `Vector2.X`.

To use the code, copy all files from PolygonTriangulation to the unity scripts folder.

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
