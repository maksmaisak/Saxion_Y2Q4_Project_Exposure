using System;
using System.Collections;
using System.Collections.Generic;

public static class ArrayExtensions
{
    public static void Shuffle<T>(this T[] array) => array.Shuffle(new Random());
    
    public static void Shuffle<T>(this T[] array, Random rng)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            int j = rng.Next(i, array.Length);

            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}