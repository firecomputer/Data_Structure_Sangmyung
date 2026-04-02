#pragma once
typedef int element;

typedef struct stackNode {
	element data;
	struct stackNode* link; //노드 만들건데 링크는 다음 노드의 주소를 저장할거야

} stackNode;

stackNode* top;

int isStackEmpty();
void push(element item);
element pop();
element peek();
void printStack();