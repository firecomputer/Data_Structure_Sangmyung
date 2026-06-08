# 스택, 큐, 그래프가 이 프로젝트에서 쓰인 방식

이 프로젝트는 Unity 기반 배송/경로 시뮬레이션이며, 데이터 구조는 크게 `맵 생성`, `경로 탐색`, `배송 처리`의 세 단계에서 사용된다.

## 1. 스택

이 프로젝트에는 `Stack<T>`가 직접 선언된 코드는 없다.  
대신, **경로를 역추적하고 다시 순서대로 복원하는 과정**이 스택과 비슷한 역할을 한다.

### 어디에서 사용되나

- `Assets/Scripts/AStarPathfinder.cs`
  - `cameFrom` 딕셔너리를 따라 목표 노드에서 시작 노드 쪽으로 거슬러 올라간다.
  - `ReconstructPath()`에서 `path.Insert(0, current)`를 사용해 경로를 앞에서부터 다시 만든다.
  - 결과적으로 "마지막에 찾은 노드부터 되돌아가서" 올바른 이동 순서를 만드는 LIFO 성격이 나타난다.

- `Assets/Scripts/CourierController.cs`
  - `ReturnToCenter()`에서 현재 경로를 `Reverse()` 해서 반대로 복귀한다.
  - 이것도 엄밀한 스택 사용은 아니지만, **마지막에 지나간 경로를 되짚어 돌아가는 방식**이라 스택 개념과 연결된다.

### 정리

- 직접적인 스택 자료구조: 없음
- 스택 개념의 역할:
  - 경로 역추적
  - 복귀 경로 반전

## 2. 큐

큐는 이 프로젝트에서 **배송 요청의 순서 관리**에 직접 사용된다.

### 어디에서 사용되나

- `Assets/Scripts/Game/DeliveryQueue.cs`
  - `_destinations`가 `Queue<Vector2>`로 선언되어 있다.
  - `Enqueue(destination)`으로 배송 목적지를 추가한다.
  - `ProcessNext()`에서 `_destinations.Dequeue()`로 가장 먼저 들어온 목적지를 먼저 처리한다.

### 의미

- 배송 요청은 먼저 들어온 순서대로 처리된다.
- 즉, 이 프로젝트의 큐는 **배송 작업 대기열** 역할을 한다.

### 추가로 참고할 점

- `Assets/Scripts/AStarPathfinder.cs`의 `PriorityQueue<T>`는 일반 큐가 아니라 **우선순위 큐**다.
  - A* 탐색에서 다음에 방문할 노드를 `fScore`가 낮은 순서로 꺼내기 위해 사용한다.
  - FIFO가 아니라 "가장 유리한 노드부터 처리"하는 구조다.

## 3. 그래프

그래프는 이 프로젝트의 핵심 구조다.  
맵의 도로망을 **노드와 간선**으로 표현하고, 그 위에서 경로 탐색을 수행한다.

### 3-1. 맵 생성 단계의 그래프

- `Assets/Scripts/MazeManager.cs`
  - 빌딩 위치를 `nodePositions`에 모은다.
  - 인접한 길/건물 칸을 연결해 `MSTGenerator.Edge` 목록을 만든다.
  - `GenerateProceduralMaze()`에서는 점들을 만들고, Delaunay 삼각분할 후 MST를 구해 도로망의 골격을 만든다.
  - `LoadManualMap()`에서는 JSON 맵 데이터를 읽어 격자 기반의 노드와 간선을 구성한다.

- `Assets/Scripts/DelaunayTriangulator.cs`
  - 점 집합을 삼각형/간선으로 나누어 후보 연결 구조를 만든다.
  - 이후 `MSTGenerator`로 넘어갈 입력 그래프 역할을 한다.

- `Assets/Scripts/MSTGenerator.cs`
  - `UnionFind`를 이용해 최소 신장 트리(MST)를 만든다.
  - 결과적으로 불필요한 간선을 줄인 **도로 그래프의 뼈대**를 만든다.

### 3-2. 경로 탐색 단계의 그래프

- `Assets/Scripts/DeliveryManager.cs`
  - `BuildPathfinderGraph()`에서 `MazeManager`가 만든 MST 간선을 `AStarPathfinder`에 전달한다.
  - `FindPathToDestination()` / `FindPathFromTo()`가 실제 경로 탐색 진입점이다.

- `Assets/Scripts/AStarPathfinder.cs`
  - `_graph`는 `Dictionary<Vector2, List<Vector2>>` 형태의 인접 리스트다.
  - `BuildGraph()`에서 간선을 양방향으로 등록한다.
  - `FindPathInternal()`에서 현재 노드의 이웃을 순회하며 A* 탐색을 수행한다.
  - `cameFrom`, `gScore`, `fScore`를 사용해 최단 경로를 찾는다.

### 3-3. 그래프 시각화와 활용

- `Assets/Scripts/RoadVisualizer.cs`
  - `Visualize()`에서 간선을 실제 도로 오브젝트로 만든다.
  - 즉, 내부 그래프 구조를 게임 화면의 도로로 변환한다.

- `Assets/Scripts/CourierController.cs`
  - A*가 반환한 경로를 따라 택배 차량이 이동한다.
  - 그래프 위의 이동 결과가 실제 게임 플레이 동작으로 이어진다.

## 4. 전체 흐름 요약

1. `MazeManager`가 노드와 도로 간선을 만든다.
2. `MSTGenerator`가 최소 신장 트리 형태의 도로 그래프를 만든다.
3. `DeliveryManager`가 그 간선을 `AStarPathfinder`의 인접 리스트 그래프로 바꾼다.
4. `AStarPathfinder`가 최단 경로를 찾는다.
5. `DeliveryQueue`가 배송 요청 순서를 관리한다.
6. `CourierController`가 경로를 따라 이동하고, 필요하면 반대 방향으로 복귀한다.

## 5. 한 줄 정리

- 스택: 경로 역추적과 복귀 경로 반전에서 스택과 비슷한 방식이 쓰였다.
- 큐: 배송 요청을 먼저 온 순서대로 처리하는 데 직접 사용됐다.
- 그래프: 맵의 도로망과 A* 최단 경로 탐색의 핵심 표현 방식으로 사용됐다.
