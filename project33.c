#include <stdio.h>

int main(void) {

  int length = 1;
  char structure_student[100][3][30];
  while (length > 0) {
    printf("학생 %d의 이름: ", length);
    scanf("%s", &*structure_student[length-1][0]);
    printf("학생 %d의 학과: ", length);
    scanf("%s", &*structure_student[length-1][1]);
    printf("학생 %d의 학번: ", length);
    scanf("%s", &*structure_student[length-1][2]);
    length++;
    if (length == 3) {
      break;
    }
  }

  for(int j = 1; j < length; j++) {
    printf("학생 %d\n", j);
    printf("           이름: %s\n", structure_student[j-1][0]);
    printf("           학과: %s\n", structure_student[j-1][1]);
    printf("           학번: %s\n", structure_student[j-1][2]);
  }

  return 0;
}
