# Procedural Race Track Generator
 
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white)
![Blender](https://img.shields.io/badge/blender-%23F5792A.svg?style=for-the-badge&logo=blender&logoColor=white)
![Krita](https://img.shields.io/badge/Krita-203759?style=for-the-badge&logo=krita&logoColor=EEF37B)
![Itch.io](https://img.shields.io/badge/Itch-%23FF0B34.svg?style=for-the-badge&logo=Itch.io&logoColor=white)

![cover](https://user-images.githubusercontent.com/35520562/194764682-cb782bc4-1ce5-4442-bbbe-a63d65dffb74.png)

## Overview
This application generates a 3D, procedural race track and terrain, with a settings menu allowing you to tweak the track shape. There is also a car model and driving controller allowing you to drive around the track.

![game screenshot 1](https://user-images.githubusercontent.com/35520562/194768545-eee519a9-f9bf-4faa-9a30-ec30535e03a7.png)

## Installation and Use
Only the Unity script files are included in this repository therefore it is not possible to install and run the entire Unity project. However a playable version is available on my [itch.io page](https://fraser-curry-games.itch.io/race-track-generator).

## Generation

### Points
This is using a technique by Gustavo Maciel detailed in [this article](http://blog.meltinglogic.com/2013/12/how-to-generate-procedural-racetracks/).

Firstly, we generate a set of random points and calculate their convex hull to give us a looping polygon for the base of our track. To avoid clipping and other issues we space the points out if they are too close together. In order to make it look more like a race track we add a midpoint on each line of the hull polygon and displace that to create more curves and corners. Finally, we space the points again and ensure that no corner is too tight.

To convert this technique to a 3D space we generate a 2D perlin noise map and then offset the y axis of each point by the noise value at that coordinate. Note that this step is actually completed with the mesh generation so that we can match up the terrain to the track.

### Spline
We convert each set of consecutive points into a spline segment using catmull-rom splines.

### Track Mesh
This is using the technique explained by Freya Holmer in [this video](https://www.youtube.com/watch?v=6xs0Saff940&t=21544s).

Firstly, we generate a set of equally spaced points along each spline segment, a higher number of points is used for segments with a tighter turn or larger elevation change. For each point we determine an orientation based on the direction to neighbouring points, then we place the vertices of a 2D cross section mesh at each point in that local orientation. Triangles are set between the same lines of the 2D mesh at consecutive points to create a sort of hollow tube, an example between two points:

![Curb Road Preview](https://user-images.githubusercontent.com/35520562/194766648-15a4b2eb-3960-4f12-bddd-febcb6b8368c.png)

We can set the U coordinates of each vertex directly from the 2D mesh and we find the V coordinates by multiplying the percentage along the spline segment of the current point and the ratio of the total length of the spline segment to the span of the 2D mesh. This is not a perfect solution and there are still cases of UV stretching on tight corners, this would require the use of lookup tables to fix.

The normals are simply set from the 2D mesh but converted in the orientation of the point.

A mesh collider for the track is generated the same way, just without the need for the UV coordinates or normals, using a lower number of points to help performance.

### Terrain Mesh
To create the terrain we generate a grid of points, drawing triangles between neighbouring points and setting UV coordinates as a percentage of the grid length, these coordinates are scaled up so that the texture will tile as this is a particularly large mesh. As mentioned before, we use the same perlin noise map to displace the y coordinates of the grid as we used for the track. Again a mesh collider is then generated with less points.
