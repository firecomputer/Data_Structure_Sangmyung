#include <stdio.h>

int main(void) {
  int val[2][3][4] = { 0, };

  for (int i= 0; i < 2; i++) {
    for (int j = 0; j < 3; j++) {
      for (int k=0; k < 4; k++) {
        scanf("%d", &val[i][j][k]);
      }
    }
  }
}
