using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ConsoleApp1Project.core
{
    /// <summary>
    /// Clase para gestionar una tarea que a partir de una entrada devuelve una salida.
    /// Para instanciar un objeto de esta clase hay que especificar el objeto con el valor de entrada y el método
    /// de ejecución que determina la salida.
    /// </summary>
    /// <typeparam name="I">Clase para el objeto con el valor de entrada</typeparam>
    /// <typeparam name="O">Clase para el objeto con el valor de salida</typeparam>
    public class PTask<I, O>
    {
        // entrada
        private readonly I _input = default;
        public I Input { get { return _input; } }

        // delegado y método para el ejecutor (que genera la salida a partir de una entrada)
        public delegate O ExecutorDelegate(I input);
        private readonly ExecutorDelegate _executeMethod;

        /// <summary>
        /// Instancia un nuevo objeto para una tarea a partir de una entrada y un método de ejecución
        /// </summary>
        /// <param name="input">objeto con el valor de entrada</param>
        /// <param name="executeMethod">método  ejecutar en la tarea para, a partir de la entrada, devolver una salida</param>
        public PTask(I input, ExecutorDelegate executeMethod)
        {
            _input = input;
            _executeMethod = executeMethod;
        }


        /// <summary>
        /// Método de ejecución 
        /// </summary>
        /// <returns>Valor de la salida a partir de la entrada actual</returns>
        private O execute()
        {
            return _executeMethod(this.Input);
        }

        /// <summary>
        /// Devuelve una Task para la ejecución de la tarea definida en el objeto actual
        /// </summary>
        /// <returns>objeto con la tarea ejecutable</returns>
        public Task<O> createTask()
        {
            return new Task<O>( this.execute );
            //return new Task<O>(delegate () { return this.execute(); });
        }
    }


    /// <summary>
    /// Clase para gestionar ejecuciones en paralelo de tareas que a partir de una entrada devuelven una salida. 
    /// Hace uso de la clase PTask<I, O>, y permite gestionar la ejecución en paralelo de forma parametrizada en
    /// base a métodos para dividir la entrada y componer a salida
    /// </summary>
    /// <typeparam name="I">Clase para el objeto con el valor de entrada</typeparam>
    /// <typeparam name="O">Clase para el objeto con el valor de salida</typeparam>
    public class PHandler<I, O>
    {
        #region delegado y propiedad asociada al método para dividir la entrada en una colección de entradas equivalente

        /// <summary>
        /// Definición o firma de función que, a partir de un objeto de entrada devuelve 
        /// una colección de objetos de entrada equivalente
        /// </summary>
        /// <param name="input">objeto con el valor de la entrada</param>
        /// <param name="parts">número de objetos de entrada a devolver. En caso de que la función que se implemente no pueda devolver una colección con este número 
        /// de objetos, no pasa nada, pero lo ideal es que devuelve una colección de este tamaño, para poder ajustar la ejecución en paralelo</param>
        /// <returns>Colección de objetos de entrada equivalente</returns>
        public delegate ICollection<I> DivideInputDelegate(I input, int parts);

        private DivideInputDelegate _divideInputMethod = default;

        /// <summary>
        /// Método que divide una entrada en una colección de entradas equivalente
        /// </summary>
        public DivideInputDelegate DivideInputMethod { set { _divideInputMethod = value; } }

        #endregion


        #region delegado y propiedad asociada al método para unir una colección de objetos de salidas en un único objeto de salida equivalente

        /// <summary>
        /// Definición o firma de una función que, a partir de una colecciónb de objetos de salida, devuelve 
        /// un objeto con el valor de salida equivalente.
        /// </summary>
        /// <param name="outputs">colección de objetos con valores de salida</param>
        /// <returns>objeto de salida equivalente a la colección especificada</returns>
        public delegate O MergeOutputsDelegate(ICollection<O> outputs);

        private MergeOutputsDelegate _mergeOutputsMethod = default;

        /// <summary>
        /// Método que devuelve una salida equivalente a partir de una colección de objetos de salida
        /// </summary>
        public MergeOutputsDelegate MergeOutputsMethod { set { _mergeOutputsMethod = value; } }

        #endregion


        #region  propiedad asociada al método  que se ejecuta en la tarea para producir una salida a partir de una entrada

        private PTask<I, O>.ExecutorDelegate _taskExecutorMethod = default;

        /// <summary>
        /// Método que devuelve un objeto de salida a partir de un objeto de entrada
        /// </summary>
        public PTask<I, O>.ExecutorDelegate TaskExecutorMethod { set { _taskExecutorMethod = value; } }


        #endregion


        // entrada
        private I _input = default;
        public I Input { 
            get { return _input; } 
            set { _input = value; } 
        }

        // salida
        private O _output = default;
        public O Output { 
            get { return _output; } 
        }

        // time out
        private int _timeOut = 0;
        public int TimeOut {
            get { return _timeOut; } 
            set 
            { 
                if (value < 0)
                {
                    throw new Exception("el timeOut no puede ser negativo (cero para ignorarlo)");
                }
                _timeOut = value; 
            } 
        }

        // contador de partes en las que se dividirá 
        private int _inputPartsCount = 2;
        public int InputPartsCount {
            get { return _inputPartsCount; }
            set
            {
                if (value < 2)
                {
                    throw new Exception("la entrada no se puede dividir en menos de dos partes");
                }

                _inputPartsCount = value;
            }
        }

        // número de hilos a ejecutar en paralelo
        private int _threadCount = 1;
        public int ThreadCount { 
            get { return _threadCount; } 
            set 
            {
                if (value < 1)
                {
                    throw new Exception("el número de hilos no puede ser menor que uno");
                }
                _threadCount = value; 
            } 
        }

        
        // devuelve una lista de tareas nuevas correspondientes a la ejecúcion del método principal
        // sobre cada una de las particiones de la entrada.
        private List<Task<O>> createTasks()
        {
            // comprobar que se hayan asignado los métodos necesarios
            if (_divideInputMethod == default)
            {
                throw new Exception("No se asignó el método de particionado de entradas");
            }

            if (_taskExecutorMethod == default)
            {
                throw new Exception("No se asignó el método a ejecutar en las tareas");
            }

            // lista a devolver
            List<Task<O>> tasks = new List<Task<O>>();

            ICollection<I> inputs = _divideInputMethod(_input, _inputPartsCount);
            foreach (I input in inputs)
            {
                PTask<I, O> ptask = new PTask<I, O>(input, _taskExecutorMethod);
                tasks.Add(ptask.createTask());
            }

            return tasks;
        }


        public O execute() 
        {
            // lista de tareas
            List<Task<O>> taskList = this.createTasks();
            Console.WriteLine("número de tareas total: " + taskList.Count);

            // estructuramos la lista de tareas en bloques con tantos elementos como hilos (cada bloque se ejecuta en paralelo)
            BlockList<Task<O>> taskBlock = new BlockList<Task<O>>(taskList, _threadCount);
            Console.WriteLine("número de bloques de tareas: " + taskBlock.BlockCount);
            Console.WriteLine("número de tareas por bloque: " + taskBlock.BlockSize);

            int timeOut = _timeOut;
            for (int i = 0; i < taskBlock.BlockCount; i++)
            {   // ejecutar las tareas del bloque 
                DateTime inicio = DateTime.Now;

                List<Task<O>> tasks = taskBlock.Block(i);
                foreach (Task<O> task in tasks)
                {
                    task.Start();
                }

                // esperar finalización (con o sin timeout)
                if (_timeOut == 0) 
                {
                    Task<O>.WaitAll(tasks.ToArray());
                }
                else
                {
                    if (!Task<O>.WaitAll(tasks.ToArray(), timeOut))
                    {
                        throw new Exception("TimeOut finalizado");
                    }
                }

                // actualizar el tiempo restante
                TimeSpan ts = DateTime.Now.Subtract(inicio);
                timeOut -= (int)ts.TotalMilliseconds;
            }

            // componer la salida y devolverla
            List<O> outputs = new List<O>();
            foreach (Task<O> task in taskList)
            {
                outputs.Add(task.Result);
            }

            _output = _mergeOutputsMethod(outputs);
            return _output;
        }
    }

    namespace test
    {
        public class CustomInput
        {
            public List<int> Numeros { get; set; }

            public CustomInput(List<int> numeros)
            {
                Numeros = numeros;
            }

            override public string ToString()
            {
                string resultado = "{";
                resultado +=  "[";
                if (Numeros == null) {
                    resultado += "null";
                } else {
                    for (int i = 0; i < Numeros.Count; i++) {
                        if (i != 0) {
                            resultado += ",";
                        }
                        resultado += Numeros[i];
                    }

                }
                resultado += "]}";

                return resultado;
            }
        }

        public class CustomOutput
        {
            public List<int> Numeros { get; set; }
            public int Suma { get; set; } = 0;

            public CustomOutput(List<int> numeros)
            {
                Numeros = numeros;
            }

            override public string ToString()
            {
                string resultado = "{";
                resultado += Suma + ",[";
                if (Numeros == null)
                {
                    resultado += "null";
                }
                else
                {
                    for (int i=0; i< Numeros.Count; i++)
                    {
                        if (i != 0)
                        {
                            resultado += ",";
                        }
                        resultado += Numeros[i];
                    }

                }
                resultado += "]}";

                return resultado;
            }
        }

        public static class Contador
        {
            
            private static int _valor = 0;

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static int get()
            {
                return (++_valor);
            }
        }



        public static class Metodos
        {
            public static List<CustomInput> dividirEntrada(CustomInput input, int parts)
            {
                List<CustomInput> resultado = new List<CustomInput>();
                int blockSize = Math.Max(1, input.Numeros.Count / parts);
                BlockList<int> bloques = new BlockList<int>(input.Numeros, blockSize);
                for (int i = 0; i < bloques.BlockCount; i++)
                {
                    resultado.Add(new CustomInput(bloques.Block(i)));
                }
                return resultado;
            }

            public static CustomOutput componerSalida(ICollection<CustomOutput> outputs)
            {
                CustomOutput resultado = new CustomOutput(new List<int>());
                foreach (CustomOutput output in outputs)
                {
                    resultado.Numeros.AddRange(output.Numeros);
                    resultado.Suma += output.Suma;
                }
                return resultado;
            }


            public static CustomOutput ejecutar(CustomInput input)
            {
                DateTime inicio = DateTime.Now;
                int contador = Contador.get();
                string ejecucion = "ejecución_[" + contador + "]" + Thread.CurrentThread.ManagedThreadId;// ;
                Console.WriteLine(ejecucion + " [INICIO] " + inicio.ToString("hh:mm:ss"));
                //Console.WriteLine(ejecucion + " [INICIO]: " + input.ToString());

                CustomOutput resultado = new CustomOutput(input.Numeros);
                int suma = 0;
                foreach (int numero in input.Numeros)
                {
                    Thread.Sleep(500);
                    suma += numero;
                }
                resultado.Suma = suma;
                //Console.WriteLine(ejecucion + " [FIN]: " + resultado.ToString());
                Console.WriteLine(ejecucion + " [FIN] " + DateTime.Now.ToString("hh:mm:ss") + " (" + DateTime.Now.Subtract(inicio).TotalSeconds + " s)") ;

                return resultado;
            }

            public static void writeList(string titulo, List<int> numeros)
            {
                Console.Write(titulo + ":");
                foreach (int numero in numeros)
                {
                    Console.Write(" " + numero);
                }
                Console.WriteLine();
            }


            public static void testPHandler()
            {
                Console.WriteLine("Máximo -> " + TaskScheduler.Default.MaximumConcurrencyLevel);


                List<int> milNumeros = new List<int>() {
                1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,
                51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,
                101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,
                151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,
                201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,
                251,252,253,254,255,256,257,258,259,260,261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,276,277,278,279,280,281,282,283,284,285,286,287,288,289,290,291,292,293,294,295,296,297,298,299,300,
                301,302,303,304,305,306,307,308,309,310,311,312,313,314,315,316,317,318,319,320,321,322,323,324,325,326,327,328,329,330,331,332,333,334,335,336,337,338,339,340,341,342,343,344,345,346,347,348,349,350,
                351,352,353,354,355,356,357,358,359,360,361,362,363,364,365,366,367,368,369,370,371,372,373,374,375,376,377,378,379,380,381,382,383,384,385,386,387,388,389,390,391,392,393,394,395,396,397,398,399,400,
                401,402,403,404,405,406,407,408,409,410,411,412,413,414,415,416,417,418,419,420,421,422,423,424,425,426,427,428,429,430,431,432,433,434,435,436,437,438,439,440,441,442,443,444,445,446,447,448,449,450,
                451,452,453,454,455,456,457,458,459,460,461,462,463,464,465,466,467,468,469,470,471,472,473,474,475,476,477,478,479,480,481,482,483,484,485,486,487,488,489,490,491,492,493,494,495,496,497,498,499,500,
                501,502,503,504,505,506,507,508,509,510,511,512,513,514,515,516,517,518,519,520,521,522,523,524,525,526,527,528,529,530,531,532,533,534,535,536,537,538,539,540,541,542,543,544,545,546,547,548,549,550,
                551,552,553,554,555,556,557,558,559,560,561,562,563,564,565,566,567,568,569,570,571,572,573,574,575,576,577,578,579,580,581,582,583,584,585,586,587,588,589,590,591,592,593,594,595,596,597,598,599,600,
                601,602,603,604,605,606,607,608,609,610,611,612,613,614,615,616,617,618,619,620,621,622,623,624,625,626,627,628,629,630,631,632,633,634,635,636,637,638,639,640,641,642,643,644,645,646,647,648,649,650,
                651,652,653,654,655,656,657,658,659,660,661,662,663,664,665,666,667,668,669,670,671,672,673,674,675,676,677,678,679,680,681,682,683,684,685,686,687,688,689,690,691,692,693,694,695,696,697,698,699,700,
                701,702,703,704,705,706,707,708,709,710,711,712,713,714,715,716,717,718,719,720,721,722,723,724,725,726,727,728,729,730,731,732,733,734,735,736,737,738,739,740,741,742,743,744,745,746,747,748,749,750,
                751,752,753,754,755,756,757,758,759,760,761,762,763,764,765,766,767,768,769,770,771,772,773,774,775,776,777,778,779,780,781,782,783,784,785,786,787,788,789,790,791,792,793,794,795,796,797,798,799,800,
                801,802,803,804,805,806,807,808,809,810,811,812,813,814,815,816,817,818,819,820,821,822,823,824,825,826,827,828,829,830,831,832,833,834,835,836,837,838,839,840,841,842,843,844,845,846,847,848,849,850,
                851,852,853,854,855,856,857,858,859,860,861,862,863,864,865,866,867,868,869,870,871,872,873,874,875,876,877,878,879,880,881,882,883,884,885,886,887,888,889,890,891,892,893,894,895,896,897,898,899,900,
                901,902,903,904,905,906,907,908,909,910,911,912,913,914,915,916,917,918,919,920,921,922,923,924,925,926,927,928,929,930,931,932,933,934,935,936,937,938,939,940,941,942,943,944,945,946,947,948,949,950,
                951,952,953,954,955,956,957,958,959,960,961,962,963,964,965,966,967,968,969,970,971,972,973,974,975,976,977,978,979,980,981,982,983,984,985,986,987,988,989,990,991,992,993,994,995,996,997,998,999,1000
                };

                List<int> numeros = milNumeros.GetRange(0, 100);
                DateTime inicio = DateTime.Now;
                int HILOS = 100;
                int PARTES = 50;
                PHandler<CustomInput, CustomOutput> phandler = new PHandler<CustomInput, CustomOutput>();
                phandler.Input = new CustomInput(numeros);
                phandler.ThreadCount = HILOS;
                phandler.InputPartsCount = PARTES;
                phandler.DivideInputMethod = Metodos.dividirEntrada;
                phandler.MergeOutputsMethod = Metodos.componerSalida;
                phandler.TaskExecutorMethod = Metodos.ejecutar;
                CustomOutput output = phandler.execute();

                TimeSpan ts = DateTime.Now.Subtract(inicio);
                Console.WriteLine("resultado: " + output.ToString());
                Console.WriteLine("tiempo: " + ts.TotalSeconds + " s");
            }

        }
    }
}

