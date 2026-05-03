#include <stdio.h>
#include <stdlib.h>

// --- 헤더 파일(traversalBT.h) 내용 ---
typedef char element;       // 트리에 저장될 데이터의 자료형을 char로 정의

typedef struct treeNode {   // 이진 트리 노드 구조체 정의
    element data;
    struct treeNode* left;
    struct treeNode* right;
} treeNode;

// --- 함수 원형 선언 ---
treeNode* makeRootNode(element data, treeNode* leftNode, treeNode* rightNode);
void preorder(treeNode* root);
void inorder(treeNode* root);
void postorder(treeNode* root);


// --- 이진 트리 함수 구현(traversalBT.c) ---

// data를 루트 노드로 하여 왼쪽 서브 트리와 오른쪽 서브 트리를 연결하는 연산
treeNode* makeRootNode(element data, treeNode* leftNode, treeNode* rightNode) {
    treeNode* root = (treeNode*)malloc(sizeof(treeNode));
    root->data = data;
    root->left = leftNode;
    root->right = rightNode;
    return root;
}

// 이진 트리에 대한 전위 순회 연산 (Preorder Traversal: DLR)
void preorder(treeNode* root) {
    if (root) {
        printf("%c", root->data);  // 작업D
        preorder(root->left);      // 작업L
        preorder(root->right);     // 작업R
    }
}

// 이진 트리에 대한 중위 순회 연산 (Inorder Traversal: LDR)
void inorder(treeNode* root) {
    if (root) {
        inorder(root->left);       // 작업L
        printf("%c", root->data);  // 작업D
        inorder(root->right);      // 작업R
    }
}

// 이진 트리에 대한 후위 순회 연산 (Postorder Traversal: LRD)
void postorder(treeNode* root) {
    if (root) {
        postorder(root->left);     // 작업L
        postorder(root->right);    // 작업R
        printf("%c", root->data);  // 작업D
    }
}


// --- 메인 함수(ex7_1.c) ---
int main(void) {
    // (A*B-C/D) 수식 이진 트리 만들기
    treeNode* n7 = makeRootNode('D', NULL, NULL);
    treeNode* n6 = makeRootNode('C', NULL, NULL);
    treeNode* n5 = makeRootNode('B', NULL, NULL);
    treeNode* n4 = makeRootNode('A', NULL, NULL);
    treeNode* n3 = makeRootNode('/', n6, n7);
    treeNode* n2 = makeRootNode('*', n4, n5);
    treeNode* n1 = makeRootNode('-', n2, n3);

    printf("\n preorder : ");
    preorder(n1);

    printf("\n inorder : ");
    inorder(n1);

    printf("\n postorder : ");
    postorder(n1);

    printf("\n");
    getchar(); 
    return 0;
}
