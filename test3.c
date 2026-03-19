#include <stdio.h>

int main(void) {
  char string[4][15] = {"Korea", "Seoul", "Mapo", "152번지 2/3"};
  char *ptr[4] = { "Korea", "Seoul", "Mapo", "152번지 2/3" };

  for(int i=0; i<3; i++) {
    printf("%s\n", &string[i]);
    printf("%s\n", &*ptr[i]);
  }


  return 0;
 }
