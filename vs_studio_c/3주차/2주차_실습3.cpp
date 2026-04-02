#include <stdio.h>

int main()
{
    int num = 10;
    int* pnum = &num;

    printf("%d, %d\n", num, *pnum);

    return 0;
}
