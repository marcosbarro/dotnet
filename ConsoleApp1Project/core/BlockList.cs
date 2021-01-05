using System;
using System.Collections.Generic;
using System.Text;

namespace Indigo.core
{
    /// <summary>
    /// Clase para estructuración de listas en bloques o páginas,
    /// de forma que se puedan tratar sus elementos en conjuntos
    /// de tamaño fijo.
    /// </summary>
    /// <typeparam name="T">Tipo de los elementos de la lista</typeparam>
    public class BlockList<T>
    {

        // lista original
        private List<T> _sourceList;

        // tamaño de los bloques
        private int _blockSize;
        public int BlockSize { get { return _blockSize; } }

        // lista de bloques 
        private List<List<T>> _blockList;

        /// <summary>
        /// Devuelve el número de bloques (páginas)
        /// </summary>
        public int BlockCount { get { return _blockList.Count; } }

        /// <summary>
        /// Devuelve una lista con los elementos del bloque especificado
        /// </summary>
        /// <param name="i">número de bloque (0... BlockCount - 1)</param>
        /// <returns>lista con los elementos del bloque especificado</returns>
        public List<T> Block(int i)
        {   
            List<T> result = new List<T>();
            if (i >= 0 && i < this.BlockCount)
            {
                result.AddRange(_blockList[i]);
            }
            return result;
        }

        /// <summary>
        /// Instancia una nueva lista de bloques para la colección y tamaño
        /// de bloque especificados
        /// </summary>
        /// <param name="sourceList">lista original</param>
        /// <param name="blockSize">tamaño de bloque</param>
        public BlockList(ICollection<T> sourceList, int blockSize)
        {
            if (sourceList == null)
            {
                throw new Exception("No se especificó la lista subyacente");
            }

            if (blockSize < 1)
            {
                throw new Exception("El tamaño de bloque no puede ser menor que 1");
            }

            _sourceList = new List<T>(sourceList);
            _blockSize = blockSize;

            this.initialize();
        }

        /// <summary>
        /// inicializa la lista de bloques con los correspondientes para
        /// la lista de origen y el tamaño de bloque actuales
        /// </summary>
        private void initialize()
        {   // se inicializa la lista de bloques
            _blockList = new List<List<T>>();

            // número de elementos pendientes de agregar
            int pendientes = _sourceList.Count;
            int i = 0;
            while (pendientes > 0)
            {
                int offset = Math.Min(_blockSize, pendientes);
                List<T> bloque = _sourceList.GetRange(i, offset);
                _blockList.Add(bloque);

                pendientes -= bloque.Count;
                i += offset;
            }
        }
    }
}
