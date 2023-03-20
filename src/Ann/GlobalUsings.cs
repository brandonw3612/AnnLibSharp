// Coordinate data type
global using AnnCoordinate = System.Double;
// Distance data type
global using AnnDistance = System.Double;
// Point index
global using AnnIndex = System.Int32;

// A point
global using AnnPoint = System.Collections.Generic.List<double>;
// An array of points
global using AnnPointCollection = System.Collections.Generic.List<System.Collections.Generic.List<double>>;
// An array of distances
global using AnnDistanceCollection = System.Collections.Generic.List<double>;
// An array of point indices
global using AnnIndexCollection = System.Collections.Generic.List<int>;

global using AnnOrthogonalHalfSpaceCollection = System.Collections.Generic.List<Ann.Primitives.OrthogonalHalfSpace>;
using Ann.Primitives;

namespace Ann;

public delegate (int CuttingDimension, AnnCoordinate CuttingValue, int LowSidePointCount) SplittingRoutine(
    AnnPointCollection points,
    AnnIndexCollection indices,
    OrthogonalRectangle boundingRectangle,
    int numberOfPoints,
    int dimension
);