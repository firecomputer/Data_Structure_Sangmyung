#pragma once
typedef int element;

typedef struct queueNode {
  element data;
  struct queueNode* link;
} queueNode;

queueNode* queue;

int isQueueEmpty();
void enqueue(element item);
element dequeue();
element peek();
void printQueue();
