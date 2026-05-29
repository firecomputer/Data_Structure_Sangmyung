#include <stdio.h>
#include <stdlib.h>

#define MAX_VERTEX 30

// [이미지 내용] 그래프를 인접 행렬로 표현하기 위한 구조체 정의
typedef struct graphType {
    int n;                               // 그래프의 정점 개수
    int adjMatrix[MAX_VERTEX][MAX_VERTEX]; // 그래프에 대한 30x30의 2차원 배열
} graphType;

// 함수 선언 (Prototypes)
void createGraph(graphType* g);
void insertVertex(graphType* g, int v);
void insertEdge(graphType* g, int u, int v);
void print_adjMatrix(graphType* g);

// 메인 함수: 구현된 그래프 기능을 테스트한다
int main() {
    // 그래프 구조체 동적 할당
    graphType *g = (graphType*)malloc(sizeof(graphType));
    if (g == NULL) {
        printf("메모리 할당 실패!\n");
        return 1;
    }

    // 1. 그래프 초기화
    createGraph(g);

    // 2. 정점 삽입 (0, 1, 2, 3 총 4개의 정점 생성)
    for (int i = 0; i < 4; i++) {
        insertVertex(g, i);
    }

    // 3. 간선 삽입 (무방향 그래프 예시)
    insertEdge(g, 0, 1);
    insertEdge(g, 0, 2);
    insertEdge(g, 1, 2);
    insertEdge(g, 2, 3);

    // 4. 인접 행렬 출력
    printf("--- 그래프 인접 행렬 결과 ---\n");
    print_adjMatrix(g);

    // 메모리 해제
    free(g);
    return 0;
}

// ------------------------------------------------------------
// 함수 구현부
// ------------------------------------------------------------

// 그래프 초기화: 정점 수를 0으로 설정하고 행렬을 모두 0으로 비운다
void createGraph(graphType* g) {
    int i, j;
    g->n = 0;
    for (i = 0; i < MAX_VERTEX; i++) {
        for (j = 0; j < MAX_VERTEX; j++) {
            g->adjMatrix[i][j] = 0;
        }
    }
}

// 정점 삽입: 정점 개수를 하나 증가시킨다
void insertVertex(graphType* g, int v) {
    if (((g->n) + 1) > MAX_VERTEX) {
        printf("\n 그래프 정점 개수 초과! \n");
        return;
    }
    g->n++;
}

// 간선 삽입: 두 정점 u와 v 사이의 연결을 행렬에 1로 표시한다
void insertEdge(graphType* g, int u, int v) {
    // 존재하지 않는 정점에 간선을 연결하려고 할 때 예외 처리
    if (u >= g->n || v >= g->n) {
        printf("\n 그래프에 존재하지 않는 정점 번호! \n");
        return;
    }
    
    g->adjMatrix[u][v] = 1;
    g->adjMatrix[v][u] = 1; // 무방향(Undirected) 그래프 기준이라 대칭으로 넣어줬다
}

// 인접 행렬 출력: 현재 생성된 정점 크기만큼 행렬을 예쁘게 출력한다
void print_adjMatrix(graphType* g) {
    int i, j;
    for (i = 0; i < g->n; i++) {
        for (j = 0; j < g->n; j++) {
            printf("%2d ", g->adjMatrix[i][j]);
        }
        printf("\n");
    }
}
