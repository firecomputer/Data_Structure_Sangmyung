#include <stdio.h>
#include <stdlib.h>
#include "stackS.h"


int main(void) {
  element item;
  printf("\n** 순차 스택 연산 **\n");
  printStack();
  push(1);  printStack();
  push(2); printStack();
  push(3); printStack();

  item = peek(); printStack();
  printf("peek => %d", item);

  item = pop(); printStack();
  printf("\t pop => %d", item);

  item = pop(); printStack();
  printf("\t pop => %d", item);

  item = pop(); printStack();
  printf("\t pop => %d", item);

  getchar(); return 0;
}

int top = -1;

int isStackEmpty() {
  if(top == -1) return 1;
  else return 0;
}

int isStackFull() {
  if(top == STACK_SIZE - 1) return 1;
  else return 0;
}

void push(element item) {
  if (isStackFull()) {
    printf("\n\n  Stack is FULL! \n");
    return;
  }
  else stack[++top] = item;
}

element pop() {
  if (isStackEmpty()) {
    printf("\n\n Stack is Empty!! \n");
    return 0;
  }
  else return stack[top--];
}

element peek() {
  if(isStackEmpty()) {
    printf("\n\n Stack is Empty !\n");
    exit(1);
  }
  else return stack[top];
}

void printStack() {
  int i;
  printf("\n STACK [");
  for (i = 0; i <= top; i++)
    printf("%d ", stack[i]);
  printf("] ");
}
