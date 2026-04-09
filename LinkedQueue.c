#include <stdio.h>
#include <stdlib.h>
#include "queueL.h"

int main()
{
  enqueue(1); printQueue();
  enqueue(2); printQueue();
  enqueue(3); printQueue();
  enqueue(4); printQueue();

  printf("peek = %d\n", peek());

  dequeue(); printQueue();
  dequeue(); printQueue();
  dequeue(); printQueue();
  dequeue(); printQueue();

  printf("----  프로그램 종료  ----");

  return 0;
}

int front, rear = -1;

void enqueue(int item) 
{
  rear++;
  queueNode* temp = (queueNode*)malloc(sizeof(queueNode));
  temp->link = queue;
  temp->data = item;
  queue = temp;
}

element peek()
{
  if(isQueueEmpty() == -1) {
    return 0;
  }
  queueNode* temp = (queueNode*)malloc(sizeof(queueNode));
  for(int i = -1; i < rear; i++){
    if (i == -1) {
      temp = queue;
    }
    else {
      temp = temp->link;
    }
  }
  return temp->data;
}

element dequeue()
{
  if(isQueueEmpty() == -1) {
    return 0;
  }
  element temp;
  temp = peek();
  rear--;
  return temp;
}


void printQueue()
{
  printf("QUEUE [ ");
  queueNode* temp = (queueNode*)malloc(sizeof(queueNode));
  for (int i = -1; i < rear; i++) {
    if (i == -1) {
      temp = queue;
    }
    else {
      temp = temp->link;
    }
    printf("%d ", temp->data);
  }
  printf(" ]\n");
}

int isQueueEmpty()
{
  if(rear == front) {
    printf("queue is empty!!");
    return -1;
  }
  else {
    return 0;
  }
}
