#include<stdio.h>
int main() {
	int val = 15;
	int* pval = NULL;

	pval = &val;

	*(pval) = 16;

	return 0;
}