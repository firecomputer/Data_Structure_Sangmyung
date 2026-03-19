#include <stdio.h>

int fact(int n);

int main (void) {
  int num = 0;
  int result = 0;
  printf("정수를 입력하세요: ");
  scanf("%d", &num);
  
  result = fact(num);

  printf("%d의 팩토리얼 값은 %d 입니다. \n", num, result);

  return 0;
}

int fact (int n) {
  if (n <= 1) {
    return 1;
  }
  else {
    return (n * fact(n-1));
  }
}
