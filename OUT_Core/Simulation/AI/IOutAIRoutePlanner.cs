using UnityEngine;

public interface IOutAIRoutePlanner
{
    // Пытается построить маршрут под конкретный запрос.
    // Возвращает первую waypoint-точку, в которую уже можно реально двигаться.
    bool TryBuildRoute(in OUT_AIRouteRequest request, out Vector3 firstWaypoint);

    // Пытается найти укрытие от угрозы.
    bool TryFindCover(Vector3 threatPosition, float minDistance, float maxDistance, out Vector3 coverPoint);

    // Пытается сделать локальный обход без полного перестроения большого маршрута.
    bool TryTriangulate(Vector3 destination, out Vector3 apexPoint);

    // Обновить текущий маршрут, если он устарел или мир изменился.
    void RefreshRoute();

    // Полностью очистить текущий маршрут.
    void ClearRoute();

    // Есть ли сейчас активный маршрут.
    bool HasActiveRoute { get; }

    // Текущая точка маршрута, к которой должен идти mover.
    Vector3 CurrentWaypoint { get; }
}