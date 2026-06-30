using UnityEngine;

public static class OUTL_GeometryConstants
{
    public const float Pi = 3.14159265358979323846f;
    public const float Tau = 6.28318530717958647692f;
    public const float HalfPi = 1.57079632679489661923f;
    public const float Deg2Rad = 0.01745329251994329577f;
    public const float Rad2Deg = 57.295779513082320876f;

    public const float E = 2.71828182845904523536f;
    public const float EulerMascheroni = 0.57721566490153286060f;

    public const float Sqrt2 = 1.4142135623730951f;
    public const float Sqrt3 = 1.7320508075688772f;
    public const float Sqrt5 = 2.23606797749979f;
    public const float InvSqrt2 = 0.7071067811865475f;
    public const float InvSqrt3 = 0.5773502691896258f;

    public const float Phi = 1.618033988749895f;
    public const float PhiInv = 0.6180339887498948f;
    public const float PhiSquared = 2.618033988749895f;
    public const float GoldenAngleDegrees = 137.50776405003785f;
    public const float GoldenAngleRadians = 2.399963229728653f;

    public const int BinaryParityPeriod = 2;
    public const int TriangleSides = 3;
    public const int SquareSides = 4;
    public const int PentagonSides = 5;
    public const int HexagonSides = 6;
    public const int HeptagonSides = 7;
    public const int OctagonSides = 8;
    public const int EnneagonSides = 9;
    public const int DecagonSides = 10;
    public const int DodecagonSides = 12;

    public const float TriangleInteriorAngle = 60f;
    public const float SquareInteriorAngle = 90f;
    public const float PentagonInteriorAngle = 108f;
    public const float HexagonInteriorAngle = 120f;
    public const float OctagonInteriorAngle = 135f;

    public const float TriangleStepAngle = 120f;
    public const float SquareStepAngle = 90f;
    public const float PentagonStepAngle = 72f;
    public const float HexagonStepAngle = 60f;
    public const float HeptagonStepAngle = 51.42857142857143f;
    public const float OctagonStepAngle = 45f;
    public const float EnneagonStepAngle = 40f;
    public const float DodecagonStepAngle = 30f;

    public const int TetrahedronVertices = 4;
    public const int TetrahedronEdges = 6;
    public const int TetrahedronFaces = 4;

    public const int CubeVertices = 8;
    public const int CubeEdges = 12;
    public const int CubeFaces = 6;

    public const int OctahedronVertices = 6;
    public const int OctahedronEdges = 12;
    public const int OctahedronFaces = 8;

    public const int DodecahedronVertices = 20;
    public const int DodecahedronEdges = 30;
    public const int DodecahedronFaces = 12;

    public const int IcosahedronVertices = 12;
    public const int IcosahedronEdges = 30;
    public const int IcosahedronFaces = 20;

    public const int TruncatedIcosahedronPentagons = 12;
    public const int TruncatedIcosahedronHexagons = 20;
    public const int TruncatedIcosahedronFaces = 32;
    public const int TruncatedIcosahedronVertices = 60;
    public const int TruncatedIcosahedronEdges = 90;

    public const int EulerCharacteristicSphere = 2;
    public const int EulerCharacteristicConvexPolyhedron = 2;
    public const int EulerCharacteristicTorus = 0;

    public const float UnitCircleArea = Pi;
    public const float UnitSphereSurfaceArea = Tau * 2f;
    public const float UnitSphereVolume = 4.1887902047863905f;
    public const float UnitConeVolume = Pi / 3f;

    public static float RegularPolygonStepDegrees(int sides)
    {
        return sides > 0 ? 360f / sides : 0f;
    }

    public static float RegularPolygonInteriorDegrees(int sides)
    {
        return sides >= 3 ? ((sides - 2) * 180f) / sides : 0f;
    }

    public static float RegularPolygonExteriorDegrees(int sides)
    {
        return sides > 0 ? 360f / sides : 0f;
    }

    public static int EulerCharacteristic(int vertices, int edges, int faces)
    {
        return vertices - edges + faces;
    }

    public static Vector3 GoldenSpiralPointXZ(int index, float radiusScale = 1f)
    {
        float r = Mathf.Sqrt(Mathf.Max(0, index)) * radiusScale;
        float a = index * GoldenAngleRadians;
        return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    public static Vector2 GoldenSpiralPoint2D(int index, float radiusScale = 1f)
    {
        float r = Mathf.Sqrt(Mathf.Max(0, index)) * radiusScale;
        float a = index * GoldenAngleRadians;
        return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
    }

    public static int CheckerParity(int x, int z)
    {
        return (x ^ z) & 1;
    }

    public static float SmoothStep01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }
}
