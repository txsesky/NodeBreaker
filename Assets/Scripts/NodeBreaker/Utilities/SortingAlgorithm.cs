using System.Collections.Generic;
using NodeBreaker.Data;

namespace NodeBreaker.Utilities
{
    public static class SortingAlgorithm
    {
        public static void QuickSort(List<Entity> arr, int left, int right)
        {
            if (left >= right)
            {
                return;
            }

            var pivot = Sorting(arr, left, right);
            QuickSort(arr, left, pivot - 1);
            QuickSort(arr, pivot + 1, right);
        }

        private static int Sorting(List<Entity> arr, int left, int right)
        {
            var pointer = left;

            for (int i = left; i <= right; i++)
            {
                if (arr[i].distanceFromBaseCenter < arr[right].distanceFromBaseCenter)
                {
                    SwapElements(arr, pointer, i);
                    pointer++;
                }
            }

            SwapElements(arr, pointer, right);
            return pointer;
        }

        private static void SwapElements(List<Entity> arr, int i, int j)
        {
            var a = arr[i];
            arr[i] = arr[j];
            arr[j] = a;
        }
    }
}