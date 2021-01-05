using System;
using Indigo.core;
using System.Collections.Generic;
using Indigo.core.test;

namespace ConsoleApp1Project
{
    class Program
    {
        static void Main(string[] args)
        {

            Metodos.testPHandler();
        }

        static void bloquesTest()
        {
            List<int> numeros = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
            Metodos.writeList("lista original", numeros);

            

            BlockList<int> paginas = new BlockList<int>(numeros, 2);
            Console.WriteLine("paginas: " + paginas.BlockCount);
            Console.WriteLine("bloque:  " + paginas.BlockSize);
            for (int i = 0; i < paginas.BlockCount; i++)
            {
                Metodos.writeList("bloque [" + i + "]", paginas.Block(i));
            }
        }
    }

}
