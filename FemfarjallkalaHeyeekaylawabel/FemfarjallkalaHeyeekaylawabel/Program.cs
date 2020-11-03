using System;
using System.Threading;

namespace FemfarjallkalaHeyeekaylawabel
{
    class Program
    {
        static void Main(string[] args)
        {
            
        }
    }

    class SpinReaderAndWriterLock
    {
        public void EnterReaderLock()
        {
            while (_writerCount > 0)
            {

            }

            Interlocked.Increment(ref _readerCount);
        }

        public void ExitReaderLock()
        {
            Interlocked.Decrement(ref _readerCount);
        }

        public void EnterWriterLock()
        {
            while (_readerCount > 0)
            {

            }

            while (_writerCount>0)
            {
                
            }

            Interlocked.Increment(ref _writerCount);
        }

        public void ExitWriterLock()
        {
            Interlocked.Decrement(ref _writerCount);
        }

        private int _writerCount;

        private int _readerCount;
    }
}
